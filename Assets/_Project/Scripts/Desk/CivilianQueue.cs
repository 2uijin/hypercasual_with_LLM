using System.Collections.Generic;
using PrisonLife.Core;
using PrisonLife.NPC;
using UnityEngine;

namespace PrisonLife.Desk
{
    /// <summary>
    /// Spawns civilians at an interval and manages a FIFO queue leading to the desk.
    /// The head civilian purchases handcuffs (drains from desk) and pays cash.
    /// </summary>
    public class CivilianQueue : MonoBehaviour
    {
        [SerializeField] private ProcessingDesk desk;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private Transform[] queueSlots;
        [SerializeField] private Transform processPoint; // head slot where purchase happens
        [SerializeField] private Transform[] exitPath;   // intermediate waypoints between desk and jail (order: first → last)
        [SerializeField] private PrisonLife.Jail.JailCell jail;
        [SerializeField] private CivilianNPC civilianPrefab;

        private readonly List<CivilianNPC> _queue = new();
        private float _spawnTimer;
        private float _purchaseTimer;

        private void Update()
        {
            var cfg = GameManager.Instance != null ? GameManager.Instance.Config : null;
            if (cfg == null) return;

            _spawnTimer -= Time.deltaTime;
            if (_spawnTimer <= 0f && _queue.Count < queueSlots.Length)
            {
                _spawnTimer = cfg.civilianSpawnInterval;
                SpawnCivilian(cfg);
            }

            for (int i = 0; i < _queue.Count; i++)
            {
                var c = _queue[i];
                if (c == null) continue;
                Transform slot = i == 0 ? processPoint : queueSlots[Mathf.Min(i, queueSlots.Length - 1)];
                c.MoveTo(slot.position);
                c.SetRequirementVisible(i == 0);
            }

            TryProcessHead(cfg);
        }

        private void SpawnCivilian(PrisonLife.Config.GameConfig cfg)
        {
            if (civilianPrefab == null || spawnPoint == null) return;
            var c = Instantiate(civilianPrefab, spawnPoint.position, Quaternion.identity);
            int min = Mathf.Max(1, cfg.handcuffsPerCivilianMin);
            int max = Mathf.Max(min, cfg.handcuffsPerCivilianMax);
            c.SetHandcuffsRequired(Random.Range(min, max + 1));
            _queue.Add(c);
        }

        private Vector3[] BuildExitPath()
        {
            if (exitPath == null || exitPath.Length == 0) return null;
            int valid = 0;
            for (int i = 0; i < exitPath.Length; i++) if (exitPath[i] != null) valid++;
            if (valid == 0) return null;
            var result = new Vector3[valid];
            int k = 0;
            for (int i = 0; i < exitPath.Length; i++)
                if (exitPath[i] != null) result[k++] = exitPath[i].position;
            return result;
        }

        private void TryProcessHead(PrisonLife.Config.GameConfig cfg)
        {
            if (_queue.Count == 0 || desk == null) return;
            var head = _queue[0];
            if (head == null) { _queue.RemoveAt(0); return; }
            if (!head.AtDestination) return;

            int required = head.HandcuffsRequired;
            if (desk.HandcuffCount < required) return;
            // Don't take handcuffs / pay / convert if jail can't accept (full + outside-wait at capacity).
            if (jail != null && !jail.CanAccept) return;
            _purchaseTimer -= Time.deltaTime;
            if (_purchaseTimer > 0f) return;
            _purchaseTimer = cfg.civilianPurchaseDuration;

            if (desk.ConsumeHandcuffs(required))
            {
                desk.AddCashVisual(required * cfg.cashPerHandcuff);
                head.ConvertToPrisoner();
                _queue.RemoveAt(0);

                if (jail != null)
                {
                    Vector3[] path = BuildExitPath();
                    jail.EnqueuePrisoner(head, path);
                }
                else
                {
                    Destroy(head.gameObject, 10f);
                }
            }
        }
    }
}
