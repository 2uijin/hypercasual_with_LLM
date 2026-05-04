using System;
using UnityEngine;

namespace PrisonLife.Zones
{
    /// <summary>
    /// Base zone: fires events when tagged actors enter/exit a trigger collider.
    /// Actor is identified by a Tag (default: Player).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class InteractionZone : MonoBehaviour
    {
        [SerializeField] protected string interactorTag = "Player";

        public event Action<GameObject> OnEnter;
        public event Action<GameObject> OnExit;

        private void Reset()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        protected virtual void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(interactorTag)) return;
            OnEnter?.Invoke(other.gameObject);
        }

        protected virtual void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag(interactorTag)) return;
            OnExit?.Invoke(other.gameObject);
        }
    }
}
