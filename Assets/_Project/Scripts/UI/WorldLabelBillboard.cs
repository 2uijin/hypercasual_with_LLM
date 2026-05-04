using UnityEngine;

namespace PrisonLife.UI
{
    public class WorldLabelBillboard : MonoBehaviour
    {
        private Camera _cam;
        private void LateUpdate()
        {
            if (_cam == null) _cam = Camera.main;
            if (_cam == null) return;
            transform.rotation = Quaternion.LookRotation(transform.position - _cam.transform.position, Vector3.up);
        }
    }
}
