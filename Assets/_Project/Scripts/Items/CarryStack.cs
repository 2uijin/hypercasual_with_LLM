using System.Collections.Generic;
using PrisonLife.FX;
using PrisonLife.UI;
using UnityEngine;

namespace PrisonLife.Items
{
    public enum CarryKind { Rock, Handcuff, Cash }

    /// <summary>
    /// Visual stack of carry items attached to an NPC/Player. Holds primitive meshes with per-type color.
    /// Capacity limit only applies when specified (>0).
    /// </summary>
    public class CarryStack : MonoBehaviour
    {
        [SerializeField] private Transform stackRoot;
        [SerializeField] private float itemHeight = 0.15f;
        [SerializeField] private float sideOffset = 0.18f;
        [SerializeField] private int rowSize = 4;
        [SerializeField] private int capacity = -1; // -1 = infinite
        [SerializeField] private Vector3 itemScale = new Vector3(0.25f, 0.12f, 0.35f);
        [SerializeField] private bool showMaxLabel = true;

        [Header("SFX (pickup = TryAdd, place = TryRemove)")]
        [SerializeField] private AudioClip rockPickupSfx;
        [SerializeField] private AudioClip rockPlaceSfx;
        [SerializeField] private AudioClip cashPickupSfx;
        [SerializeField] private AudioClip cashPlaceSfx;
        [SerializeField] private AudioClip handcuffPickupSfx;
        [SerializeField] private AudioClip handcuffPlaceSfx;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;

        [Header("Wobble (lean opposite to motion, rebound on stop; rocks + cash)")]
        [SerializeField] private bool wobbleEnabled = true;
        [SerializeField] private int wobbleMinCount = 3;
        [SerializeField] private float wobbleLeanScale = 0.05f;
        [SerializeField] private float wobbleStiffness = 120f;
        [SerializeField] private float wobbleDamping = 4f;
        [SerializeField] private float wobbleMaxOffset = 0.35f;
        [SerializeField] private float wobbleVelMax = 4f;

        private readonly List<GameObject> _items = new();
        private readonly List<Vector3> _restLocal = new();
        private CarryKind _kind;
        private int _reserved;
        private int _lastCashPickupSfxFrame = -1;
        private GameObject _maxLabelGo;
        private TextMesh _maxLabel;

        private Vector3 _prevWorldPos;
        private Vector3 _prevWorldVel;
        private bool _hasPrevPos;
        private Vector2 _sway;
        private Vector2 _swayVel;

        public int Count => _items.Count;
        public int Capacity => capacity;
        public CarryKind Kind => _kind;
        public bool IsEmpty => _items.Count == 0;
        public bool IsFull => capacity > 0 && (_items.Count + _reserved) >= capacity;
        public Transform Root => stackRoot != null ? stackRoot : transform;

        public void Reserve() => _reserved++;
        public void Release() { if (_reserved > 0) _reserved--; }

        public System.Action OnChanged;

        private void Awake()
        {
            if (stackRoot == null) stackRoot = transform;
        }

        public void SetCapacity(int cap) => capacity = cap;
        public void SetItemScale(Vector3 s) => itemScale = s;
        public void SetRowSize(int n) => rowSize = Mathf.Max(1, n);
        public void SetShowMaxLabel(bool v)
        {
            showMaxLabel = v;
            if (!v && _maxLabelGo != null && _maxLabelGo.activeSelf) _maxLabelGo.SetActive(false);
        }

        public bool TryAdd(CarryKind kind)
        {
            if (_items.Count == 0) _kind = kind;
            else if (_kind != kind) return false;
            if (IsFull) return false;

            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var col = block.GetComponent<Collider>();
            if (col != null) Destroy(col);
            block.transform.SetParent(stackRoot, false);
            block.transform.localScale = itemScale;

            int idx = _items.Count;
            int row = idx / rowSize;
            int col2 = idx % rowSize;
            float x = (col2 - (rowSize - 1) * 0.5f) * sideOffset;
            float y = row * itemHeight + itemHeight * 0.5f;
            Vector3 rest = new Vector3(x, y, 0f);
            block.transform.localPosition = rest;
            block.transform.localRotation = Quaternion.identity;

            var rend = block.GetComponent<Renderer>();
            if (rend != null) rend.sharedMaterial = GetMaterial(kind);
            block.name = $"Stack_{kind}_{idx}";

            _items.Add(block);
            _restLocal.Add(rest);
            OnChanged?.Invoke();
            StartCoroutine(PunchScale(block.transform, itemScale));
            UpdateMaxLabel();
            if (kind == CarryKind.Cash)
            {
                if (_lastCashPickupSfxFrame != Time.frameCount)
                {
                    _lastCashPickupSfxFrame = Time.frameCount;
                    SfxPlayer.PlayOneShot(GetPickupSfx(kind), Root.position, sfxVolume);
                }
            }
            else
            {
                SfxPlayer.PlayOneShot(GetPickupSfx(kind), Root.position, sfxVolume);
            }
            return true;
        }

        private AudioClip GetPickupSfx(CarryKind kind)
        {
            switch (kind)
            {
                case CarryKind.Rock: return rockPickupSfx;
                case CarryKind.Handcuff: return handcuffPickupSfx;
                case CarryKind.Cash: return cashPickupSfx;
            }
            return null;
        }

        private AudioClip GetPlaceSfx(CarryKind kind)
        {
            switch (kind)
            {
                case CarryKind.Rock: return rockPlaceSfx;
                case CarryKind.Handcuff: return handcuffPlaceSfx;
                case CarryKind.Cash: return cashPlaceSfx;
            }
            return null;
        }

        private System.Collections.IEnumerator PunchScale(Transform t, Vector3 baseScale)
        {
            const float dur = 0.18f;
            float u = 0f;
            while (u < 1f)
            {
                if (t == null) yield break;
                u += Time.deltaTime / dur;
                float k = Mathf.Clamp01(u);
                float s = 1f + 0.35f * Mathf.Sin(k * Mathf.PI);
                t.localScale = new Vector3(baseScale.x * s, baseScale.y * s, baseScale.z * s);
                yield return null;
            }
            if (t != null) t.localScale = baseScale;
        }

        public bool TryRemove()
        {
            if (_items.Count == 0) return false;
            int last = _items.Count - 1;
            var go = _items[last];
            _items.RemoveAt(last);
            if (last < _restLocal.Count) _restLocal.RemoveAt(last);
            if (go != null) Destroy(go);
            OnChanged?.Invoke();
            UpdateMaxLabel();
            SfxPlayer.PlayOneShot(GetPlaceSfx(_kind), Root.position, sfxVolume);
            return true;
        }

        public void Clear()
        {
            for (int i = 0; i < _items.Count; i++)
                if (_items[i] != null) Destroy(_items[i]);
            _items.Clear();
            _restLocal.Clear();
            OnChanged?.Invoke();
            UpdateMaxLabel();
        }

        private void LateUpdate()
        {
            if (!wobbleEnabled) return;
            var root = Root;
            if (root == null) return;

            // Only rocks and cash wobble.
            bool kindAllowed = (_kind == CarryKind.Cash || _kind == CarryKind.Rock);
            if (!kindAllowed || _items.Count == 0)
            {
                if (_sway.sqrMagnitude > 0f || _swayVel.sqrMagnitude > 0f)
                {
                    _sway = Vector2.zero;
                    _swayVel = Vector2.zero;
                    ApplyWobbleToItems();
                }
                _prevWorldPos = root.position;
                _prevWorldVel = Vector3.zero;
                _hasPrevPos = true;
                return;
            }

            float dt = Time.deltaTime;
            if (dt < 1e-5f) return;

            Vector3 worldPos = root.position;
            Vector3 rawVel = _hasPrevPos ? (worldPos - _prevWorldPos) / dt : Vector3.zero;
            // Reject teleport spikes.
            if (rawVel.magnitude > 50f) rawVel = Vector3.zero;
            Vector3 smoothVel = _hasPrevPos ? Vector3.Lerp(_prevWorldVel, rawVel, 0.25f) : rawVel;

            // Target sway: opposite to local-XZ motion, capped by leanScale * maxSpeed.
            Vector2 targetSway = Vector2.zero;
            if (_items.Count >= wobbleMinCount)
            {
                Vector3 localVel = root.InverseTransformDirection(smoothVel);
                Vector2 horiz = new Vector2(localVel.x, localVel.z);
                if (horiz.magnitude > wobbleVelMax) horiz = horiz.normalized * wobbleVelMax;
                targetSway = -horiz * wobbleLeanScale;
                if (targetSway.magnitude > wobbleMaxOffset)
                    targetSway = targetSway.normalized * wobbleMaxOffset;
            }

            // Damped spring pulled toward target.
            Vector2 disp = _sway - targetSway;
            Vector2 accel = -wobbleStiffness * disp - wobbleDamping * _swayVel;
            _swayVel += accel * dt;
            _sway += _swayVel * dt;
            if (_sway.magnitude > wobbleMaxOffset)
                _sway = _sway.normalized * wobbleMaxOffset;

            ApplyWobbleToItems();

            _prevWorldPos = worldPos;
            _prevWorldVel = smoothVel;
            _hasPrevPos = true;
        }

        private void ApplyWobbleToItems()
        {
            int n = Mathf.Min(_items.Count, _restLocal.Count);
            if (n == 0) return;

            bool active = _items.Count >= wobbleMinCount &&
                          (Mathf.Abs(_sway.x) > 1e-4f || Mathf.Abs(_sway.y) > 1e-4f ||
                           Mathf.Abs(_swayVel.x) > 1e-4f || Mathf.Abs(_swayVel.y) > 1e-4f);

            float topY = 0f;
            if (active)
            {
                for (int i = 0; i < n; i++) if (_restLocal[i].y > topY) topY = _restLocal[i].y;
                if (topY < 1e-4f) active = false;
            }

            for (int i = 0; i < n; i++)
            {
                var go = _items[i];
                if (go == null) continue;
                Vector3 rest = _restLocal[i];
                if (active)
                {
                    float h = Mathf.Clamp01(rest.y / topY);
                    Vector3 offset = new Vector3(_sway.x * h, 0f, _sway.y * h);
                    go.transform.localPosition = rest + offset;
                }
                else
                {
                    go.transform.localPosition = rest;
                }
            }
        }

        private void UpdateMaxLabel()
        {
            bool show = showMaxLabel && capacity > 0 && _items.Count >= capacity;
            if (!show)
            {
                if (_maxLabelGo != null && _maxLabelGo.activeSelf) _maxLabelGo.SetActive(false);
                return;
            }
            EnsureMaxLabel();
            float midY = (capacity * 0.5f / Mathf.Max(1, rowSize)) * itemHeight;
            _maxLabelGo.transform.localPosition = new Vector3(0f, midY, 0f);
            if (!_maxLabelGo.activeSelf) _maxLabelGo.SetActive(true);
        }

        private void EnsureMaxLabel()
        {
            if (_maxLabelGo != null) return;
            _maxLabelGo = new GameObject("_MaxLabel");
            _maxLabelGo.transform.SetParent(Root, false);
            _maxLabelGo.transform.localScale = Vector3.one * 0.35f;
            _maxLabel = _maxLabelGo.AddComponent<TextMesh>();
            _maxLabel.text = "MAX";
            _maxLabel.fontSize = 20;
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

        public Vector3 GetTopWorldPosition()
        {
            int idx = Mathf.Max(0, _items.Count - 1);
            int row = idx / rowSize;
            float y = row * itemHeight + itemHeight * 0.5f;
            return stackRoot.TransformPoint(new Vector3(0, y, 0));
        }

        private static Material _matRock, _matHandcuff, _matCash;
        private static Material GetMaterial(CarryKind kind)
        {
            switch (kind)
            {
                case CarryKind.Rock:
                    if (_matRock == null) _matRock = MakeMat(new Color(0.32f, 0.32f, 0.35f));
                    return _matRock;
                case CarryKind.Handcuff:
                    if (_matHandcuff == null) _matHandcuff = MakeMat(new Color(0.85f, 0.85f, 0.9f));
                    return _matHandcuff;
                case CarryKind.Cash:
                default:
                    if (_matCash == null) _matCash = MakeMat(new Color(0.25f, 0.75f, 0.35f));
                    return _matCash;
            }
        }

        private static Material MakeMat(Color c)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            m.color = c;
            return m;
        }
    }
}
