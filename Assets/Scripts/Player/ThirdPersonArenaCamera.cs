using IronSand.Enemy;
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
        [SerializeField, Min(0f)] private float lockYawSharpness = 8f;

        private PlayerGladiator player;
        private float yaw;
        private float pitch = 18f;

        public void SetTarget(Transform value)
        {
            target = value;
            player = target != null ? target.GetComponent<PlayerGladiator>() : null;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            EnemyGladiator lockTarget = player != null ? player.LockTarget : null;
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                if (lockTarget == null)
                {
                    yaw += Input.GetAxis("Mouse X") * sensitivity * Time.deltaTime;
                }

                pitch -= Input.GetAxis("Mouse Y") * sensitivity * Time.deltaTime;
                pitch = Mathf.Clamp(pitch, pitchLimits.x, pitchLimits.y);
            }

            Vector3 focus = target.position + targetOffset;
            if (lockTarget != null)
            {
                Vector3 toLock = lockTarget.transform.position - target.position;
                toLock.y = 0f;
                if (toLock.sqrMagnitude > 0.01f)
                {
                    float desiredYaw = Mathf.Atan2(toLock.x, toLock.z) * Mathf.Rad2Deg;
                    yaw = Mathf.LerpAngle(yaw, desiredYaw, 1f - Mathf.Exp(-lockYawSharpness * Time.deltaTime));
                }

                Vector3 lockFocus = lockTarget.transform.position + Vector3.up * 1.1f;
                focus = Vector3.Lerp(focus, lockFocus, 0.22f);
            }

            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 desired = focus - rotation * Vector3.forward * distance;
            transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-followSharpness * Time.deltaTime));
            transform.rotation = rotation;
        }
    }
}
