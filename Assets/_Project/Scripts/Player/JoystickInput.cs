using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PrisonLife.Player
{
    /// <summary>
    /// Minimal virtual joystick. Drag the Handle within Background. Returns normalized [-1,1] Vector2.
    /// Works with touch and mouse.
    /// </summary>
    public class JoystickInput : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private RectTransform background;
        [SerializeField] private RectTransform handle;
        [SerializeField, Range(0.01f, 1f)] private float handleRange = 0.6f;
        [SerializeField, Range(0f, 0.9f)] private float deadZone = 0.1f;

        public Vector2 Value { get; private set; }
        public bool IsPressed { get; private set; }

        private Canvas _canvas;

        private void Awake()
        {
            _canvas = GetComponentInParent<Canvas>();
            if (handle != null) handle.anchoredPosition = Vector2.zero;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            IsPressed = true;
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (background == null || handle == null) return;

            Camera cam = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? _canvas.worldCamera
                : null;

            Vector2 localPoint;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(background, eventData.position, cam, out localPoint))
                return;

            Vector2 size = background.rect.size;
            Vector2 normalized = new Vector2(localPoint.x / (size.x * 0.5f), localPoint.y / (size.y * 0.5f));
            if (normalized.magnitude > 1f) normalized = normalized.normalized;

            if (normalized.magnitude < deadZone) normalized = Vector2.zero;

            Value = normalized;
            handle.anchoredPosition = new Vector2(normalized.x * size.x * 0.5f * handleRange, normalized.y * size.y * 0.5f * handleRange);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            IsPressed = false;
            Value = Vector2.zero;
            if (handle != null) handle.anchoredPosition = Vector2.zero;
        }
    }
}
