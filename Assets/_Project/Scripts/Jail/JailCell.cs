using System.Collections;
using System.Collections.Generic;
using PrisonLife.Core;
using PrisonLife.FX;
using PrisonLife.NPC;
using UnityEngine;

namespace PrisonLife.Jail
{
    /// <summary>
    /// Capacity-managed jail. Expansion doubles capacity.
    /// </summary>
    public class JailCell : MonoBehaviour
    {
        [SerializeField] private Transform entryPoint;
        [SerializeField] private Transform[] cellSlots;
        [SerializeField] private ParticleSystem confettiFx;
        [SerializeField] private GameObject[] extraBeds; // activated (with scale-up animation) on expansion
        [SerializeField] private GameObject[] expansionStructures; // extra props revealed on expansion (scale-in)
        [SerializeField] private Transform[] stretchZTargets; // floor + side walls — z-scale doubles, anchored at entrance (front)
        [SerializeField] private Transform[] shiftBackZTargets; // back wall + back pillars — moved backward by base depth
        [SerializeField] private float expansionAnimDuration = 1.1f;
        [SerializeField] private float expansionStagger = 0.03f;
        [SerializeField] private Transform waitingOutsidePoint;
        [SerializeField] private int waitingOutsideCapacity = 3;

        private int _capacity;
        private readonly List<CivilianNPC> _prisoners = new();
        private readonly Queue<CivilianNPC> _waiting = new();
        private Vector3[] _stretchOrigScale;
        private Vector3[] _stretchOrigPos;
        private Vector3[] _shiftBackOrigPos;
        private float _baseDepthZ;

        public int Capacity => _capacity;
        public int Count => _prisoners.Count;
        public bool IsFull => _prisoners.Count >= _capacity;
        public int WaitingOutsideCount => _waiting.Count;
        public int WaitingOutsideCapacity => waitingOutsideCapacity;
        // True when there's room inside, OR there's still slack in the outside-wait buffer.
        public bool CanAccept => !IsFull || _waiting.Count < waitingOutsideCapacity;

        public event System.Action<int, int> OnCapacityChanged; // current, cap

        private void Start()
        {
            var gm = GameManager.Instance;
            _capacity = gm != null ? gm.Config.jailBaseCapacity : 20;
            CacheExpansionTransforms();
            if (extraBeds != null)
                foreach (var b in extraBeds) if (b != null) { b.transform.localScale = Vector3.zero; b.SetActive(false); }
            if (expansionStructures != null)
                foreach (var s in expansionStructures) if (s != null) { s.transform.localScale = Vector3.zero; s.SetActive(false); }
            if (gm != null)
            {
                gm.OnUpgradeChanged += OnUpgradeChanged;
                if (gm.IsPurchased(UpgradeIds.JailExpand)) Expand(false);
            }
            if (confettiFx == null)
                confettiFx = FxSpawner.CreateConfetti(transform, new Vector3(0f, 2.5f, 0f));
            OnCapacityChanged?.Invoke(_prisoners.Count, _capacity);
        }

        private void CacheExpansionTransforms()
        {
            if (stretchZTargets != null)
            {
                _stretchOrigScale = new Vector3[stretchZTargets.Length];
                _stretchOrigPos = new Vector3[stretchZTargets.Length];
                for (int i = 0; i < stretchZTargets.Length; i++)
                {
                    var tr = stretchZTargets[i];
                    if (tr != null) { _stretchOrigScale[i] = tr.localScale; _stretchOrigPos[i] = tr.localPosition; }
                }
                if (stretchZTargets.Length > 0 && stretchZTargets[0] != null)
                    _baseDepthZ = stretchZTargets[0].localScale.z;
            }
            if (shiftBackZTargets != null)
            {
                _shiftBackOrigPos = new Vector3[shiftBackZTargets.Length];
                for (int i = 0; i < shiftBackZTargets.Length; i++)
                {
                    var tr = shiftBackZTargets[i];
                    if (tr != null) _shiftBackOrigPos[i] = tr.localPosition;
                }
            }
            if (_baseDepthZ <= 0f) _baseDepthZ = 6f;
        }

        private void OnDestroy()
        {
            var gm = GameManager.Instance;
            if (gm != null) gm.OnUpgradeChanged -= OnUpgradeChanged;
        }

        private void OnUpgradeChanged(string id, bool purchased)
        {
            if (id == UpgradeIds.JailExpand && purchased) Expand(true);
        }

        private void Expand(bool withFx)
        {
            var gm = GameManager.Instance;
            _capacity = gm != null ? gm.Config.jailExpandedCapacity : 40;

            if (withFx)
            {
                if (confettiFx == null)
                    confettiFx = FxSpawner.CreateConfetti(transform, new Vector3(0f, 2.5f, 0f));
                confettiFx.Play();
                if (isActiveAndEnabled) StartCoroutine(AnimateExpansion());
                else ActivateExpansionInstant();
            }
            else
            {
                ActivateExpansionInstant();
            }

            // Try to admit any waiting prisoners.
            while (_waiting.Count > 0 && !IsFull)
                AdmitFromWaiting();
            OnCapacityChanged?.Invoke(_prisoners.Count, _capacity);
        }

        private void ActivateExpansionInstant()
        {
            if (extraBeds != null)
                foreach (var b in extraBeds) if (b != null) { b.SetActive(true); b.transform.localScale = Vector3.one; }
            if (expansionStructures != null)
                foreach (var s in expansionStructures) if (s != null) { s.SetActive(true); s.transform.localScale = Vector3.one; }
            ApplyStretchShift(1f);
        }

        // p in [0,1] — interpolation factor between original and expanded layout.
        private void ApplyStretchShift(float p)
        {
            if (stretchZTargets != null && _stretchOrigScale != null)
            {
                for (int i = 0; i < stretchZTargets.Length; i++)
                {
                    var tr = stretchZTargets[i];
                    if (tr == null) continue;
                    float origZ = _stretchOrigScale[i].z;
                    float origPosZ = _stretchOrigPos[i].z;
                    var sc = tr.localScale; sc.z = Mathf.Lerp(origZ, origZ * 2f, p); tr.localScale = sc;
                    var pos = tr.localPosition; pos.z = Mathf.Lerp(origPosZ, origPosZ - origZ * 0.5f, p); tr.localPosition = pos;
                }
            }
            if (shiftBackZTargets != null && _shiftBackOrigPos != null)
            {
                for (int i = 0; i < shiftBackZTargets.Length; i++)
                {
                    var tr = shiftBackZTargets[i];
                    if (tr == null) continue;
                    float origPosZ = _shiftBackOrigPos[i].z;
                    var pos = tr.localPosition; pos.z = Mathf.Lerp(origPosZ, origPosZ - _baseDepthZ, p); tr.localPosition = pos;
                }
            }
        }

        private IEnumerator AnimateExpansion()
        {
            // Scale-in targets (extra beds + expansion props).
            var scaleIn = new List<Transform>();
            if (expansionStructures != null)
                foreach (var s in expansionStructures) if (s != null) { s.SetActive(true); scaleIn.Add(s.transform); }
            if (extraBeds != null)
                foreach (var b in extraBeds) if (b != null) { b.SetActive(true); scaleIn.Add(b.transform); }

            float dur = Mathf.Max(0.1f, expansionAnimDuration);
            float totalSpan = dur + expansionStagger * scaleIn.Count;
            float t = 0f;
            while (t < totalSpan)
            {
                t += Time.deltaTime;
                float gT = Mathf.Clamp01(t / dur);
                ApplyStretchShift(EaseOutBack(gT));
                for (int i = 0; i < scaleIn.Count; i++)
                {
                    var tr = scaleIn[i];
                    if (tr == null) continue;
                    float localT = Mathf.Clamp01((t - i * expansionStagger) / dur);
                    float s = EaseOutBack(localT);
                    tr.localScale = new Vector3(s, s, s);
                }
                yield return null;
            }
            ApplyStretchShift(1f);
            for (int i = 0; i < scaleIn.Count; i++)
                if (scaleIn[i] != null) scaleIn[i].localScale = Vector3.one;
        }

        private static float EaseOutBack(float k)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float k1 = k - 1f;
            return 1f + c3 * k1 * k1 * k1 + c1 * k1 * k1;
        }

        public void EnqueuePrisoner(CivilianNPC p, Vector3[] approachPath)
        {
            if (p == null) return;
            if (IsFull)
            {
                _waiting.Enqueue(p);
                Vector3 lastApproach = approachPath != null && approachPath.Length > 0
                    ? approachPath[approachPath.Length - 1]
                    : transform.position;
                Vector3 wait = waitingOutsidePoint != null ? waitingOutsidePoint.position : lastApproach;
                wait += Random.insideUnitSphere * 0.8f; wait.y = lastApproach.y;
                if (approachPath != null && approachPath.Length > 0)
                {
                    var full = new Vector3[approachPath.Length + 1];
                    System.Array.Copy(approachPath, full, approachPath.Length);
                    full[approachPath.Length] = wait;
                    p.SetPath(full);
                }
                else
                {
                    p.SetTarget(wait);
                }
                return;
            }
            AdmitPrisoner(p, approachPath);
        }

        private void AdmitPrisoner(CivilianNPC p, Vector3[] approachPath)
        {
            int idx = _prisoners.Count;
            Transform slot = cellSlots != null && idx < cellSlots.Length ? cellSlots[idx] : entryPoint;
            Vector3 final = slot != null ? slot.position : transform.position;
            if (approachPath != null && approachPath.Length > 0)
            {
                var full = new Vector3[approachPath.Length + 1];
                System.Array.Copy(approachPath, full, approachPath.Length);
                full[approachPath.Length] = final;
                p.SetPath(full);
            }
            else
            {
                p.SetTarget(final);
            }
            _prisoners.Add(p);
            if (slot != null)
            {
                var sparkle = FxSpawner.CreateSparkle(slot, new Vector3(0f, 1.2f, 0f));
                sparkle.Play();
                Object.Destroy(sparkle.gameObject, 1.2f);
            }
            OnCapacityChanged?.Invoke(_prisoners.Count, _capacity);
        }

        private void AdmitFromWaiting()
        {
            if (_waiting.Count == 0 || IsFull) return;
            var p = _waiting.Dequeue();
            if (p != null) AdmitPrisoner(p, null);
        }
    }
}
