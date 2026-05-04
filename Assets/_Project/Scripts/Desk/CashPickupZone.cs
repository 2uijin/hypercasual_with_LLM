using PrisonLife.Core;
using PrisonLife.Items;
using PrisonLife.Player;
using UnityEngine;

namespace PrisonLife.Desk
{
    /// <summary>
    /// When the player stands on the desk cash pile, transfers cash cubes from desk to player stack → adds cash to GameManager on each pickup.
    /// </summary>
    public class CashPickupZone : MonoBehaviour
    {
        [SerializeField] private ProcessingDesk desk;
        [SerializeField] private int cashPerCube = 1;

        private PlayerController _player;

        private void OnTriggerEnter(Collider other)
        {
            var p = other.GetComponentInParent<PlayerController>();
            if (p != null) { _player = p; DrainAll(); }
        }

        private void OnTriggerExit(Collider other)
        {
            var p = other.GetComponentInParent<PlayerController>();
            if (p == _player) _player = null;
        }

        private void Update()
        {
            if (_player == null || desk == null || desk.CashAvailable <= 0) return;
            DrainAll();
        }

        private void DrainAll()
        {
            if (_player == null || desk == null) return;
            int available = desk.CashAvailable;
            if (available <= 0) return;
            int taken = desk.TakeCash(available);
            if (taken <= 0) return;
            for (int i = 0; i < taken; i++) _player.CashStack.TryAdd(CarryKind.Cash);
            if (GameManager.Instance != null)
                GameManager.Instance.AddCash(taken * cashPerCube);
        }
    }
}
