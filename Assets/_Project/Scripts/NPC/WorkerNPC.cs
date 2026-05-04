using PrisonLife.Core;
using PrisonLife.Factory;
using PrisonLife.FX;
using PrisonLife.Quarry;
using UnityEngine;

namespace PrisonLife.NPC
{
    /// <summary>
    /// Prisoner worker that mines rocks endlessly. Mined rocks are fed directly into
    /// the processing machine — the worker never carries anything. Each of N workers
    /// claims a different rock via RockSpawner.
    /// </summary>
    public class WorkerNPC : BaseNPC
    {
        public enum State { SearchRock, GoToRock, Mining, WaitingNoRock }

        [SerializeField] private RockSpawner spawner;
        [SerializeField] private ProcessingMachine machine;
        [SerializeField] private BoxCollider miningCollider;
        [SerializeField] private float idleRespawnCheck = 1.0f;

        [Header("Pickaxe visual & swing")]
        [SerializeField] private GameObject pickaxePrefab;
        [SerializeField] private Transform pickaxeAttachRoot;
        [SerializeField] private Vector3 pickaxeHoldLocalPosition = new Vector3(0.45f, 1.0f, 0.15f);
        [SerializeField] private Vector3 pickaxeHoldLocalEuler = Vector3.zero;
        [SerializeField] private float pickaxePeriod = 0.7f;
        [SerializeField] private float pickaxeRaiseAngle = -35f;
        [SerializeField] private float pickaxeStrikeAngle = 55f;
        [SerializeField, Range(0.1f, 0.9f)] private float pickaxeStrikeFraction = 0.32f;

        [Header("SFX")]
        [SerializeField] private AudioClip pickaxeSwingSfx;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;

        [Header("Runtime (read-only)")]
        [SerializeField] private State _state = State.SearchRock;
        private Rock _targetRock;
        private float _mineTimer;
        private float _idleTimer;
        private Transform _pickaxe;
        private float _swingT;
        private int _lastSwingCycle = -1;

        public State CurrentState => _state;

        public void Configure(RockSpawner s, ProcessingMachine m)
        {
            spawner = s;
            machine = m;
        }

        private void Start()
        {
            var gm = GameManager.Instance;
            if (gm != null) moveSpeed = gm.Config.npcMoveSpeed;

            if (pickaxePrefab != null)
            {
                var root = pickaxeAttachRoot != null ? pickaxeAttachRoot : (model != null ? model : transform);
                var go = Instantiate(pickaxePrefab, root);
                go.name = "PickaxeVisual";
                _pickaxe = go.transform;
                _pickaxe.localPosition = pickaxeHoldLocalPosition;
                _pickaxe.localRotation = Quaternion.Euler(pickaxeHoldLocalEuler);
                go.SetActive(false);
            }
        }

        private void OnDisable()
        {
            if (spawner != null && _targetRock != null) spawner.ReleaseClaim(_targetRock);
            _targetRock = null;
        }

        private bool IsRockInMiningBox(Rock r)
        {
            if (r == null || miningCollider == null) return false;
            Vector3 local = miningCollider.transform.InverseTransformPoint(r.transform.position);
            Vector3 c = miningCollider.center;
            Vector3 hs = miningCollider.size * 0.5f;
            return Mathf.Abs(local.x - c.x) <= hs.x
                && Mathf.Abs(local.y - c.y) <= hs.y
                && Mathf.Abs(local.z - c.z) <= hs.z;
        }

        protected override void Update()
        {
            var cfg = GameManager.Instance != null ? GameManager.Instance.Config : null;

            switch (_state)
            {
                case State.SearchRock:
                    _targetRock = spawner != null ? spawner.ClaimNearestAlive(transform.position, 100f) : null;
                    if (_targetRock != null)
                    {
                        SetTarget(_targetRock.transform.position);
                        _state = State.GoToRock;
                    }
                    else
                    {
                        _idleTimer = idleRespawnCheck;
                        _state = State.WaitingNoRock;
                    }
                    break;

                case State.WaitingNoRock:
                    _idleTimer -= Time.deltaTime;
                    if (_idleTimer <= 0f) _state = State.SearchRock;
                    break;

                case State.GoToRock:
                    if (_targetRock == null || !_targetRock.IsAlive)
                    {
                        if (_targetRock != null) spawner?.ReleaseClaim(_targetRock);
                        _targetRock = null;
                        _state = State.SearchRock;
                        break;
                    }
                    if (IsRockInMiningBox(_targetRock))
                    {
                        _hasTarget = false;
                        _state = State.Mining;
                        _mineTimer = cfg != null ? cfg.workerMiningInterval : 0.9f;
                    }
                    else
                    {
                        base.Update();
                        if (AtDestination)
                        {
                            _state = State.Mining;
                            _mineTimer = cfg != null ? cfg.workerMiningInterval : 0.9f;
                        }
                    }
                    break;

                case State.Mining:
                    _mineTimer -= Time.deltaTime;
                    if (_mineTimer <= 0f)
                    {
                        if (_targetRock != null && _targetRock.IsAlive)
                        {
                            _targetRock.Mine(); // NotifyMined releases the claim
                            if (machine != null) machine.AcceptRock(1);
                        }
                        else if (_targetRock != null) spawner?.ReleaseClaim(_targetRock);
                        _targetRock = null;
                        _state = State.SearchRock;
                    }
                    break;
            }

            UpdatePickaxeVisual();
        }

        private void UpdatePickaxeVisual()
        {
            if (_pickaxe == null) return;
            bool show = _state == State.Mining;
            if (_pickaxe.gameObject.activeSelf != show) _pickaxe.gameObject.SetActive(show);
            if (!show) { _swingT = 0f; _lastSwingCycle = -1; return; }

            _swingT += Time.deltaTime;
            float period = Mathf.Max(0.05f, pickaxePeriod);
            float phase = _swingT / period;
            int cycle = (int)phase;
            float u = phase - cycle;
            float strikeFrac = Mathf.Clamp(pickaxeStrikeFraction, 0.05f, 0.95f);
            float angle;
            if (u < strikeFrac)
                angle = Mathf.Lerp(pickaxeRaiseAngle, pickaxeStrikeAngle, EaseOutQuad(u / strikeFrac));
            else
                angle = Mathf.Lerp(pickaxeStrikeAngle, pickaxeRaiseAngle, EaseInOutQuad((u - strikeFrac) / (1f - strikeFrac)));
            _pickaxe.localPosition = pickaxeHoldLocalPosition;
            _pickaxe.localRotation = Quaternion.Euler(angle, pickaxeHoldLocalEuler.y, pickaxeHoldLocalEuler.z);

            if (u >= strikeFrac && cycle != _lastSwingCycle)
            {
                _lastSwingCycle = cycle;
                SfxPlayer.PlayOneShot(pickaxeSwingSfx, _pickaxe.position, sfxVolume);
            }
        }

        private static float EaseOutQuad(float k) { return 1f - (1f - k) * (1f - k); }
        private static float EaseInOutQuad(float k) { return k < 0.5f ? 2f * k * k : 1f - Mathf.Pow(-2f * k + 2f, 2f) * 0.5f; }
    }
}
