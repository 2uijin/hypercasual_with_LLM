using System.Collections.Generic;
using PrisonLife.Core;
using PrisonLife.FX;
using PrisonLife.Items;
using PrisonLife.Player;
using UnityEngine;

namespace PrisonLife.Quarry
{
    /// <summary>
    /// Attach to Player. While player is close to live rocks, mines at interval.
    /// Vehicle mode: mines up to 4 nearest rocks per cycle.
    /// </summary>
    public class MiningTool : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private RockSpawner spawner;
        [SerializeField] private BoxCollider miningCollider;
        [SerializeField] private Transform sparkOrigin;
        [SerializeField] private ParticleSystem sparkFx;
        [SerializeField] private int vehicleBatchSize = 4;
        [SerializeField] private int drillBatchSize = 2;

        [Header("Mining box size by mode (multiplies base size)")]
        [SerializeField] private float pickaxeBoxMultiplier = 1f;
        [SerializeField] private float drillBoxMultiplier = 1.5f;
        [SerializeField] private float vehicleBoxMultiplier = 2.2f;

        [Header("Flight to player stack")]
        [SerializeField] private float flightDuration = 0.42f;
        [SerializeField] private float flightArcHeight = 1.4f;
        [SerializeField] private Vector3 flightTargetLocalOffset = new Vector3(0f, 0.05f, 0f);

        private float _cooldown;
        private readonly List<Rock> _buffer = new();

        private ParticleSystem _debrisFx;
        private Transform _debrisAnchor;

        private float _activeLinger;
        public bool IsMining => _activeLinger > 0f;

        private Vector3 _baseBoxSize;

        private void Start()
        {
            if (sparkFx == null && sparkOrigin != null)
                sparkFx = FxSpawner.CreateSpark(sparkOrigin, Vector3.zero);
            _debrisAnchor = sparkOrigin != null ? sparkOrigin : transform;
            _debrisFx = FxSpawner.CreateDebris(_debrisAnchor, Vector3.zero, 1f);
            if (miningCollider != null) _baseBoxSize = miningCollider.size;
        }

        private void Update()
        {
            if (player == null || spawner == null || miningCollider == null) return;
            if (_cooldown > 0f) _cooldown -= Time.deltaTime;
            if (_activeLinger > 0f) _activeLinger -= Time.deltaTime;

            var cfg = GameManager.Instance != null ? GameManager.Instance.Config : null;
            if (cfg == null) return;

            float interval = cfg.miningIntervalPickaxe;
            bool riding = player.IsRiding;
            if (riding) interval = cfg.miningIntervalVehicle;
            else if (player.HasDrill) interval = cfg.miningIntervalDrill;

            float boxMul = riding ? vehicleBoxMultiplier : (player.HasDrill ? drillBoxMultiplier : pickaxeBoxMultiplier);
            miningCollider.size = _baseBoxSize * boxMul;

            if (_cooldown > 0f) return;

            int batch = riding ? vehicleBatchSize : (player.HasDrill ? drillBatchSize : 1);
            int mined = MineBatch(batch);
            if (mined > 0)
            {
                _cooldown = interval;
                _activeLinger = Mathf.Max(_activeLinger, interval * 1.15f + 0.05f);
                if (sparkFx != null && sparkOrigin != null)
                {
                    sparkFx.transform.position = sparkOrigin.position;
                    sparkFx.Emit(6 * mined);
                }
                if (_debrisFx != null && _debrisAnchor != null)
                {
                    float scale = riding ? 2.5f : (player.HasDrill ? 1f : 0.7f);
                    int count = Mathf.RoundToInt((riding ? 28 : (player.HasDrill ? 14 : 8)) * mined);
                    _debrisFx.transform.position = _debrisAnchor.position;
                    var main = _debrisFx.main;
                    main.startSize = new ParticleSystem.MinMaxCurve(0.06f * scale, 0.12f * scale);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(2f * scale, 5f * scale);
                    _debrisFx.Emit(count);
                }
            }
        }

        private bool IsInsideMiningBox(Vector3 worldPos)
        {
            Vector3 local = miningCollider.transform.InverseTransformPoint(worldPos);
            Vector3 c = miningCollider.center;
            Vector3 hs = miningCollider.size * 0.5f;
            return Mathf.Abs(local.x - c.x) <= hs.x
                && Mathf.Abs(local.y - c.y) <= hs.y
                && Mathf.Abs(local.z - c.z) <= hs.z;
        }

        private int MineBatch(int wanted)
        {
            _buffer.Clear();
            var rocks = spawner.Rocks;
            Vector3 p = miningCollider.bounds.center;
            for (int i = 0; i < rocks.Count; i++)
            {
                var r = rocks[i];
                if (r == null || !r.IsAlive) continue;
                if (IsInsideMiningBox(r.transform.position)) _buffer.Add(r);
            }
            if (_buffer.Count == 0) return 0;
            _buffer.Sort((a, b) => (a.transform.position - p).sqrMagnitude.CompareTo((b.transform.position - p).sqrMagnitude));

            int count = 0;
            var stack = player.RockStack;
            for (int i = 0; i < _buffer.Count && count < wanted; i++)
            {
                var r = _buffer[i];
                if (r == null || !r.IsAlive) continue;
                Vector3 spawnPos = r.transform.position + Vector3.up * 0.4f;
                r.Mine();
                if (!stack.IsFull)
                {
                    stack.Reserve();
                    FlyingPickup.Spawn(spawnPos, stack.Root, flightTargetLocalOffset, CarryKind.Rock,
                        flightDuration, flightArcHeight,
                        () => { stack.Release(); stack.TryAdd(CarryKind.Rock); });
                }
                count++;
            }
            return count;
        }
    }
}
