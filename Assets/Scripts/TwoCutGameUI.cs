using UnityEngine;
using HairSalonGame;

namespace TwoCutGame
{
    /// <summary>
    /// TwoCut Modern HUD & Action Banner UI.
    /// Draws clean top dashboard (Day, Vault, Debt, Shift, Cleanliness)
    /// and bottom contextual interaction hints ([E] Yönlendir, [E] Al, [G] Bırak, [F] Kes).
    /// </summary>
    public class TwoCutGameUI : MonoBehaviour
    {
        private GUIStyle headerStyle;
        private GUIStyle bannerStyle;
        private GUIStyle popupStyle;
        private Texture2D darkBgTex;

        private void InitializeStyles()
        {
            if (darkBgTex == null)
            {
                darkBgTex = new Texture2D(1, 1);
                darkBgTex.SetPixel(0, 0, new Color(0.08f, 0.1f, 0.15f, 0.92f));
                darkBgTex.Apply();
            }

            if (headerStyle == null)
            {
                headerStyle = new GUIStyle(GUI.skin.box);
                headerStyle.normal.background = darkBgTex;
                headerStyle.normal.textColor = Color.white;
                headerStyle.fontSize = 13;
                headerStyle.padding = new RectOffset(12, 12, 10, 10);
                headerStyle.alignment = TextAnchor.UpperLeft;
            }

            if (bannerStyle == null)
            {
                bannerStyle = new GUIStyle(GUI.skin.box);
                bannerStyle.normal.background = darkBgTex;
                bannerStyle.normal.textColor = new Color(1f, 0.95f, 0.4f); // Golden yellow text
                bannerStyle.fontSize = 15;
                bannerStyle.fontStyle = FontStyle.Bold;
                bannerStyle.alignment = TextAnchor.MiddleCenter;
            }

            if (popupStyle == null)
            {
                popupStyle = new GUIStyle(GUI.skin.box);
                popupStyle.normal.background = darkBgTex;
                popupStyle.normal.textColor = Color.white;
                popupStyle.fontSize = 16;
                popupStyle.fontStyle = FontStyle.Bold;
                popupStyle.alignment = TextAnchor.MiddleCenter;
            }
        }

        private void OnGUI()
        {
            InitializeStyles();

            var eco = TwoCutEconomyManager.Instance;
            var dirt = DirtCleanerSystem.Instance;

            // 1. Top-Left Economy & Salon Status Dashboard
            if (eco != null)
            {
                GUILayout.BeginArea(new Rect(20, 20, 380, 220), headerStyle);
                GUILayout.Label("✂️ <b><size=15>TWOCUT - DÜKKAN YÖNETİMİ</size></b>", GUILayout.Height(24));
                GUILayout.Space(2);
                GUILayout.Label($"📅 <b>Gün:</b> {eco.currentDay} / 7   |   ⏳ <b>Kalan Süre:</b> {Mathf.CeilToInt(eco.timeRemaining)} saniye");
                GUILayout.Space(4);
                GUILayout.Label($"💵 <b>Kasa Bakiyesi:</b> <color=#55ff88>${eco.currentVaultMoney}</color> (Bugünkü Kazanç: ${eco.todayEarnings})");
                GUILayout.Label($"🎯 <b>Bugünkü Taksit Hedefi:</b> ${eco.dailyInstallmentTarget}");
                GUILayout.Label($"🏦 <b>Kalan Banka Borcu:</b> <color=#ff7777>${eco.totalLoanDebt}</color>");
                GUILayout.Space(4);

                if (dirt != null)
                {
                    string cleanText = dirt.cleanlinessPercent > 65
                        ? $"<color=#55ff55>Pırıl Pırıl (%{dirt.cleanlinessPercent:F0})</color>"
                        : $"<color=#ff5555>Kirli (%{dirt.cleanlinessPercent:F0}) - Müşteriler sabırsızlanıyor!</color>";
                    GUILayout.Label($"🧹 <b>Dükkan Temizliği:</b> {cleanText}");
                }

                GUILayout.EndArea();
            }

            // 2. Bottom-Center Contextual Action Prompt Banner
            PlayerInteraction player = FindFirstObjectByType<PlayerInteraction>();
            if (player != null && !string.IsNullOrEmpty(player.contextualHint))
            {
                float bannerWidth = 480f;
                float bannerHeight = 44f;
                float bx = (Screen.width - bannerWidth) / 2f;
                float by = Screen.height - bannerHeight - 30f;

                GUI.Box(new Rect(bx, by, bannerWidth, bannerHeight), $"💡 {player.contextualHint}", bannerStyle);
            }

            // 3. Shift End or Bankrupt Popups
            if (eco != null)
            {
                if (eco.isBankrupt)
                {
                    float pw = 420f;
                    float ph = 180f;
                    Rect popRect = new Rect((Screen.width - pw) / 2f, (Screen.height - ph) / 2f, pw, ph);
                    GUI.Box(popRect, "💥 <b>İFLAS EDİLDİ!</b>\n\nBanka borç taksitleri ödenemedi ve dükkan mühürlendi!\nTekrar başlamak için Play'e yeniden basın.", popupStyle);
                }
                else if (eco.isShiftEnded)
                {
                    float pw = 400f;
                    float ph = 120f;
                    Rect popRect = new Rect((Screen.width - pw) / 2f, (Screen.height - ph) / 2f, pw, ph);
                    if (GUI.Button(popRect, $"✅ <b>GÜN {eco.currentDay} TAMAMLANDI!</b>\nTaksit ödendi.\n<b>Sonraki Güne Geçmek İçin Tıklayın</b>", popupStyle))
                    {
                        eco.StartNextDay();
                    }
                }
            }
        }
    }
}
