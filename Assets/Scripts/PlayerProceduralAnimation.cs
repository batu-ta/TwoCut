using UnityEngine;

namespace HairSalonGame
{
    /// <summary>
    /// Overcooked-style Procedural Squash, Stretch, Bobbing, and Waddling animation.
    /// Animates the visual child model of the player based on the Rigidbody movement velocity.
    /// This prevents physics issues because the parent collider remains stable while the visual model bobs and waddles.
    /// </summary>
    public class PlayerProceduralAnimation : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The visual child model to animate. If left empty, will automatically find the first child.")]
        public Transform modelTransform;
        
        private PlayerController playerController;
        private Rigidbody rb;
        private Vector3 baseLocalPosition;
        private Quaternion baseLocalRotation;
        private Vector3 baseLocalScale;

        [Header("Idle Settings (Breathing)")]
        public float idleSpeed = 4f;
        public float idleScaleAmount = 0.04f;

        [Header("Run Bobbing Settings (Bounce)")]
        public float runBobSpeed = 12f;
        public float runBobAmount = 0.15f;
        public float runSquashAmount = 0.08f;

        [Header("Run Waddling Settings (Left/Right Roll)")]
        public float runWaddleSpeed = 12f;
        public float runWaddleAmount = 8f; // Degrees of roll tilt

        [Header("Dash Settings")]
        public float dashLeanAmount = 25f; // Degrees of forward lean tilt
        public float dashStretchY = 1.35f;
        public float dashSquashXZ = 0.75f;
        public float dashHeightOffset = -0.3f;

        private void Start()
        {
            playerController = GetComponent<PlayerController>();
            rb = GetComponent<Rigidbody>();

            // Auto-find child model if not assigned
            if (modelTransform == null)
            {
                if (transform.childCount > 0)
                {
                    // Find the child that contains the model (e.g. "Hairdresser player")
                    modelTransform = transform.Find("Hairdresser player");
                    if (modelTransform == null)
                    {
                        modelTransform = transform.GetChild(0);
                    }
                }
                else
                {
                    Debug.LogError("PlayerProceduralAnimation: No child model found to animate!", this);
                    enabled = false;
                    return;
                }
            }

            // Save baseline transforms
            baseLocalPosition = modelTransform.localPosition;
            baseLocalRotation = modelTransform.localRotation;
            baseLocalScale = modelTransform.localScale;
        }

        private void Update()
        {
            if (modelTransform == null || rb == null || playerController == null) return;

            // Get horizontal velocity
            Vector3 horizontalVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            float speed = horizontalVel.magnitude;

            if (playerController.IsDashing)
            {
                AnimateDash();
            }
            else if (speed > 0.1f)
            {
                AnimateRun(speed);
            }
            else
            {
                AnimateIdle();
            }
        }

        private void AnimateIdle()
        {
            // Breathing effect: slow vertical squash and stretch
            float timeFactor = Time.time * idleSpeed;
            float idleScaleY = 1f + Mathf.Sin(timeFactor) * idleScaleAmount;
            
            // Volume-preserving scaling (as Y stretches, X and Z contract slightly)
            float scaleXZ = 1f / Mathf.Sqrt(idleScaleY);
            
            modelTransform.localScale = new Vector3(
                baseLocalScale.x * scaleXZ,
                baseLocalScale.y * idleScaleY,
                baseLocalScale.z * scaleXZ
            );

            // Smoothly return position and rotation to baseline
            modelTransform.localPosition = Vector3.Lerp(modelTransform.localPosition, baseLocalPosition, Time.deltaTime * 10f);
            modelTransform.localRotation = Quaternion.Slerp(modelTransform.localRotation, baseLocalRotation, Time.deltaTime * 10f);
        }

        private void AnimateRun(float currentSpeed)
        {
            // Calculate animation speed based on movement speed
            float animationSpeedModifier = Mathf.Max(0.5f, currentSpeed / playerController.moveSpeed);
            float timeFactor = Time.time * runBobSpeed * animationSpeedModifier;

            // 1. Bobbing (Up/Down bounce)
            // Using absolute value of Sine to create a bouncing curve (touching the ground and going up)
            float bobSin = Mathf.Abs(Mathf.Sin(timeFactor));
            float bobOffset = bobSin * runBobAmount;
            modelTransform.localPosition = baseLocalPosition + new Vector3(0f, bobOffset, 0f);

            // 2. Waddling (Left/Right tilt on roll axis)
            float rollAngle = Mathf.Sin(timeFactor * 0.5f) * runWaddleAmount;
            modelTransform.localRotation = baseLocalRotation * Quaternion.Euler(0f, 0f, rollAngle);

            // 3. Squash and Stretch on steps
            // When bobSin is 0 (touching the ground), we squash. When it's 1 (highest point), we stretch.
            float squashFactor = 1f - (1f - bobSin) * runSquashAmount;
            float scaleXZ = 1f / Mathf.Sqrt(squashFactor);

            modelTransform.localScale = new Vector3(
                baseLocalScale.x * scaleXZ,
                baseLocalScale.y * squashFactor,
                baseLocalScale.z * scaleXZ
            );
        }

        private void AnimateDash()
        {
            // Forward lean
            Quaternion targetRot = baseLocalRotation * Quaternion.Euler(dashLeanAmount, 0f, 0f);
            modelTransform.localRotation = Quaternion.Slerp(modelTransform.localRotation, targetRot, Time.deltaTime * 20f);

            // Forward stretch and squash
            Vector3 targetScale = new Vector3(
                baseLocalScale.x * dashSquashXZ,
                baseLocalScale.y * dashStretchY,
                baseLocalScale.z * dashSquashXZ
            );
            modelTransform.localScale = Vector3.Lerp(modelTransform.localScale, targetScale, Time.deltaTime * 20f);

            // Low-to-ground position offset
            Vector3 targetPos = baseLocalPosition + new Vector3(0f, dashHeightOffset, 0f);
            modelTransform.localPosition = Vector3.Lerp(modelTransform.localPosition, targetPos, Time.deltaTime * 20f);
        }

        private void OnDisable()
        {
            // Reset to default on disable/destroy
            if (modelTransform != null)
            {
                modelTransform.localPosition = baseLocalPosition;
                modelTransform.localRotation = baseLocalRotation;
                modelTransform.localScale = baseLocalScale;
            }
        }
    }
}
