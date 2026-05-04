using System;
using PrisonLife.Items;
using UnityEngine;

namespace PrisonLife.FX
{
    /// <summary>
    /// One-shot projectile that arcs from a world point to a target Transform, then
    /// fires a callback (typically: add the item to the destination stack). Self-destructs on arrival.
    /// </summary>
    public class FlyingPickup : MonoBehaviour
    {
        private Vector3 _start;
        private Transform _target;
        private Vector3 _targetLocalOffset;
        private float _t, _duration, _arcHeight;
        private Vector3 _spinEuler;
        private Action _onArrive;
        private Vector3 _baseScale;
        private float _popDuration = 0.18f;
        private bool _arrived;

        public static FlyingPickup Spawn(
            Vector3 startWorld, Transform target, Vector3 targetLocalOffset,
            CarryKind kind, float duration, float arcHeight, Action onArrive)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var col = go.GetComponent<Collider>(); if (col != null) Destroy(col);
            go.name = $"Flying_{kind}";
            go.transform.position = startWorld;
            go.transform.localScale = ScaleFor(kind) * 0.6f;
            var rend = go.GetComponent<Renderer>();
            if (rend != null) rend.sharedMaterial = MatFor(kind);

            var fp = go.AddComponent<FlyingPickup>();
            fp._start = startWorld;
            fp._target = target;
            fp._targetLocalOffset = targetLocalOffset;
            fp._duration = Mathf.Max(0.05f, duration);
            fp._arcHeight = arcHeight;
            fp._onArrive = onArrive;
            fp._baseScale = ScaleFor(kind);
            fp._spinEuler = new Vector3(
                UnityEngine.Random.Range(360f, 720f),
                UnityEngine.Random.Range(360f, 720f),
                UnityEngine.Random.Range(180f, 540f));
            return fp;
        }

        private Vector3 EndWorld()
        {
            if (_target == null) return _start;
            return _target.TransformPoint(_targetLocalOffset);
        }

        private void Update()
        {
            if (_target == null) { Arrive(); return; }
            _t += Time.deltaTime;
            float u = Mathf.Clamp01(_t / _duration);
            Vector3 pos = Vector3.Lerp(_start, EndWorld(), u);
            pos.y += _arcHeight * 4f * u * (1f - u);
            transform.position = pos;
            transform.Rotate(_spinEuler * Time.deltaTime);

            float pop = Mathf.Min(1f, _t / _popDuration);
            float ease = 1f - (1f - pop) * (1f - pop);
            transform.localScale = _baseScale * (0.6f + 0.4f * ease);

            if (u >= 1f) Arrive();
        }

        private void Arrive()
        {
            if (_arrived) return;
            _arrived = true;
            try { _onArrive?.Invoke(); } catch (Exception) { }
            Destroy(gameObject);
        }

        private static Vector3 ScaleFor(CarryKind kind)
        {
            switch (kind)
            {
                case CarryKind.Rock: return new Vector3(0.28f, 0.24f, 0.28f);
                case CarryKind.Handcuff: return new Vector3(0.32f, 0.1f, 0.22f);
                case CarryKind.Cash:
                default: return new Vector3(0.26f, 0.1f, 0.36f);
            }
        }

        private static Material _matRock, _matHandcuff, _matCash;
        private static Material MatFor(CarryKind kind)
        {
            switch (kind)
            {
                case CarryKind.Rock:
                    if (_matRock == null) _matRock = Make(new Color(0.32f, 0.32f, 0.35f));
                    return _matRock;
                case CarryKind.Handcuff:
                    if (_matHandcuff == null) _matHandcuff = Make(new Color(0.85f, 0.85f, 0.9f));
                    return _matHandcuff;
                case CarryKind.Cash:
                default:
                    if (_matCash == null) _matCash = Make(new Color(0.25f, 0.75f, 0.35f));
                    return _matCash;
            }
        }

        private static Material Make(Color c)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            m.color = c;
            return m;
        }
    }
}
