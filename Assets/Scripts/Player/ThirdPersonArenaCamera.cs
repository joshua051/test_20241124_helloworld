using UnityEngine;

namespace IronSand.Player
{
    public sealed class ThirdPersonArenaCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 targetOffset = new(0f, 1.35f, 0f);
        [SerializeField, Min(1f)] private float distance = 6.5f;
        [SerializeField, Min(0f)] private float sensitivity = 150f;
        [SerializeField] private Vector2 pitchLimits = new(-20f, 65f);
        [SerializeField, Min(0f)] private float followSharpness = 14f;

        private float yaw;
        private float pitch = 18f;

        public void SetTarget(Transform value)
        {
            target = value;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            if (Cursor.lockState == CursorLockMode.Locked)
            {
                yaw += Input.GetAxis("Mouse X") * sensitivity * Time.deltaTime;
                pitch -= Input.GetAxis("Mouse Y") * sensitivity * Time.deltaTime;
                pitch = Mathf.Clamp(pitch, pitchLimits.x, pitchLimits.y);
            }

            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 focus = target.position + targetOffset;
            Vector3 desired = focus - rotation * Vector3.forward * distance;
            transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-followSharpness * Time.deltaTime));
            transform.rotation = rotation;
        }
    }
}
