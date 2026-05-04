using PrisonLife.Core;
using PrisonLife.Jail;
using UnityEngine;

namespace PrisonLife.Upgrades
{
    /// <summary>
    /// Lives on an always-active sibling GameObject. Watches an unlock condition and, when met,
    /// calls SetActive(true) on the target BuyPad GameObject (which starts disabled in the scene).
    /// Destroys itself once it fires — a pad, once unlocked, never re-locks.
    /// </summary>
    public class BuyPadUnlocker : MonoBehaviour
    {
        public enum UnlockCondition
        {
            Immediate,
            OnFirstCash,
            OnPrerequisitePurchased,
            OnJailFull,
        }

        [SerializeField] private GameObject target;
        [SerializeField] private UnlockCondition condition = UnlockCondition.Immediate;
        [SerializeField] private string prerequisiteId;
        [SerializeField] private JailCell jailRef;

        public void Configure(GameObject t, UnlockCondition c, string prereq, JailCell jail)
        {
            target = t;
            condition = c;
            prerequisiteId = prereq;
            jailRef = jail;
        }

        private bool _subscribed;

        private void Start()
        {
            var gm = GameManager.Instance;

            // If the target's upgrade is already purchased, nothing to unlock.
            if (target != null)
            {
                var pad = target.GetComponent<BuyPad>();
                if (pad != null && gm != null && gm.IsPurchased(pad.UpgradeId))
                {
                    Destroy(gameObject);
                    return;
                }
            }

            if (EvaluateNow())
            {
                Activate();
                return;
            }

            if (gm != null)
            {
                if (condition == UnlockCondition.OnFirstCash) gm.OnCashChanged += OnCash;
                if (condition == UnlockCondition.OnPrerequisitePurchased) gm.OnUpgradeChanged += OnUpgrade;
            }
            if (condition == UnlockCondition.OnJailFull && jailRef != null)
                jailRef.OnCapacityChanged += OnJailCap;
            _subscribed = true;
        }

        private void OnDestroy()
        {
            if (!_subscribed) return;
            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.OnCashChanged -= OnCash;
                gm.OnUpgradeChanged -= OnUpgrade;
            }
            if (jailRef != null) jailRef.OnCapacityChanged -= OnJailCap;
        }

        private bool EvaluateNow()
        {
            var gm = GameManager.Instance;
            switch (condition)
            {
                case UnlockCondition.Immediate: return true;
                case UnlockCondition.OnFirstCash: return gm != null && gm.Cash > 0;
                case UnlockCondition.OnPrerequisitePurchased:
                    return string.IsNullOrEmpty(prerequisiteId) || (gm != null && gm.IsPurchased(prerequisiteId));
                case UnlockCondition.OnJailFull: 
                    return jailRef != null && (jailRef.Count >= 20);
            }
            return false;
        }

        private void OnCash(int cash) { if (cash > 0) Activate(); }
        private void OnUpgrade(string id, bool purchased) { if (purchased && id == prerequisiteId) Activate(); }
        private void OnJailCap(int cur, int cap) { if (cur >= cap) Activate(); }

        private void Activate()
        {
            if (target != null) target.SetActive(true);
            Destroy(gameObject);
        }
    }
}
