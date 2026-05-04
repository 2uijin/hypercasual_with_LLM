using PrisonLife.Core;
using PrisonLife.Desk;
using PrisonLife.Factory;
using PrisonLife.NPC;
using UnityEngine;

namespace PrisonLife.Upgrades
{
    public class PoliceSpawner : MonoBehaviour
    {
        [SerializeField] private PoliceCarrierNPC policePrefab;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private HandcuffShelf shelf;
        [SerializeField] private Transform shelfStand;
        [SerializeField] private ProcessingDesk desk;
        [SerializeField] private Transform deskStand;

        private void Start()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            gm.OnUpgradeChanged += OnUpgradeChanged;
            if (gm.IsPurchased(UpgradeIds.Police)) Spawn();
        }

        private void OnDestroy()
        {
            var gm = GameManager.Instance;
            if (gm != null) gm.OnUpgradeChanged -= OnUpgradeChanged;
        }

        private void OnUpgradeChanged(string id, bool purchased)
        {
            if (id == UpgradeIds.Police && purchased) Spawn();
        }

        private void Spawn()
        {
            if (policePrefab == null || spawnPoint == null) return;
            var p = Instantiate(policePrefab, spawnPoint.position, Quaternion.identity);
            p.Configure(shelf, shelfStand, desk, deskStand);
            p.SetIdlePoint(spawnPoint);
        }
    }
}
