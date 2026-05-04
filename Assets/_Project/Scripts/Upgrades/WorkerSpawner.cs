using PrisonLife.Core;
using PrisonLife.Factory;
using PrisonLife.NPC;
using PrisonLife.Quarry;
using UnityEngine;

namespace PrisonLife.Upgrades
{
    /// <summary>
    /// On 'workers' upgrade purchase, spawns N prisoner workers at spawn points.
    /// </summary>
    public class WorkerSpawner : MonoBehaviour
    {
        [SerializeField] private WorkerNPC workerPrefab;
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private RockSpawner spawner;
        [SerializeField] private ProcessingMachine machine;

        private void Start()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            gm.OnUpgradeChanged += OnUpgradeChanged;
            if (gm.IsPurchased(UpgradeIds.Workers)) SpawnAll();
        }

        private void OnDestroy()
        {
            var gm = GameManager.Instance;
            if (gm != null) gm.OnUpgradeChanged -= OnUpgradeChanged;
        }

        private void OnUpgradeChanged(string id, bool purchased)
        {
            if (id == UpgradeIds.Workers && purchased) SpawnAll();
        }

        private void SpawnAll()
        {
            var gm = GameManager.Instance;
            int count = gm != null ? gm.Config.workerCountPerPurchase : 3;
            for (int i = 0; i < count; i++)
            {
                Transform p = spawnPoints != null && spawnPoints.Length > 0 ? spawnPoints[i % spawnPoints.Length] : transform;
                var w = Instantiate(workerPrefab, p.position, Quaternion.identity);
                w.Configure(spawner, machine);
            }
        }
    }
}
