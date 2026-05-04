using System.Collections.Generic;
using UnityEngine;

namespace PrisonLife.Core
{
    public class SimpleObjectPool : MonoBehaviour
    {
        [SerializeField] private GameObject prefab;
        [SerializeField] private int prewarm = 0;

        private readonly Stack<GameObject> _pool = new();

        private void Awake()
        {
            for (int i = 0; i < prewarm; i++)
                _pool.Push(CreateInstance());
        }

        private GameObject CreateInstance()
        {
            var go = Instantiate(prefab, transform);
            go.SetActive(false);
            return go;
        }

        public GameObject Get(Vector3 pos, Quaternion rot)
        {
            var go = _pool.Count > 0 ? _pool.Pop() : CreateInstance();
            go.transform.SetPositionAndRotation(pos, rot);
            go.SetActive(true);
            return go;
        }

        public void Release(GameObject go)
        {
            if (go == null) return;
            go.SetActive(false);
            go.transform.SetParent(transform, false);
            _pool.Push(go);
        }
    }
}
