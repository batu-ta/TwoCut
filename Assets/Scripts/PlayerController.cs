using UnityEngine;

namespace HairSalonGame
{
    /// <summary>
    /// Overcooked-style Top-Down Hairdresser Movement Controller with Online Multiplayer Support.
    /// Controls local player (WASD) and prepares network sync architecture for Player 2 (Online Partner).
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Multiplayer / Network Setup")]
        [Tooltip("If true, this character is controlled locally on this PC. If false, controlled by online player.")]
        public bool isLocalPlayer = true;

        [Tooltip("Player index (1 = Local Host Player, 2 = Online Client Partner).")]
        public int playerIndex = 1;

        [Header("Movement Configuration")]
        public float moveSpeed = 8.5f;
        public float rotationSpeed = 16f;

        [Header("Dash / Rush Settings")]
        public float dashForce = 18f;
        public float dashDuration = 0.15f;
        public float dashCooldown = 0.5f;

        [Header("Camera Reference")]
        public Camera mainCamera;

        private Rigidbody rb;
        private Vector3 moveInput;
        private Vector3 moveDirection;
        private bool isDashing;
        private float dashTimer;
        private float cooldownTimer;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
        }

        private void Update()
        {
            // If this is an online partner player object, ignore local keyboard WASD input!
            if (!isLocalPlayer) return;

            HandleInput();
            HandleDashTimer();
        }

        private void FixedUpdate()
        {
            if (!isLocalPlayer) return;

            if (isDashing)
            {
                rb.linearVelocity = transform.forward * dashForce;
            }
            else
            {
                MovePlayer();
            }
        }

        private void HandleInput()
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");

            moveInput = new Vector3(horizontal, 0f, vertical).normalized;

            // Dash / Quick Rush (Left Shift)
            if (Input.GetKeyDown(KeyCode.LeftShift) && cooldownTimer <= 0f && moveInput.sqrMagnitude > 0.1f)
            {
                isDashing = true;
                dashTimer = dashDuration;
                cooldownTimer = dashCooldown;
            }

            if (moveInput.sqrMagnitude > 0.01f)
            {
                Vector3 cameraForward = mainCamera != null ? mainCamera.transform.forward : Vector3.forward;
                Vector3 cameraRight = mainCamera != null ? mainCamera.transform.right : Vector3.right;

                cameraForward.y = 0f;
                cameraRight.y = 0f;
                cameraForward.Normalize();
                cameraRight.Normalize();

                moveDirection = (cameraForward * moveInput.z + cameraRight * moveInput.x).normalized;
            }
            else
            {
                moveDirection = Vector3.zero;
            }
        }

        private void MovePlayer()
        {
            Vector3 targetVelocity = moveDirection * moveSpeed;
            rb.linearVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);

            if (moveDirection.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * rotationSpeed);
            }
        }

        private void HandleDashTimer()
        {
            if (cooldownTimer > 0) cooldownTimer -= Time.deltaTime;
            if (isDashing)
            {
                dashTimer -= Time.deltaTime;
                if (dashTimer <= 0) isDashing = false;
            }
        }
    }
}
