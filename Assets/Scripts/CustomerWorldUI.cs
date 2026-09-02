using UnityEngine;

namespace TwoCutGame
{
    /// <summary>
    /// World-Space floating UI for TwoCut customers.
    /// Draws thought bubbles, requested service icons (✂️, 🧼, 💆, 🎨),
    /// patience meters, and reaction dialogues above customer heads.
    /// </summary>
    [RequireComponent(typeof(TwoCutCustomer))]
    public class CustomerWorldUI : MonoBehaviour
    {
        private TwoCutCustomer customer;
        private Camera mainCam;

        [Header("Floating Settings")]
        public Vector3 headOffset = new Vector3(0f, 2.2f, 0f);
        public float bubbleDuration = 2.5f;

        private string currentDialogue = "";
        private float dialogueTimer = 0f;

        private static GUIStyle bubbleStyle;
        private static GUIStyle iconStyle;
        private static GUIStyle barBackgroundStyle;
        private static Texture2D whiteTexture;

        private void Awake()
        {
            customer = GetComponent<TwoCutCustomer>();
        }

        private void Start()
        {
            mainCam = Camera.main;
        }

        public void ShowDialogue(string text, float duration = 2.5f)
        {
            currentDialogue = text;
            dialogueTimer = duration;
        }

        private void Update()
        {
            if (dialogueTimer > 0f)
            {
                dialogueTimer -= Time.deltaTime;
                if (dialogueTimer <= 0f)
                {
                    currentDialogue = "";
                }
            }
        }

        private void InitializeStyles()
        {
            if (whiteTexture == null)
            {
                whiteTexture = new Texture2D(1, 1);
                whiteTexture.SetPixel(0, 0, Color.white);
                whiteTexture.Apply();
            }

            if (bubbleStyle == null)
            {
                bubbleStyle = new GUIStyle(GUI.skin.box);
                bubbleStyle.fontSize = 12;
                bubbleStyle.fontStyle = FontStyle.Bold;
                bubbleStyle.alignment = TextAnchor.MiddleCenter;
                bubbleStyle.normal.textColor = Color.white;
            }

            if (iconStyle == null)
            {
                iconStyle = new GUIStyle(GUI.skin.label);
                iconStyle.fontSize = 20;
                iconStyle.alignment = TextAnchor.MiddleCenter;
            }

            if (barBackgroundStyle == null)
            {
                barBackgroundStyle = new GUIStyle(GUI.skin.box);
            }
        }

        private void OnGUI()
        {
            if (customer == null) return;
            if (mainCam == null) mainCam = Camera.main;
            if (mainCam == null) return;

            Vector3 worldPos = transform.position + headOffset;
            Vector3 screenPos = mainCam.WorldToScreenPoint(worldPos);

            // If behind camera, do not render
            if (screenPos.z < 0) return;

            InitializeStyles();

            // Convert to GUI coordinate (Y is inverted)
            float guiX = screenPos.x;
            float guiY = Screen.height - screenPos.y;

            // 1. Service Icon + Name Tag Bubble
            string serviceEmoji = customer.GetServiceEmoji();
            string statusText = customer.GetStatusDescription();

            float boxWidth = 140f;
            float boxHeight = 44f;
            Rect bubbleRect = new Rect(guiX - boxWidth / 2f, guiY - boxHeight, boxWidth, boxHeight);

            // Background box
            Color originalBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.1f, 0.12f, 0.18f, 0.88f);
            GUI.Box(bubbleRect, GUIContent.none, bubbleStyle);

            // Icon + Text
            Rect iconRect = new Rect(bubbleRect.x + 4, bubbleRect.y + 4, 34, 34);
            GUI.Label(iconRect, serviceEmoji, iconStyle);

            Rect textRect = new Rect(bubbleRect.x + 40, bubbleRect.y + 4, bubbleRect.width - 44, 20);
            GUI.Label(textRect, $"<b>{customer.customerName}</b>");

            Rect subTextRect = new Rect(bubbleRect.x + 40, bubbleRect.y + 22, bubbleRect.width - 44, 18);
            GUI.Label(subTextRect, $"<size=10><color=#aaccff>{statusText}</color></size>");

            // 2. Patience Bar (Below bubble)
            if (customer.state != CustomerState.ServiceCompletedLeaving && customer.state != CustomerState.AngryLeaving)
            {
                float patienceRatio = Mathf.Clamp01(customer.currentPatience / customer.maxPatienceTime);
                float barWidth = 120f;
                float barHeight = 7f;
                Rect barBgRect = new Rect(guiX - barWidth / 2f, guiY + 8, barWidth, barHeight);

                // Background
                GUI.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.9f);
                GUI.DrawTexture(barBgRect, whiteTexture);

                // Patience Fill
                Color patienceColor = Color.Lerp(Color.red, Color.green, patienceRatio);
                if (patienceRatio < 0.35f)
                {
                    // Flash red when running out of patience
                    if (Mathf.Sin(Time.time * 12f) > 0) patienceColor = Color.white;
                }

                GUI.color = patienceColor;
                Rect barFillRect = new Rect(barBgRect.x + 1, barBgRect.y + 1, (barWidth - 2) * patienceRatio, barHeight - 2);
                GUI.DrawTexture(barFillRect, whiteTexture);
                GUI.color = Color.white;
            }

            // 3. Speech Bubble / Dialogue popup
            if (!string.IsNullOrEmpty(currentDialogue))
            {
                float dWidth = 160f;
                float dHeight = 28f;
                Rect dRect = new Rect(guiX - dWidth / 2f, guiY - boxHeight - dHeight - 6, dWidth, dHeight);

                GUI.backgroundColor = new Color(0.95f, 0.85f, 0.2f, 0.95f);
                GUI.Box(dRect, $"<color=black><b>{currentDialogue}</b></color>", bubbleStyle);
            }

            GUI.backgroundColor = originalBg;
        }
    }
}
