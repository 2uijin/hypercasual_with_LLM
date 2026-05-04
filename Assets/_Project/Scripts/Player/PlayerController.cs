using PrisonLife.Core;
using PrisonLife.Items;
using UnityEngine;

namespace PrisonLife.Player
{
    /// <summary>
    /// Character Controller-based player. Moves via JoystickInput. Also supports keyboard for editor testing.
    /// Public API exposes carry stacks for rocks, handcuffs, cash.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private JoystickInput joystick;
        [SerializeField] private CarryStack rockStack;
        [SerializeField] private CarryStack handcuffStack;
        [SerializeField] private CarryStack cashStack;
        [SerializeField] private Transform modelRoot;
        [SerializeField] private Transform drillVisual;  // set active when drill purchased
        [SerializeField] private Transform vehicleVisual; // set active when vehicle purchased

        private CharacterController _cc;
        private GameConfigProxy _cfg;

        public CarryStack RockStack => rockStack;
        public CarryStack HandcuffStack => handcuffStack;
        public CarryStack CashStack => cashStack;

        public bool HasDrill { get; private set; }
        public bool HasVehicle { get; private set; }
        public bool IsInQuarry { get; private set; }
        public bool IsRiding => HasVehicle && IsInQuarry;

        public void SetInQuarry(bool inside)
        {
            if (IsInQuarry == inside) return;
            IsInQuarry = inside;
            ApplyVisuals();
        }

        private const float Gravity = -9.81f;
        private float _yVel;

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _cfg = new GameConfigProxy();
        }

        private void Start()
        {
            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.OnUpgradeChanged += OnUpgradeChanged;
                HasDrill = gm.IsPurchased(UpgradeIds.Drill);
                HasVehicle = gm.IsPurchased(UpgradeIds.Vehicle);
                ApplyVisuals();

                var cfg = gm.Config;
                if (rockStack != null)
                {
                    rockStack.SetCapacity(GetRockCapacity(cfg));
                    rockStack.SetRowSize(cfg.stackRowSize);
                }
                if (cashStack != null) cashStack.SetRowSize(cfg.stackRowSize);
                if (handcuffStack != null) handcuffStack.SetRowSize(cfg.stackRowSize);
            }
        }

        private int GetRockCapacity(PrisonLife.Config.GameConfig cfg)
        {
            if (HasVehicle) return cfg.rockCarryCapacityVehicle;
            if (HasDrill) return cfg.rockCarryCapacityDrill;
            return cfg.rockCarryCapacity;
        }

        private void OnDestroy()
        {
            var gm = GameManager.Instance;
            if (gm != null) gm.OnUpgradeChanged -= OnUpgradeChanged;
        }

        private void OnUpgradeChanged(string id, bool purchased)
        {
            if (id == UpgradeIds.Drill) HasDrill = purchased;
            if (id == UpgradeIds.Vehicle) HasVehicle = purchased;
            ApplyVisuals();
            var gm = GameManager.Instance;
            if (gm != null && rockStack != null)
                rockStack.SetCapacity(GetRockCapacity(gm.Config));
        }

        private void ApplyVisuals()
        {
            bool riding = IsRiding;
            // Legacy primitive drill is superseded by MiningEquipment; keep it hidden.
            if (drillVisual != null && drillVisual.gameObject.activeSelf) drillVisual.gameObject.SetActive(false);
            if (vehicleVisual != null) vehicleVisual.gameObject.SetActive(riding);
            if (modelRoot != null) modelRoot.gameObject.SetActive(!riding);
        }

        private void Update()
        {
            Vector2 input = ReadInput();
            Vector3 planar = new Vector3(input.x, 0f, input.y);

            var cfg = _cfg.Get();
            float speed = IsRiding ? cfg.playerVehicleSpeed : cfg.playerMoveSpeed;

            if (_cc.isGrounded && _yVel < 0f) _yVel = -1f;
            _yVel += Gravity * Time.deltaTime;

            Vector3 velocity = planar * speed;
            velocity.y = _yVel;
            _cc.Move(velocity * Time.deltaTime);

            if (planar.sqrMagnitude > 0.01f && modelRoot != null)
            {
                Quaternion target = Quaternion.LookRotation(planar, Vector3.up);
                modelRoot.rotation = Quaternion.RotateTowards(modelRoot.rotation, target, cfg.playerRotateSpeed * Time.deltaTime);
                if (IsRiding && vehicleVisual != null)
                    vehicleVisual.rotation = modelRoot.rotation;
            }
        }

        private Vector2 ReadInput()
        {
            Vector2 v = joystick != null ? joystick.Value : Vector2.zero;
            if (v.sqrMagnitude < 0.0004f)
            {
                float h = Input.GetAxisRaw("Horizontal");
                float ver = Input.GetAxisRaw("Vertical");
                v = new Vector2(h, ver);
                if (v.magnitude > 1f) v = v.normalized;
            }
            return v;
        }
    }

    internal class GameConfigProxy
    {
        public PrisonLife.Config.GameConfig Get()
        {
            var gm = GameManager.Instance;
            return gm != null ? gm.Config : null;
        }
    }
}
