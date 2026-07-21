using UnityEngine;

namespace TwoCutGame
{
    /// <summary>
    /// TwoCut HUD & Game UI Manager.
    /// Draws clean OnGUI overlays for Day Number, Money Vault, Debt Target, Shift Timer, and Cleanliness.
    /// </summary>
    public class TwoCutGameUI : MonoBehaviour
    {
        private void OnGUI()
        {
            if (TwoCutEconomyManager.Instance == null) return;

            var eco = TwoCutEconomyManager.Instance;
            var dirt = DirtCleanerSystem.Instance;

            // Define UI Style
            GUIStyle headerStyle = new GUIStyle(GUI.skin.box);
            headerStyle.fontSize = 16;
            headerStyle.fontStyle = FontStyle.Bold;
            headerStyle.alignment = TextAnchor.MiddleLeft;

            // Top Left Dashboard Box
            GUILayout.BeginArea(new Rect(20, 20, 360, 210), headerStyle);
            GUILayout.Label($"✂️  <b>TWOCUT - DÜKKAN YÖNETİMİ</b>", GUILayout.Height(30));
            GUILayout.Label($"📅 <b>Gün:</b> {eco.currentDay} / 7");
            GUILayout.Label($"⏳ <b>Kalan Vardiya Süresi:</b> {Mathf.CeilToInt(eco.timeRemaining)} saniye");
            GUILayout.Space(5);
            GUILayout.Label($"💰 <b>Kasa Bakiyesi:</b> ${eco.currentVaultMoney}");
            GUILayout.Label($"🎯 <b>Bugünkü Taksit Hedefi:</b> ${eco.dailyInstallmentTarget} (Kazanç: ${eco.todayEarnings})");
            GUILayout.Label($"🏦 <b>Kalan Banka Borcu:</b> ${eco.totalLoanDebt}");
            GUILayout.Space(5);

            if (dirt != null)
            {
                string cleanStatus = dirt.cleanlinessPercent > 70 ? "<color=green>Pırıl Pırıl (% " + dirt.cleanlinessPercent.ToString("F0") + ")</color>" : "<color=red>Kirli (% " + dirt.cleanlinessPercent.ToString("F0") + ")</color>";
                GUILayout.Label($"🧹 <b>Temizlik Oranı:</b> {cleanStatus}");
            }

            GUILayout.EndArea();

            // Shift End or Bankrupt Popup
            if (eco.isBankrupt)
            {
                GUI.Box(new Rect(Screen.width / 2 - 180, Screen.height / 2 - 80, 360, 160), "❌ İFLAS EDİLDİ!\n\nBorç taksitleri ödenemedi dükkan icralık oldu!\nTekrar başlamak için Play'e yeniden basın.");
            }
            else if (eco.isShiftEnded)
            {
                if (GUI.Button(new Rect(Screen.width / 2 - 150, Screen.height / 2 - 40, 300, 80), $"✅ GÜN {eco.currentDay} BİTTİ!\nTaksit Ödendi.\nSonraki Güne Geçmek İçin Tıklayın"))
                {
                    eco.StartNextDay();
                }
            }
        }
    }
}
