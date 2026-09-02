using UnityEngine;

namespace HairSalonGame
{
    /// <summary>
    /// Direct, crisp, responsive top-down character controller for TwoCut.
    /// Pure instant facing and smooth translation with zero procedural swaying or spinning.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Multiplayer / Network Setup")]
        [Tooltip("If true, this character is controlled locally on this PC.")]
        public bool isLocalPlayer = true;

        [Tooltip("Player index (1 = Local Host Player, 2 = Online Client Partner).")]
        public int playerIndex = 1;

        [Header("Movement Configuration")]
        public float moveSpeed = 8.5f;

        [Header("Dash Settings")]
        public float dashForce = 18f;
        public float dashDuration = 0.15f;
        public float dashCooldown = 0.5f;

        [Header("Step Climbing / Stair Navigation")]
        public float maxStepHeight = 0.5f;
        public float stepSmooth = 12f;
        public LayerMask groundLayer = ~0;

        private Rigidbody rb;
        private Vector3 moveInput;
        private Vector3 moveDirection;
        private bool isDashing;
        private float dashTimer;
        private float cooldownTimer;

        public bool IsDashing => isDashing;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;

            // Sürtünmesiz fizik materyali ve doğru merkezlenmiş kapsül
            CapsuleCollider col = GetComponent<CapsuleCollider>();
            if (col != null)
            {
                col.center = new Vector3(0f, 0.9f, 0f);
                col.height = 1.8f;
                col.radius = 0.35f;

                PhysicsMaterial zeroFriction = new PhysicsMaterial("PlayerZeroFriction")
                {
                    dynamicFriction = 0f,
                    staticFriction = 0f,
                    bounciness = 0f,
                    frictionCombine = PhysicsMaterialCombine.Minimum,
                    bounceCombine = PhysicsMaterialCombine.Minimum
                };
                col.material = zeroFriction;
            }
        }

        private void Update()
        {
            if (!isLocalPlayer) return;

            HandleInput();
            HandleDashTimer();
        }

        private void FixedUpdate()
        {
            if (!isLocalPlayer) return;

            if (isDashing)
            {
                rb.linearVelocity = new Vector3(transform.forward.x * dashForce, rb.linearVelocity.y, transform.forward.z * dashForce);
            }
            else
            {
                MovePlayer();
                HandleStepClimbing();
            }
        }

        private void HandleInput()
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");

            moveInput = new Vector3(horizontal, 0f, vertical).normalized;

            // Dash (Sol Shift)
            if (Input.GetKeyDown(KeyCode.LeftShift) && cooldownTimer <= 0f && moveInput.sqrMagnitude > 0.1f)
            {
                isDashing = true;
                dashTimer = dashDuration;
                cooldownTimer = dashCooldown;
            }

            if (moveInput.sqrMagnitude > 0.01f)
            {
                // Doğrudan sabit yön: W=Yukarı, S=Aşağı, A=Sol, D=Sağ
                moveDirection = new Vector3(moveInput.x, 0f, moveInput.z).normalized;
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
                // Anında ve net bakış yönü (asla kendi etrafında dönmez, net bakar)
                transform.rotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            }
        }

        private void HandleStepClimbing()
        {
            if (moveDirection.sqrMagnitude < 0.01f) return;

            Vector3 moveDirNorm = moveDirection.normalized;
            Vector3 footPos = transform.position + Vector3.up * 0.05f;

            // Önümüzde basamak var mı kontrol et
            if (Physics.Raycast(footPos, moveDirNorm, out RaycastHit lowerHit, 0.7f, groundLayer, QueryTriggerInteraction.Ignore))
            {
                if (lowerHit.collider != null && !lowerHit.collider.isTrigger && lowerHit.transform != transform)
                {
                    Vector3 upperPos = footPos + Vector3.up * maxStepHeight + moveDirNorm * 0.4f;
                    if (Physics.Raycast(upperPos, Vector3.down, out RaycastHit upperHit, maxStepHeight, groundLayer, QueryTriggerInteraction.Ignore))
                    {
                        if (upperHit.point.y > footPos.y + 0.02f && upperHit.normal.y > 0.3f)
                        {
                            float stepDiff = upperHit.point.y - footPos.y;
                            rb.position = Vector3.MoveTowards(rb.position, rb.position + Vector3.up * stepDiff, stepSmooth * Time.fixedDeltaTime);
                            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                        }
                    }
                }
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
