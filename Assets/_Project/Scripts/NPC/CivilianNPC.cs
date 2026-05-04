using UnityEngine;

namespace PrisonLife.NPC
{
    /// <summary>
    /// Civilian that queues at desk, purchases handcuffs, converts to prisoner appearance, then walks to jail.
    /// Appearance swap = body color change.
    /// </summary>
    public class CivilianNPC : BaseNPC
    {
        [SerializeField] private Renderer body;
        [SerializeField] private Color civilianColor = new Color(0.9f, 0.9f, 0.9f);
        [SerializeField] private Color prisonerColor = new Color(0.95f, 0.5f, 0.1f);

        [Header("Purchase")]
        [SerializeField] private int handcuffsRequired = 1;

        [Header("Speech Bubble")]
        [SerializeField] private GameObject speechBubble;
        [SerializeField] private TextMesh speechText;

        private bool _isPrisoner;
        public bool IsPrisoner => _isPrisoner;
        public int HandcuffsRequired => handcuffsRequired;

        private void Start()
        {
            if (body != null) body.material.color = civilianColor;
            var gm = PrisonLife.Core.GameManager.Instance;
            if (gm != null) moveSpeed = gm.Config.npcMoveSpeed;
            SetRequirementVisible(false);
            RefreshSpeechText();
        }

        public void SetHandcuffsRequired(int n)
        {
            handcuffsRequired = Mathf.Max(1, n);
            RefreshSpeechText();
        }

        public void SetRequirementVisible(bool visible)
        {
            if (_isPrisoner) visible = false;
            if (speechBubble != null) speechBubble.SetActive(visible);
        }

        private void RefreshSpeechText()
        {
            if (speechText != null) speechText.text = "x" + handcuffsRequired;
        }

        public void ConvertToPrisoner()
        {
            _isPrisoner = true;
            if (body != null) body.material.color = prisonerColor;
            SetRequirementVisible(false);
        }
    }
}
