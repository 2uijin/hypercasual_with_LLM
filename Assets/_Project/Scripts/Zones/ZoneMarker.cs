using UnityEngine;

namespace PrisonLife.Zones
{
    /// <summary>
    /// Draws a colored outline rectangle on the ground and an optional floating label.
    /// Build is explicit via Configure() to avoid OnEnable re-entrancy during scene building.
    /// </summary>
    public class ZoneMarker : MonoBehaviour
    {
        public enum Kind { Pickup, Deposit, Buy, Quarry }

        [SerializeField] private Kind kind = Kind.Pickup;
        [SerializeField] private BoxCollider source;
        [SerializeField] private Vector2 explicitSize = Vector2.zero;
        [SerializeField] private float groundY = 0.03f;
        [SerializeField] private float lineWidth = 0.1f;
        [SerializeField] private string labelText;
        [SerializeField] private float labelHeight = 1.6f;

        private void Start()
        {
            Rebuild();
        }

        public void Configure(Kind k, string label, BoxCollider src = null, Vector2? size = null)
        {
            kind = k;
            labelText = label;
            if (src != null) source = src;
            if (size.HasValue) explicitSize = size.Value;
            Rebuild();
        }

        public void Rebuild()
        {
            if (this == null) return;
            BuildLine();
            BuildLabel();
        }

        private void BuildLine()
        {
            Transform t = transform.Find("_ZoneLine");
            if (t == null)
            {
                var g = new GameObject("_ZoneLine");
                g.transform.SetParent(transform, false);
                t = g.transform;
            }
            var line = t.GetComponent<LineRenderer>();
            if (line == null) line = t.gameObject.AddComponent<LineRenderer>();

            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = 4;
            line.widthMultiplier = lineWidth;
            line.alignment = LineAlignment.TransformZ;

            Vector2 size = explicitSize.sqrMagnitude > 0.0001f ? explicitSize : GetColliderXZ();
            float hx = size.x * 0.5f;
            float hz = size.y * 0.5f;

            t.localPosition = new Vector3(0, groundY - transform.position.y + 0.03f, 0);
            t.localRotation = Quaternion.Euler(90f, 0f, 0f);

            line.SetPosition(0, new Vector3(-hx, -hz, 0));
            line.SetPosition(1, new Vector3(hx, -hz, 0));
            line.SetPosition(2, new Vector3(hx, hz, 0));
            line.SetPosition(3, new Vector3(-hx, hz, 0));

            Color c = GetKindColor();
            var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
            var mat = new Material(shader);
            if (mat.HasProperty("_Color")) mat.color = c;
            line.sharedMaterial = mat;
            line.startColor = line.endColor = c;
        }

        private void BuildLabel()
        {
            if (string.IsNullOrEmpty(labelText)) return;

            Transform existing = transform.Find("_ZoneLabel");
            GameObject go = existing != null ? existing.gameObject : null;
            if (go == null)
            {
                go = new GameObject("_ZoneLabel");
                go.transform.SetParent(transform, false);
            }
            go.transform.localPosition = new Vector3(0, labelHeight, 0);
            go.transform.localScale = Vector3.one * 0.25f;

            var tm = go.GetComponent<TextMesh>();
            if (tm == null) tm = go.AddComponent<TextMesh>();
            tm.text = labelText;
            tm.fontSize = 48;
            tm.characterSize = 0.5f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = GetKindColor();

            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
            }

            if (go.GetComponent<PrisonLife.UI.WorldLabelBillboard>() == null)
                go.AddComponent<PrisonLife.UI.WorldLabelBillboard>();
        }

        private Color GetKindColor()
        {
            switch (kind)
            {
                case Kind.Pickup:  return new Color(0.15f, 0.9f, 0.2f, 1f);
                case Kind.Deposit: return new Color(1f, 1f, 1f, 1f);
                case Kind.Buy:     return new Color(0.25f, 0.7f, 1f, 1f);
                case Kind.Quarry:  return new Color(1f, 0.75f, 0.1f, 1f);
            }
            return Color.white;
        }

        private Vector2 GetColliderXZ()
        {
            if (source == null) return new Vector2(1f, 1f);
            Vector3 s = source.size;
            return new Vector2(s.x, s.z);
        }
    }
}
