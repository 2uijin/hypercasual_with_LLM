using PrisonLife.Core;
using PrisonLife.Jail;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PrisonLife.UI
{
    public class HudController : MonoBehaviour
    {
        [SerializeField] private TMP_Text cashLabel;
        [SerializeField] private TMP_Text jailLabel;
        [SerializeField] private JailCell jail;
        [SerializeField] private Button soundButton;
        [SerializeField] private Image soundIcon;

        private bool _soundOn = true;

        private void Start()
        {
            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.OnCashChanged += UpdateCash;
                UpdateCash(gm.Cash);
            }
            if (jail != null)
            {
                jail.OnCapacityChanged += UpdateJail;
                UpdateJail(jail.Count, jail.Capacity);
            }
            if (soundButton != null) soundButton.onClick.AddListener(ToggleSound);
        }

        private void OnDestroy()
        {
            var gm = GameManager.Instance;
            if (gm != null) gm.OnCashChanged -= UpdateCash;
            if (jail != null) jail.OnCapacityChanged -= UpdateJail;
        }

        private void UpdateCash(int amount)
        {
            if (cashLabel != null) cashLabel.text = $"$ {amount}";
        }

        private void UpdateJail(int cur, int cap)
        {
            if (jailLabel != null) jailLabel.text = $"{cur} / {cap}";
        }

        private void ToggleSound()
        {
            _soundOn = !_soundOn;
            AudioListener.volume = _soundOn ? 1f : 0f;
            if (soundIcon != null) soundIcon.color = _soundOn ? Color.white : new Color(1f, 1f, 1f, 0.4f);
        }
    }
}
