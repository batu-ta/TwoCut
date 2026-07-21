using UnityEngine;

namespace HairSalonGame
{
    public class CameraController : MonoBehaviour
    {
        [Header("Target Tracking")]
        public Transform target;
        public Vector3 offset = new Vector3(0f, 15f, -13f);
        public float pitchAngle = 50f;
        public float smoothSpeed = 8f;

        private void Start()
        {
            transform.rotation = Quaternion.Euler(pitchAngle, 0f, 0f);
        }

        private void LateUpdate()
        {
            if (target == null) return;
            Vector3 targetPosition = target.position + offset;
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothSpeed);
        }
    }
}
