using System.Collections.Generic;
using PrisonLife.Core;
using UnityEngine;

namespace PrisonLife.Quarry
{
    /// <summary>
    /// Spawns a grid of rocks. After a rock is mined, respawns after N seconds.
    /// </summary>
    public class RockSpawner : MonoBehaviour
    {
        [SerializeField] private Rock rockPrefab;
        [SerializeField] private Vector2Int grid = new Vector2Int(5, 4);
        [SerializeField] private Vector2 spacing = new Vector2(1.4f, 1.4f);
        [SerializeField] private float respawnOverrideSeconds = -1f;

        private readonly List<Rock> _rocks = new();
        private readonly Dictionary<Rock, float> _respawnTimers = new();
        private readonly HashSet<Rock> _claimed = new();

        private void Start()
        {
            for (int x = 0; x < grid.x; x++)
            for (int z = 0; z < grid.y; z++)
            {
                Vector3 local = new Vector3((x - (grid.x - 1) * 0.5f) * spacing.x, 0f, (z - (grid.y - 1) * 0.5f) * spacing.y);
                var r = Instantiate(rockPrefab, transform);
                r.transform.localPosition = local;
                r.Bind(this);
                _rocks.Add(r);
            }
        }

        public IReadOnlyList<Rock> Rocks => _rocks;

        public Rock GetNearestAlive(Vector3 worldPos, float maxRange)
        {
            float best = maxRange * maxRange;
            Rock found = null;
            for (int i = 0; i < _rocks.Count; i++)
            {
                var r = _rocks[i];
                if (r == null || !r.IsAlive) continue;
                float d = (r.transform.position - worldPos).sqrMagnitude;
                if (d <= best) { best = d; found = r; }
            }
            return found;
        }

        /// <summary>
        /// Returns the nearest alive rock that isn't already claimed by another worker,
        /// and marks it as claimed. Caller must release via ReleaseClaim when done.
        /// </summary>
        public Rock ClaimNearestAlive(Vector3 worldPos, float maxRange)
        {
            float best = maxRange * maxRange;
            Rock found = null;
            for (int i = 0; i < _rocks.Count; i++)
            {
                var r = _rocks[i];
                if (r == null || !r.IsAlive) continue;
                if (_claimed.Contains(r)) continue;
                float d = (r.transform.position - worldPos).sqrMagnitude;
                if (d <= best) { best = d; found = r; }
            }
            if (found != null) _claimed.Add(found);
            return found;
        }

        public void ReleaseClaim(Rock r)
        {
            if (r != null) _claimed.Remove(r);
        }

        public void NotifyMined(Rock r)
        {
            if (r == null) return;
            r.gameObject.SetActive(false);
            _claimed.Remove(r);
            float wait = respawnOverrideSeconds > 0f ? respawnOverrideSeconds
                : (GameManager.Instance != null ? GameManager.Instance.Config.rockRespawnSeconds : 5f);
            _respawnTimers[r] = wait;
        }

        private void Update()
        {
            if (_respawnTimers.Count == 0) return;
            List<Rock> ready = null;
            var keys = new List<Rock>(_respawnTimers.Keys);
            foreach (var rk in keys)
            {
                _respawnTimers[rk] -= Time.deltaTime;
                if (_respawnTimers[rk] <= 0f)
                {
                    ready ??= new List<Rock>();
                    ready.Add(rk);
                }
            }
            if (ready != null)
            {
                foreach (var rk in ready)
                {
                    _respawnTimers.Remove(rk);
                    if (rk != null) rk.Bind(this);
                }
            }
        }
    }
}
