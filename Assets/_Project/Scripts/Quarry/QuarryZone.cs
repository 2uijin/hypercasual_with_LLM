using PrisonLife.Player;
using UnityEngine;

namespace PrisonLife.Quarry
{
    [RequireComponent(typeof(Collider))]
    public class QuarryZone : MonoBehaviour
    {
        public bool IsPlayerInside { get; private set; }
        public PlayerController Player { get; private set; }

        private void Reset()
        {
            var c = GetComponent<Collider>();
            if (c != null) c.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            var p = other.GetComponentInParent<PlayerController>();
            if (p == null) return;
            IsPlayerInside = true;
            Player = p;
            p.SetInQuarry(true);
        }

        private void OnTriggerExit(Collider other)
        {
            var p = other.GetComponentInParent<PlayerController>();
            if (p == null) return;
            if (p == Player)
            {
                IsPlayerInside = false;
                Player = null;
                p.SetInQuarry(false);
            }
        }
    }
}
