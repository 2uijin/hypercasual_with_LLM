using System.Collections.Generic;
using PrisonLife.Core;
using UnityEngine;

namespace PrisonLife.Factory
{
    /// <summary>
    /// Visual stack of handcuffs produced by the machine. Picked up by whoever is nearby (player/police).
    /// </summary>
    public class HandcuffShelf : MonoBehaviour
    {
        [SerializeField] private Transform stackRoot;
        [SerializeField] private Vector3 itemScale = new Vector3(0.35f, 0.12f, 0.25f);
        [SerializeField] private float itemHeight = 0.14f;
        [SerializeField] private int rowSize = 4;
        [SerializeField] private float rowOffset = 0.28f;
        [SerializeField] private int maxCap = 100;

        private readonly List<GameObject> _items = new();

        public int Count => _items.Count;
        public bool IsFull => _items.Count >= maxCap;
        public bool HasAny => _items.Count > 0;

        private void Awake()
        {
            if (stackRoot == null) stackRoot = transform;
        }

        public bool AddHandcuff()
        {
            if (IsFull) return false;
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var col = block.GetComponent<Collider>(); if (col != null) Destroy(col);
            block.transform.SetParent(stackRoot, false);
            block.transform.localScale = itemScale;

            int idx = _items.Count;
            int row = idx / rowSize;
            int c = idx % rowSize;
            float x = (c - (rowSize - 1) * 0.5f) * rowOffset;
            float y = row * itemHeight + itemHeight * 0.5f;
            block.transform.localPosition = new Vector3(x, y, 0f);
            var rend = block.GetComponent<Renderer>();
            if (rend != null) rend.sharedMaterial = _mat ??= Make();
            _items.Add(block);
            return true;
        }

        public bool TryRemove()
        {
            if (_items.Count == 0) return false;
            int last = _items.Count - 1;
            var go = _items[last];
            _items.RemoveAt(last);
            if (go != null) Destroy(go);
            return true;
        }

        private static Material _mat;
        private static Material Make()
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            m.color = new Color(0.85f, 0.85f, 0.9f);
            return m;
        }
    }
}
