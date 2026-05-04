using PrisonLife.FX;
using PrisonLife.Player;
using UnityEngine;

namespace PrisonLife.Quarry
{
    /// <summary>
    /// Spawns pickaxe / Drill prefab visuals on the player and animates them
    /// while inside the quarry. Drill replaces pickaxe once unlocked, and both
    /// hide entirely once the vehicle is unlocked.
    /// </summary>
    public class MiningEquipment : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private MiningTool miningTool;       // animation runs only while this reports IsMining
        [SerializeField] private Transform attachRoot;       // parent for tool instances; defaults to this transform
        [SerializeField] private GameObject pickaxePrefab;
        [SerializeField] private GameObject drillPrefab;

        [Header("Hold pose (local to attachRoot)")]
        [SerializeField] private Vector3 holdLocalPosition = new Vector3(0.45f, 1.0f, 0.15f);
        [SerializeField] private Vector3 holdLocalEuler = new Vector3(0f, 0f, 0f);

        [Header("Pickaxe swing")]
        [SerializeField] private float pickaxePeriod = 0.7f;
        [SerializeField] private float pickaxeRaiseAngle = -35f;   // X rotation when raised
        [SerializeField] private float pickaxeStrikeAngle = 55f;   // X rotation at strike
        [SerializeField, Range(0.1f, 0.9f)] private float pickaxeStrikeFraction = 0.32f; // share of period spent striking down

        [Header("Drill shake")]
        [SerializeField] private float drillShakeFreq = 38f;
        [SerializeField] private float drillShakePosAmp = 0.025f;
        [SerializeField] private float drillShakeRotAmp = 5f;

        [Header("Legacy")]
        [SerializeField] private string legacyDrillVisualName = "DrillVisual";

        [Header("SFX")]
        [SerializeField] private AudioClip pickaxeSwingSfx;
        [SerializeField] private AudioClip drillLoopSfx;
        [SerializeField] private AudioClip vehicleLoopSfx;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;

        private Transform _pickaxe;
        private Transform _drill;
        private float _t;
        private int _lastSwingCycle = -1;
        private AudioSource _drillLoopSrc;
        private AudioSource _vehicleLoopSrc;

        private void Awake()
        {
            if (player == null) player = GetComponent<PlayerController>();
            if (miningTool == null) miningTool = GetComponent<MiningTool>();
            var root = attachRoot != null ? attachRoot : transform;

            if (pickaxePrefab != null)
            {
                var go = Instantiate(pickaxePrefab, root);
                go.name = "PickaxeVisual";
                _pickaxe = go.transform;
                ResetHold(_pickaxe);
                go.SetActive(false);
            }
            if (drillPrefab != null)
            {
                var go = Instantiate(drillPrefab, root);
                go.name = "DrillVisual_Prefab";
                _drill = go.transform;
                ResetHold(_drill);
                go.SetActive(false);
            }

            // Hide the old primitive DrillVisual that BuildPlayer used to create.
            if (!string.IsNullOrEmpty(legacyDrillVisualName))
            {
                var legacy = FindChildByName(transform, legacyDrillVisualName);
                if (legacy != null && legacy != _drill && legacy != _pickaxe)
                    legacy.gameObject.SetActive(false);
            }
        }

        private void ResetHold(Transform t)
        {
            t.localPosition = holdLocalPosition;
            t.localRotation = Quaternion.Euler(holdLocalEuler);
        }

        private static Transform FindChildByName(Transform root, string name)
        {
            for (int i = 0; i < root.childCount; i++)
            {
                var c = root.GetChild(i);
                if (c.name == name) return c;
                var r = FindChildByName(c, name);
                if (r != null) return r;
            }
            return null;
        }

        private void Update()
        {
            if (player == null) return;

            bool inQuarry = player.IsInQuarry;
            bool hasVehicle = player.HasVehicle;
            bool hasDrill = player.HasDrill;

            bool showPickaxe = inQuarry && !hasVehicle && !hasDrill;
            bool showDrill   = inQuarry && !hasVehicle &&  hasDrill;

            SetActive(_pickaxe, showPickaxe);
            SetActive(_drill,   showDrill);

            if (!showPickaxe && !showDrill) return;

            bool actuallyMining = miningTool != null && miningTool.IsMining;
            bool drillLoopActive = showDrill && actuallyMining && !player.IsRiding;
            bool vehicleLoopActive = player.IsRiding && actuallyMining;
            if (drillLoopActive) SfxPlayer.EnsureLoop(ref _drillLoopSrc, gameObject, drillLoopSfx, sfxVolume);
            else SfxPlayer.StopLoop(_drillLoopSrc);
            if (vehicleLoopActive) SfxPlayer.EnsureLoop(ref _vehicleLoopSrc, gameObject, vehicleLoopSfx, sfxVolume);
            else SfxPlayer.StopLoop(_vehicleLoopSrc);

            if (!actuallyMining)
            {
                if (_pickaxe != null) { _pickaxe.localPosition = holdLocalPosition; _pickaxe.localRotation = Quaternion.Euler(pickaxeRaiseAngle, holdLocalEuler.y, holdLocalEuler.z); }
                if (_drill != null)   { _drill.localPosition = holdLocalPosition;   _drill.localRotation = Quaternion.Euler(holdLocalEuler); }
                _lastSwingCycle = -1;
                return;
            }
            _t += Time.deltaTime;

            if (showPickaxe && _pickaxe != null)
            {
                float period = Mathf.Max(0.05f, pickaxePeriod);
                float phase = _t / period;
                int cycle = (int)phase;
                float u = phase - cycle;
                float strikeFrac = Mathf.Clamp(pickaxeStrikeFraction, 0.05f, 0.95f);
                float angle;
                if (u < strikeFrac)
                    angle = Mathf.Lerp(pickaxeRaiseAngle, pickaxeStrikeAngle, EaseOutQuad(u / strikeFrac));
                else
                    angle = Mathf.Lerp(pickaxeStrikeAngle, pickaxeRaiseAngle, EaseInOutQuad((u - strikeFrac) / (1f - strikeFrac)));
                _pickaxe.localPosition = holdLocalPosition;
                _pickaxe.localRotation = Quaternion.Euler(angle, holdLocalEuler.y, holdLocalEuler.z);

                if (u >= strikeFrac && cycle != _lastSwingCycle)
                {
                    _lastSwingCycle = cycle;
                    SfxPlayer.PlayOneShot(pickaxeSwingSfx, _pickaxe.position, sfxVolume);
                }
            }
            else
            {
                _lastSwingCycle = -1;
            }

            if (showDrill && _drill != null)
            {
                float f = drillShakeFreq;
                float dx = Mathf.Sin(_t * f)              * drillShakePosAmp;
                float dy = Mathf.Cos(_t * f * 1.13f)      * drillShakePosAmp * 0.7f;
                float dz = Mathf.Sin(_t * f * 0.87f + 1.7f) * drillShakePosAmp * 0.5f;
                _drill.localPosition = holdLocalPosition + new Vector3(dx, dy, dz);
                float rx = Mathf.Cos(_t * f * 0.92f) * drillShakeRotAmp * 0.5f;
                float rz = Mathf.Sin(_t * f * 1.05f) * drillShakeRotAmp;
                _drill.localRotation = Quaternion.Euler(holdLocalEuler.x + rx, holdLocalEuler.y, holdLocalEuler.z + rz);
            }
        }

        private static void SetActive(Transform t, bool active)
        {
            if (t == null) return;
            if (t.gameObject.activeSelf != active) t.gameObject.SetActive(active);
        }

        private static float EaseOutQuad(float k) { return 1f - (1f - k) * (1f - k); }
        private static float EaseInOutQuad(float k) { return k < 0.5f ? 2f * k * k : 1f - Mathf.Pow(-2f * k + 2f, 2f) * 0.5f; }
    }
}
