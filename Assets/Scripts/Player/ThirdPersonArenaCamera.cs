using IronSand.Combat;
using IronSand.Enemy;
using UnityEngine;

namespace IronSand.Player
{
    public sealed class ThirdPersonArenaCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 targetOffset = new(0f, 0.65f, 0f);
        [SerializeField, Min(1f)] private float distance = 6.5f;
        [SerializeField, Min(0f)] private float sensitivity = 2.5f;
        [SerializeField] private Vector2 pitchLimits = new(-20f, 65f);
        [SerializeField, Min(0f)] private float followSharpness = 14f;
        [SerializeField, Min(0f)] private float lockYawSharpness = 8f;
        [SerializeField, Min(0.05f)] private float collisionRadius = 0.22f;
        private PlayerGladiator player;
        private float yaw;
        private float pitch = 18f;
        public PlayerGladiator TrackedPlayer => player;

        private void Awake() { RebindTarget(); }
        private void OnEnable() { RebindTarget(); }
        public void SetTarget(Transform value) { target = value; RebindTarget(); }
        public void RebindTarget()
        {
            // Only target is serialized. Runtime cache must survive a scene reload.
            player = target != null ? target.GetComponent<PlayerGladiator>() : null;
        }

        private void LateUpdate()
        {
            if (target == null || Time.timeScale <= 0f || Time.deltaTime <= 0f) return;
            if (player == null) RebindTarget();
            EnemyGladiator lockTarget = player != null ? player.LockTarget : null;
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                // Mouse axes are per-frame deltas; multiplying by deltaTime adds frame-rate dependence.
                if (lockTarget == null) yaw += Input.GetAxisRaw("Mouse X") * sensitivity;
                pitch -= Input.GetAxisRaw("Mouse Y") * sensitivity;
                pitch = Mathf.Clamp(pitch, pitchLimits.x, pitchLimits.y);
            }
#endif
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
            }
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 desired = ConstrainToWorld(focus, focus - rotation * Vector3.forward * distance);
            Vector3 smoothed = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-followSharpness * Time.deltaTime));
            // Constrain after smoothing as well: easing must not carry the camera through a wall.
            transform.position = ConstrainToWorld(focus, smoothed);
            transform.rotation = rotation;
        }

        public Vector3 ConstrainToWorld(Vector3 focus, Vector3 desired)
        {
            Vector3 delta = desired - focus;
            float length = delta.magnitude;
            if (length < 0.001f) return focus;
            float allowed = length;
            foreach (RaycastHit hit in Physics.SphereCastAll(focus, collisionRadius, delta / length,
                         length, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.GetComponentInParent<Combatant>() != null) continue;
                allowed = Mathf.Min(allowed, Mathf.Max(0f, hit.distance - 0.05f));
            }
            return focus + delta / length * allowed;
        }
    }
}
