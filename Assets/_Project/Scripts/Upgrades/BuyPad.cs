using System;
using PrisonLife.Core;
using PrisonLife.FX;
using PrisonLife.Items;
using PrisonLife.Player;
using TMPro;
using UnityEngine;

namespace PrisonLife.Upgrades
{
    /// <summary>
    /// Diamond-shaped buy pad. Player steps on → cash drains incrementally → purchase applies.
    /// The pad's GameObject is disabled while locked — a sibling <see cref="BuyPadUnlocker"/>
    /// handles SetActive(true) when its unlock condition is met.
    /// </summary>
    public class BuyPad : MonoBehaviour
    {
        [SerializeField] private string upgradeId;
        [SerializeField] private int price = 50;
        [SerializeField] private TextMeshPro costLabel;
        [SerializeField] private GameObject padVisual;
        [SerializeField] private ParticleSystem purchaseFx;

        [Header("Cash flight to pad")]
        [SerializeField] private float cashFlightDuration = 0.32f;
        [SerializeField] private float cashFlightArcHeight = 0.9f;
        [SerializeField] private Vector3 cashFlightTargetOffset = new Vector3(0f, 0.05f, 0f);

        [Header("Fill overlay (paints visual green with paid cash)")]
        [SerializeField] private Transform fillVisual;
        [SerializeField] private Color fillColor = new Color(0.25f, 0.75f, 0.35f);

        public event Action<BuyPad> OnPurchased;

        private PlayerController _player;
        private int _accumulated;
        private int _credited;
        private bool _purchased;

        public string UpgradeId => upgradeId;
        public int Price => price;
        public int Remaining => Mathf.Max(0, price - _credited);

        public void SetPrice(int p) { price = p; UpdateLabel(); }
        public void SetUpgradeId(string id) { upgradeId = id; }

        private void Start()
        {
            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.OnUpgradeChanged += OnUpgradeChanged;
                if (gm.IsPurchased(upgradeId)) { _purchased = true; gameObject.SetActive(false); return; }
            }
            EnsureFill();
            UpdateLabel();
            UpdateFill();
        }

        private void OnDestroy()
        {
            var gm = GameManager.Instance;
            if (gm != null) gm.OnUpgradeChanged -= OnUpgradeChanged;
        }

        private void OnUpgradeChanged(string id, bool purchased)
        {
            if (id == upgradeId && purchased) gameObject.SetActive(false);
        }

        private void OnTriggerEnter(Collider other)
        {
            var p = other.GetComponentInParent<PlayerController>();
            if (p != null) _player = p;
        }

        private void OnTriggerExit(Collider other)
        {
            var p = other.GetComponentInParent<PlayerController>();
            if (p == _player) _player = null;
        }

        private float _drainCarry;

        private void Update()
        {
            if (_purchased || _player == null) return;
            var gm = GameManager.Instance;
            if (gm == null) return;

            int outstanding = price - _accumulated;
            if (outstanding <= 0) return; // waiting for in-flight bills to credit

            float ratePerSec = gm.Config.buyPadDrainPerSecond;
            _drainCarry += ratePerSec * Time.deltaTime;
            int request = (int)_drainCarry;
            if (request <= 0) return;
            _drainCarry -= request;
            request = Mathf.Min(request, outstanding);

            int spent = gm.SpendUpTo(request);
            if (spent <= 0) return;
            _accumulated += spent;

            var stack = _player.CashStack;
            for (int i = 0; i < spent; i++)
            {
                Vector3 spawn = stack != null && stack.Count > 0
                    ? stack.GetTopWorldPosition()
                    : (_player != null ? _player.transform.position + Vector3.up * 1.5f : transform.position);
                if (stack != null) stack.TryRemove();
                FlyingPickup.Spawn(spawn, transform, cashFlightTargetOffset, CarryKind.Cash,
                    cashFlightDuration + UnityEngine.Random.Range(-0.04f, 0.04f),
                    cashFlightArcHeight, OnCashArrived);
            }
        }

        private void OnCashArrived()
        {
            if (_purchased) return;
            _credited++;
            UpdateLabel();
            UpdateFill();
            if (_credited >= price) Complete();
        }

        private void Complete()
        {
            _purchased = true;
            var gm = GameManager.Instance;
            if (gm != null) gm.MarkPurchased(upgradeId);
            if (purchaseFx != null) purchaseFx.Play();
            if (_player != null)
                FxSpawner.SpawnPurchaseGlowAt(_player.transform.position);
            OnPurchased?.Invoke(this);
            gameObject.SetActive(false);
        }

        private void UpdateLabel()
        {
            if (costLabel == null) return;
            costLabel.text = $"$ {Remaining}";
        }

        private static Material _fillMat;
        private Transform GetVisualTransform()
        {
            if (padVisual != null) return padVisual.transform;
            return transform.Find("Visual");
        }

        private void EnsureFill()
        {
            if (fillVisual != null) return;
            var visT = GetVisualTransform();
            if (visT == null) return;

            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
            go.name = "Fill";
            go.transform.SetParent(transform, false);
            go.transform.localRotation = visT.localRotation;
            go.transform.localScale = new Vector3(visT.localScale.x, visT.localScale.y, 0.0001f);
            float frontZ = visT.localPosition.z - visT.localScale.z * 0.5f;
            go.transform.localPosition = new Vector3(visT.localPosition.x, visT.localPosition.y + 0.001f, frontZ);

            var rend = go.GetComponent<Renderer>();
            if (rend != null)
            {
                if (_fillMat == null)
                {
                    _fillMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                    _fillMat.color = fillColor;
                }
                rend.sharedMaterial = _fillMat;
                rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            fillVisual = go.transform;
        }

        private void UpdateFill()
        {
            if (fillVisual == null) return;
            var visT = GetVisualTransform();
            if (visT == null) return;
            float ratio = price > 0 ? Mathf.Clamp01((float)_credited / price) : 0f;
            float fullZ = visT.localScale.z;
            float scaleZ = Mathf.Max(0.0001f, ratio * fullZ);
            float frontZ = visT.localPosition.z - fullZ * 0.5f;
            float centerZ = frontZ + scaleZ * 0.5f;
            fillVisual.localScale = new Vector3(visT.localScale.x, visT.localScale.y, scaleZ);
            fillVisual.localPosition = new Vector3(visT.localPosition.x, visT.localPosition.y + 0.001f, centerZ);
        }
    }
}
