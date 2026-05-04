using System.Collections.Generic;
using UnityEngine;

namespace PrisonLife.NPC
{
    public abstract class BaseNPC : MonoBehaviour
    {
        [SerializeField] protected float moveSpeed = 2.8f;
        [SerializeField] protected float arriveRadius = 0.2f;
        [SerializeField] protected Transform model;

        protected Vector3 _target;
        protected bool _hasTarget;
        protected Queue<Vector3> _path;
        public bool AtDestination => !_hasTarget || (transform.position - _target).sqrMagnitude <= arriveRadius * arriveRadius;

        public virtual void SetTarget(Vector3 worldPos)
        {
            _path?.Clear();
            _target = worldPos;
            _hasTarget = true;
        }

        public virtual void SetPath(Vector3[] waypoints)
        {
            if (waypoints == null || waypoints.Length == 0) { _hasTarget = false; return; }
            _path ??= new Queue<Vector3>();
            _path.Clear();
            for (int i = 1; i < waypoints.Length; i++) _path.Enqueue(waypoints[i]);
            _target = waypoints[0];
            _hasTarget = true;
        }

        public virtual void MoveTo(Vector3 worldPos) => SetTarget(worldPos);

        protected virtual void Update()
        {
            if (!_hasTarget) return;
            Vector3 flatTarget = _target; flatTarget.y = transform.position.y;
            Vector3 to = flatTarget - transform.position;
            float dist = to.magnitude;
            if (dist <= arriveRadius)
            {
                if (_path != null && _path.Count > 0) { _target = _path.Dequeue(); return; }
                _hasTarget = false;
                return;
            }
            Vector3 dir = to / dist;
            float step = moveSpeed * Time.deltaTime;
            transform.position += dir * Mathf.Min(step, dist);
            if (model != null)
                model.rotation = Quaternion.Slerp(model.rotation, Quaternion.LookRotation(dir, Vector3.up), 10f * Time.deltaTime);
        }

        public void SetMoveSpeed(float s) => moveSpeed = s;
    }
}
