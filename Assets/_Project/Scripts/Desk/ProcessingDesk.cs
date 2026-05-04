using System.Collections.Generic;
using PrisonLife.Core;
using PrisonLife.Items;
using PrisonLife.Player;
using UnityEngine;

namespace PrisonLife.Desk
{
    /// <summary>
    /// Holds a handcuff pile on the table (deposited by player/police) and a cash pile (payments from civilians).
    /// Civilians are processed one at a time by CivilianQueue.
    /// </summary>
    public class ProcessingDesk : MonoBehaviour
    {
        [SerializeField] private Collider depositZone;
        [SerializeField] private Transform handcuffStackRoot;
        [SerializeField] private Transform cashStackRoot;
        [SerializeField] private Vector3 handcuffScale = new Vector3(0.35f, 0.12f, 0.25f);
        [SerializeField] private Vector3 cashScale = new Vector3(0.25f, 0.08f, 0.4f);

        private readonly List<GameObject> _handcuffs = new();
        private readonly List<GameObject> _cash = new();

        private readonly HashSet<CarryStack> _handcuffCarriers = new();
        private readonly HashSet<CarryStack> _cashPickers = new();

        private float _intakeTimer;
        private float _cashTimer;

        public int HandcuffCount => _handcuffs.Count;

        private void OnTriggerEnter(Collider other)
        {
            var p = other.GetComponentInParent<PlayerController>();
            if (p != null) { _handcuffCarriers.Add(p.HandcuffStack); return; }
            var police = other.GetComponentInParent<PrisonLife.NPC.PoliceCarrierNPC>();
            if (police != null) _handcuffCarriers.Add(police.HandcuffStack);
        }

        private void OnTriggerExit(Collider other)
        {
            var p = other.GetComponentInParent<PlayerController>();
            if (p != null) { _handcuffCarriers.Remove(p.HandcuffStack); return; }
            var police = other.GetComponentInParent<PrisonLife.NPC.PoliceCarrierNPC>();
            if (police != null) _handcuffCarriers.Remove(police.HandcuffStack);
        }

        private void Update()
        {
            var cfg = GameManager.Instance != null ? GameManager.Instance.Config : null;
            if (cfg == null) return;

            _intakeTimer -= Time.deltaTime;
            if (_intakeTimer <= 0f)
            {
                _intakeTimer = cfg.deskHandcuffDepositInterval;
                TryPullHandcuffFromCarrier();
            }
        }

        private void TryPullHandcuffFromCarrier()
        {
            foreach (var stack in _handcuffCarriers)
            {
                if (stack == null || stack.Count == 0 || stack.Kind != CarryKind.Handcuff) continue;
                if (stack.TryRemove())
                {
                    AddHandcuffVisual();
                    return;
                }
            }
        }

        public bool ConsumeHandcuffs(int n)
        {
            if (_handcuffs.Count < n) return false;
            for (int i = 0; i < n; i++)
            {
                int last = _handcuffs.Count - 1;
                var go = _handcuffs[last];
                _handcuffs.RemoveAt(last);
                if (go != null) Destroy(go);
            }
            return true;
        }

        public void AddCashVisual(int amount)
        {
            for (int i = 0; i < amount; i++) SpawnCashCube();
        }

        private void AddHandcuffVisual()
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            if (block.TryGetComponent<Collider>(out var col)) Destroy(col);
            block.transform.SetParent(handcuffStackRoot != null ? handcuffStackRoot : transform, false);
            block.transform.localScale = handcuffScale;
            int idx = _handcuffs.Count;
            block.transform.localPosition = new Vector3(((idx % 4) - 1.5f) * 0.28f, (idx / 4) * 0.14f + 0.07f, 0f);
            var rend = block.GetComponent<Renderer>();
            if (rend != null) rend.sharedMaterial = _matH ??= Make(new Color(0.85f, 0.85f, 0.9f));
            _handcuffs.Add(block);
        }

        private void SpawnCashCube()
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            if (block.TryGetComponent<Collider>(out var col)) Destroy(col);
            block.transform.SetParent(cashStackRoot != null ? cashStackRoot : transform, false);
            block.transform.localScale = cashScale;
            int idx = _cash.Count;
            block.transform.localPosition = new Vector3(((idx % 4) - 1.5f) * 0.22f, (idx / 4) * 0.09f + 0.045f, 0f);
            var rend = block.GetComponent<Renderer>();
            if (rend != null) rend.sharedMaterial = _matC ??= Make(new Color(0.25f, 0.75f, 0.35f));
            _cash.Add(block);
        }

        public int CashAvailable => _cash.Count;

        public int TakeCash(int max)
        {
            int taken = 0;
            while (taken < max && _cash.Count > 0)
            {
                int last = _cash.Count - 1;
                var go = _cash[last];
                _cash.RemoveAt(last);
                if (go != null) Destroy(go);
                taken++;
            }
            return taken;
        }

        private static Material _matH, _matC;
        private static Material Make(Color c)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            m.color = c; return m;
        }
    }
}
