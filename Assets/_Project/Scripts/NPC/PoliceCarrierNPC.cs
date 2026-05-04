using PrisonLife.Core;
using PrisonLife.Factory;
using PrisonLife.Items;
using PrisonLife.Desk;
using PrisonLife.Jail;
using PrisonLife.UI;
using TMPro;
using UnityEngine;

namespace PrisonLife.NPC
{
    /// <summary>
    /// Police NPC that ferries handcuffs from the HandcuffShelf to the ProcessingDesk.
    /// </summary>
    public class PoliceCarrierNPC : BaseNPC
    {
        public enum State { Idle, GoToShelf, PickingUp, GoToDesk, Depositing }

        [SerializeField] private CarryStack stack;
        [SerializeField] private HandcuffShelf shelf;
        [SerializeField] private Transform shelfStandPoint;
        [SerializeField] private ProcessingDesk desk;
        [SerializeField] private Transform deskStandPoint;
        [SerializeField] private int carryCap = 6;

        [SerializeField] private Transform idlePoint;
        [SerializeField] private float idlePollInterval = 0.5f;

        [Header("Runtime (read-only)")]
        [SerializeField] private State _state = State.Idle;
        private float _idleTimer;

        public State CurrentState => _state;

        public void SetIdlePoint(Transform pt) { idlePoint = pt; }

        public CarryStack HandcuffStack => stack;

        public void Configure(HandcuffShelf s, Transform shelfStand, ProcessingDesk d, Transform deskStand)
        {
            shelf = s; shelfStandPoint = shelfStand; desk = d; deskStandPoint = deskStand;
        }

        private void Start()
        {
            var gm = GameManager.Instance;
            if (gm != null)
            {
                moveSpeed = gm.Config.policeCarrierSpeed;
                carryCap = gm.Config.policeCarryAmount;
                if (stack != null)
                {
                    stack.SetCapacity(carryCap);
                    stack.SetShowMaxLabel(false);
                }
            }
        }

        protected override void Update()
        {
            switch (_state)
            {
                case State.Idle:
                    if (idlePoint != null) SetTarget(idlePoint.position);
                    base.Update();
                    _idleTimer -= Time.deltaTime;
                    if (_idleTimer <= 0f)
                    {
                        _idleTimer = idlePollInterval;
                        if (shelf != null && shelf.HasAny) _state = State.GoToShelf;
                    }
                    break;
                case State.GoToShelf:
                    if (shelf == null || !shelf.HasAny) { _state = State.Idle; break; }
                    if (shelfStandPoint != null) SetTarget(shelfStandPoint.position);
                    base.Update();
                    if (AtDestination) _state = State.PickingUp;
                    break;
                case State.PickingUp:
                    // HandcuffPickupZone transfers from shelf -> stack while we're inside.
                    if (stack != null && stack.Count >= carryCap) _state = State.GoToDesk;
                    else if (shelf == null || !shelf.HasAny)
                    {
                        if (stack != null && stack.Count > 0) _state = State.GoToDesk;
                    }
                    break;
                case State.GoToDesk:
                    if (deskStandPoint != null) SetTarget(deskStandPoint.position);
                    base.Update();
                    if (AtDestination) _state = State.Depositing;
                    break;
                case State.Depositing:
                    // ProcessingDesk drains our stack through its zone.
                    if (stack == null || stack.Count == 0) _state = State.Idle;
                    break;
            }
        }
    }
}
