using System.Collections.Generic;
using PrisonLife.Core;
using PrisonLife.FX;
using PrisonLife.Items;
using PrisonLife.Player;
using PrisonLife.UI;
using UnityEngine;

namespace PrisonLife.Factory
{
    /// <summary>
    /// Consumes Rock from carriers inside its deposit zone, produces Handcuffs onto its shelf at a configured rate.
    /// Stops producing when the shelf reaches capacity and shows a MAX label.
    /// </summary>
    public class ProcessingMachine : MonoBehaviour
    {
        [System.Serializable]
        public class SmokeSettings
        {
            public Vector3 localOffset = new Vector3(0f, 1.8f, -0.4f);
            [Tooltip("If true, override the wired smokeFx's localPosition with localOffset. Off by default so a hand-placed PS stays where you put it.")]
            public bool overrideOffsetWhenWired = false;
            [Min(0.05f)] public float lifetime = 1.6f;
            [Min(0f)] public float startSpeed = 0.7f;
            [Min(0f)] public float startSizeMin = 0.45f;
            [Min(0f)] public float startSizeMax = 0.75f;
            public Color tint = new Color(1f, 1f, 1f, 0.45f);
            [Min(0f)] public float emissionRate = 8f;
            [Range(0f, 90f)] public float coneAngle = 15f;
            [Min(0f)] public float coneRadius = 0.1f;
            [Min(0f)] public float sizeStart = 0.3f;
            [Min(0f)] public float sizeEnd = 1.4f;
            [Range(0f, 1f)] public float alphaStart = 0.5f;
        }

        [SerializeField] private Collider depositZone;
        [SerializeField] private HandcuffShelf shelf;
        [SerializeField] private ParticleSystem smokeFx;
        [SerializeField] private SmokeSettings smoke = new SmokeSettings();
        [SerializeField] private int shelfCap = 100;
        [SerializeField] private Vector3 maxLabelOffset = new Vector3(0f, 3f, 0f);

        [Header("SFX")]
        [SerializeField] private AudioClip machineLoopSfx;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;
        private AudioSource _machineLoopSrc;

        [Header("Rock Pile (intake buffer)")]
        [SerializeField] private Transform rockPileRoot;
        [SerializeField] private Vector3 rockPileItemScale = new Vector3(0.32f, 0.28f, 0.32f);
        [SerializeField] private float rockPileItemHeight = 0.3f;
        [SerializeField] private float rockPileSideOffset = 0.34f;
        [SerializeField] private int rockPileRowSize = 4;
        [SerializeField] private int rockPileMax = 60;

        private readonly HashSet<CarryStack> _rockStacksInZone = new();

        private float _rockIntakeTimer;
        private float _produceTimer;
        private readonly List<GameObject> _pile = new();
        private GameObject _maxLabelGo;
        private TextMesh _maxLabel;
        private bool _smokeAutoCreated;

        private void Reset()
        {
            if (depositZone != null) depositZone.isTrigger = true;
        }

        private void Start()
        {
            if (smokeFx == null)
            {
                smokeFx = FxSpawner.CreateSmokeLoop(transform, smoke.localOffset);
                _smokeAutoCreated = true;
            }
            ApplySmokeSettings();
        }

        /// <summary>Push current SmokeSettings into the live ParticleSystem. Safe to call at runtime.</summary>
        [ContextMenu("Apply Smoke Settings")]
        public void ApplySmokeSettings()
        {
            if (smokeFx == null) return;
            if (_smokeAutoCreated || smoke.overrideOffsetWhenWired)
                smokeFx.transform.localPosition = smoke.localOffset;

            var m = smokeFx.main;
            m.startLifetime = smoke.lifetime;
            m.startSpeed = smoke.startSpeed;
            m.startSize = new ParticleSystem.MinMaxCurve(
                Mathf.Min(smoke.startSizeMin, smoke.startSizeMax),
                Mathf.Max(smoke.startSizeMin, smoke.startSizeMax));
            m.startColor = smoke.tint;
            m.loop = true;
            m.playOnAwake = false;

            var em = smokeFx.emission;
            em.rateOverTime = smoke.emissionRate;

            var sh = smokeFx.shape;
            sh.shapeType = ParticleSystemShapeType.Cone;
            sh.angle = smoke.coneAngle;
            sh.radius = smoke.coneRadius;

            var sol = smokeFx.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f,
                new AnimationCurve(new Keyframe(0f, smoke.sizeStart), new Keyframe(1f, smoke.sizeEnd)));

            var col = smokeFx.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            var rgb = new Color(smoke.tint.r, smoke.tint.g, smoke.tint.b, 1f);
            grad.SetKeys(
                new[] { new GradientColorKey(rgb, 0f), new GradientColorKey(rgb, 1f) },
                new[] { new GradientAlphaKey(smoke.alphaStart, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(grad);
        }

        private void OnTriggerEnter(Collider other)
        {
            var carrier = other.GetComponentInParent<CarryStack>();
            var player = other.GetComponentInParent<PlayerController>();
            if (player != null) { _rockStacksInZone.Add(player.RockStack); return; }
            if (carrier != null && carrier.Kind == CarryKind.Rock) _rockStacksInZone.Add(carrier);
        }

        private void OnTriggerExit(Collider other)
        {
            var carrier = other.GetComponentInParent<CarryStack>();
            var player = other.GetComponentInParent<PlayerController>();
            if (player != null) { _rockStacksInZone.Remove(player.RockStack); return; }
            if (carrier != null) _rockStacksInZone.Remove(carrier);
        }

        private void Update()
        {
            var cfg = GameManager.Instance != null ? GameManager.Instance.Config : null;
            if (cfg == null) return;

            bool shelfFull = shelf != null && shelf.Count >= shelfCap;
            UpdateMaxLabel(shelfFull);

            _rockIntakeTimer -= Time.deltaTime;
            if (_rockIntakeTimer <= 0f)
            {
                _rockIntakeTimer = cfg.machineRockIntakeInterval;
                TryPullRock();
            }

            bool producing = _pile.Count > 0 && !shelfFull;
            if (producing)
            {
                _produceTimer -= Time.deltaTime;
                if (_produceTimer <= 0f)
                {
                    _produceTimer = cfg.machineSecondsPerHandcuff;
                    if (shelf != null && shelf.AddHandcuff())
                        RemovePileVisual();
                }
                if (smokeFx != null && !smokeFx.isPlaying) smokeFx.Play();
                SfxPlayer.EnsureLoop(ref _machineLoopSrc, gameObject, machineLoopSfx, sfxVolume);
            }
            else
            {
                if (smokeFx != null && smokeFx.isPlaying) smokeFx.Stop();
                SfxPlayer.StopLoop(_machineLoopSrc);
            }
        }

        private void TryPullRock()
        {
            if (_pile.Count >= rockPileMax) return;
            foreach (var stack in _rockStacksInZone)
            {
                if (stack != null && stack.Kind == CarryKind.Rock && stack.Count > 0)
                {
                    if (stack.TryRemove())
                    {
                        AddPileVisual();
                        return;
                    }
                }
            }
        }

        /// <summary>Directly feed rocks into the machine without needing a carrier (used by workers).</summary>
        public void AcceptRock(int count = 1)
        {
            if (count <= 0) return;
            for (int i = 0; i < count; i++)
            {
                if (_pile.Count >= rockPileMax) break;
                AddPileVisual();
            }
        }

        private void AddPileVisual()
        {
            var root = rockPileRoot != null ? rockPileRoot : transform;
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var col = block.GetComponent<Collider>(); if (col != null) Destroy(col);
            block.transform.SetParent(root, false);
            block.transform.localScale = rockPileItemScale;

            int idx = _pile.Count;
            int rs = Mathf.Max(1, rockPileRowSize);
            int row = idx / rs;
            int c = idx % rs;
            float x = (c - (rs - 1) * 0.5f) * rockPileSideOffset;
            float y = row * rockPileItemHeight + rockPileItemHeight * 0.5f;
            block.transform.localPosition = new Vector3(x, y, 0f);
            block.transform.localRotation = Quaternion.Euler(Random.Range(-12f, 12f), Random.Range(0f, 360f), Random.Range(-12f, 12f));
            var rend = block.GetComponent<Renderer>();
            if (rend != null) rend.sharedMaterial = _rockMat ??= MakeRockMat();
            block.name = $"PileRock_{idx}";
            _pile.Add(block);
        }

        private void RemovePileVisual()
        {
            if (_pile.Count == 0) return;
            int last = _pile.Count - 1;
            var go = _pile[last];
            _pile.RemoveAt(last);
            if (go != null) Destroy(go);
        }

        private static Material _rockMat;
        private static Material MakeRockMat()
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            m.color = new Color(0.32f, 0.32f, 0.35f);
            return m;
        }

        private void UpdateMaxLabel(bool show)
        {
            if (_maxLabelGo == null)
            {
                _maxLabelGo = new GameObject("_MaxLabel");
                _maxLabelGo.transform.SetParent(transform, false);
                _maxLabelGo.transform.localPosition = maxLabelOffset;
                _maxLabelGo.transform.localScale = Vector3.one * 0.35f;
                _maxLabel = _maxLabelGo.AddComponent<TextMesh>();
                _maxLabel.text = "MAX";
                _maxLabel.fontSize = 64;
                _maxLabel.characterSize = 0.6f;
                _maxLabel.anchor = TextAnchor.MiddleCenter;
                _maxLabel.alignment = TextAlignment.Center;
                _maxLabel.color = new Color(1f, 0.25f, 0.2f);
                var mr = _maxLabelGo.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    mr.receiveShadows = false;
                }
                _maxLabelGo.AddComponent<WorldLabelBillboard>();
            }
            if (_maxLabelGo.activeSelf != show) _maxLabelGo.SetActive(show);
        }
    }
}
