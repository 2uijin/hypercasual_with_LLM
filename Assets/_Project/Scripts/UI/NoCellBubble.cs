using PrisonLife.Jail;
using TMPro;
using UnityEngine;

namespace PrisonLife.UI
{
    public class NoCellBubble : MonoBehaviour
    {
        [SerializeField] private JailCell jail;
        [SerializeField] private TMP_Text label;

        public void Configure(JailCell j, GameObject bubbleGo, TMP_Text labelTmp)
        {
            jail = j;
            label = labelTmp;
        }

        private void Start()
        {
            if (jail != null) jail.OnCapacityChanged += OnJailChanged;
            if (label != null) label.text = "no cell!";
        }

        private void OnDestroy()
        {
            if (jail != null) jail.OnCapacityChanged -= OnJailChanged;
        }

        private void OnJailChanged(int cur, int cap)
        {
            if (label != null) label.gameObject.SetActive(cur >= cap);
        }
    }
}
