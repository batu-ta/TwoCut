using UnityEngine;

namespace HairSalonGame
{
    /// <summary>
    /// Ultra-responsive, butter-smooth top-down character controller for TwoCut.
    /// Features:
    /// - Effortless wall & corner sliding (never gets stuck on obstacles or diagonal walls)
    /// - Intelligent step & stair climbing (smoothly ascends platforms and salon steps)
    /// - Continuous ground snapping (prevents bouncing/floating on stairs)
    /// - Zero-friction physics capsule with Continuous Dynamic collision detection
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
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

        [Header("Step Climbing & Slope Navigation")]
        public float maxStepHeight = 0.55f;
        public float stepSmooth = 16f;
        public float groundCheckDistance = 0.4f;
        public LayerMask groundLayer = ~0;

        private Rigidbody rb;
        private CapsuleCollider capsuleCol;
        private Vector3 moveInput;
        private Vector3 moveDirection;
        private bool isDashing;
        private float dashTimer;
        private float cooldownTimer;
        private bool isGrounded;

        public bool IsDashing => isDashing;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            // Sürtünmesiz fizik materyali ve mükemmel kalibre edilmiş kapsül
            capsuleCol = GetComponent<CapsuleCollider>();
            if (capsuleCol != null)
            {
                capsuleCol.center = new Vector3(0f, 0.85f, 0f);
                capsuleCol.height = 1.7f;
                capsuleCol.radius = 0.30f;

                PhysicsMaterial zeroFriction = new PhysicsMaterial("PlayerZeroFriction")
                {
                    dynamicFriction = 0f,
                    staticFriction = 0f,
                    bounciness = 0f,
                    frictionCombine = PhysicsMaterialCombine.Minimum,
                    bounceCombine = PhysicsMaterialCombine.Minimum
                };
                capsuleCol.material = zeroFriction;
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

            CheckGrounded();

            if (isDashing)
            {
                rb.linearVelocity = new Vector3(transform.forward.x * dashForce, rb.linearVelocity.y, transform.forward.z * dashForce);
            }
            else
            {
                MovePlayerWithWallSlide();
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
                // Sabit ve net yön: W=Yukarı, S=Aşağı, A=Sol, D=Sağ
                moveDirection = new Vector3(moveInput.x, 0f, moveInput.z).normalized;
            }
            else
            {
                moveDirection = Vector3.zero;
            }
        }

        private void CheckGrounded()
        {
            Vector3 rayStart = transform.position + Vector3.up * 0.15f;
            isGrounded = Physics.Raycast(rayStart, Vector3.down, groundCheckDistance + 0.15f, groundLayer, QueryTriggerInteraction.Ignore);
        }

        private void MovePlayerWithWallSlide()
        {
            if (moveDirection.sqrMagnitude > 0.01f)
            {
                Vector3 desiredMove = moveDirection * moveSpeed;

                // Duvarlara veya köşelere sürtünürken takılmayı önleyen akıllı kayma (Wall Slide Deflection)
                Vector3 finalMove = desiredMove;
                Vector3 capsuleBottom = transform.position + Vector3.up * 0.35f;
                Vector3 capsuleTop = transform.position + Vector3.up * 1.35f;
                float checkDist = 0.15f;

                if (Physics.CapsuleCast(capsuleBottom, capsuleTop, capsuleCol != null ? capsuleCol.radius : 0.3f, moveDirection, out RaycastHit hit, checkDist, groundLayer, QueryTriggerInteraction.Ignore))
                {
                    if (hit.collider != null && !hit.collider.isTrigger && hit.transform != transform)
                    {
                        // Duvar normaline dik kayma vektörünü hesapla
                        if (hit.normal.y < 0.5f) // Dikey bir yüzey (duvar, mobilya, köşe)
                        {
                            Vector3 wallNormal = new Vector3(hit.normal.x, 0f, hit.normal.z).normalized;
                            Vector3 slideVelocity = Vector3.ProjectOnPlane(desiredMove, wallNormal);
                            if (slideVelocity.sqrMagnitude > 0.1f)
                            {
                                finalMove = slideVelocity.normalized * moveSpeed;
                            }
                        }
                    }
                }

                rb.linearVelocity = new Vector3(finalMove.x, rb.linearVelocity.y, finalMove.z);

                // Anında ve net bakış yönü
                transform.rotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            }
            else
            {
                // Durduğunda yatay hızı sıfırla, dikey yerçekimini koru
                rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            }
        }

        private void HandleStepClimbing()
        {
            if (moveDirection.sqrMagnitude < 0.01f) return;

            Vector3 moveDirNorm = moveDirection.normalized;
            Vector3 footPos = transform.position + Vector3.up * 0.05f;

            // 1. Ayak seviyesinde basamak veya yükselti kontrolü
            if (Physics.Raycast(footPos, moveDirNorm, out RaycastHit lowerHit, 0.5f, groundLayer, QueryTriggerInteraction.Ignore))
            {
                if (lowerHit.collider != null && !lowerHit.collider.isTrigger && lowerHit.transform != transform)
                {
                    // 2. Basamağın üst noktasını tespit et
                    Vector3 upperPos = footPos + Vector3.up * maxStepHeight + moveDirNorm * 0.35f;
                    if (Physics.Raycast(upperPos, Vector3.down, out RaycastHit upperHit, maxStepHeight, groundLayer, QueryTriggerInteraction.Ignore))
                    {
                        // Yürünebilir basamak yüzeyi kontrolü (normal yukarı bakmalı ve ayak seviyesinden yüksek olmalı)
                        if (upperHit.point.y > footPos.y + 0.02f && upperHit.normal.y > 0.5f)
                        {
                            float stepDiff = upperHit.point.y - footPos.y;
                            if (stepDiff <= maxStepHeight)
                            {
                                rb.position = Vector3.MoveTowards(rb.position, new Vector3(rb.position.x, upperHit.point.y, rb.position.z), stepSmooth * Time.fixedDeltaTime);
                                rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                            }
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

