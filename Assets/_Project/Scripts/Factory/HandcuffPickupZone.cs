using PrisonLife.Core;
using PrisonLife.Items;
using PrisonLife.Player;
using UnityEngine;

namespace PrisonLife.Factory
{
    /// <summary>
    /// When a carrier stands near the shelf, moves handcuffs from shelf -> carrier's handcuff stack at a rate.
    /// </summary>
    public class HandcuffPickupZone : MonoBehaviour
    {
        [SerializeField] private HandcuffShelf shelf;
        [SerializeField] private float pickupInterval = 0.1f;

        private readonly System.Collections.Generic.HashSet<CarryStack> _stacks = new();
        private float _timer;

        private void Reset()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            var p = other.GetComponentInParent<PlayerController>();
            if (p != null) { _stacks.Add(p.HandcuffStack); return; }
            var npcCarrier = other.GetComponentInParent<PrisonLife.NPC.PoliceCarrierNPC>();
            if (npcCarrier != null) _stacks.Add(npcCarrier.HandcuffStack);
        }

        private void OnTriggerExit(Collider other)
        {
            var p = other.GetComponentInParent<PlayerController>();
            if (p != null) { _stacks.Remove(p.HandcuffStack); return; }
            var npcCarrier = other.GetComponentInParent<PrisonLife.NPC.PoliceCarrierNPC>();
            if (npcCarrier != null) _stacks.Remove(npcCarrier.HandcuffStack);
        }

        private void Update()
        {
            if (shelf == null || !shelf.HasAny) return;
            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            _timer = pickupInterval;

            foreach (var stack in _stacks)
            {
                if (stack == null || stack.IsFull) continue;
                if (stack.Kind != CarryKind.Handcuff && stack.Count > 0) continue;
                if (shelf.TryRemove())
                {
                    stack.TryAdd(CarryKind.Handcuff);
                    return;
                }
            }
        }
    }
}
