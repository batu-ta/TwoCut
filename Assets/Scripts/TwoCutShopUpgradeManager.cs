using UnityEngine;

namespace TwoCutGame
{
    public enum UpgradeType
    {
        GoldenScissors,     // Altın Makas (Saç kesimini 2 kat hızlandırır)
        MassageChair,       // Masaj Koltuğu (Yeni Masaj hizmetini açar)
        FloorCleaningBot,   // Otomatik Temizlik Makinesi (Yerleri otomatik süpürür)
        AssistantHelper     // Yardımcı Çırak (İş akışını hızlandırır)
    }

    /// <summary>
    /// TwoCut Shop Upgrades & Staff Hiring Manager.
    /// Allows buying new tools, unlocking massage chairs, and hiring assistants with shop profits.
    /// </summary>
    public class TwoCutShopUpgradeManager : MonoBehaviour
    {
        public static TwoCutShopUpgradeManager Instance { get; private set; }

        [Header("Upgrade Status")]
        public bool hasGoldenScissors = false;
        public bool hasMassageChair = false;
        public bool hasFloorCleaningBot = false;
        public bool hasAssistantHelper = false;

        [Header("Upgrade Costs")]
        public int goldenScissorsCost = 200;
        public int massageChairCost = 350;
        public int cleaningBotCost = 300;
        public int assistantCost = 400;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public bool TryPurchaseUpgrade(UpgradeType upgrade)
        {
            if (TwoCutEconomyManager.Instance == null) return false;

            int cost = 0;
            switch (upgrade)
            {
                case UpgradeType.GoldenScissors: cost = goldenScissorsCost; break;
                case UpgradeType.MassageChair: cost = massageChairCost; break;
                case UpgradeType.FloorCleaningBot: cost = cleaningBotCost; break;
                case UpgradeType.AssistantHelper: cost = assistantCost; break;
            }

            if (TwoCutEconomyManager.Instance.currentVaultMoney >= cost)
            {
                TwoCutEconomyManager.Instance.currentVaultMoney -= cost;

                switch (upgrade)
                {
                    case UpgradeType.GoldenScissors:
                        hasGoldenScissors = true;
                        Debug.Log("✨ [Upgrade] Altın Makas satın alındı! Kesim hızı 2 katına çıktı!");
                        break;
                    case UpgradeType.MassageChair:
                        hasMassageChair = true;
                        Debug.Log("🛋️ [Upgrade] Masaj Koltuğu kuruldu! Masaj hizmeti aktif!");
                        break;
                    case UpgradeType.FloorCleaningBot:
                        hasFloorCleaningBot = true;
                        Debug.Log("🤖 [Upgrade] Otomatik Temizleyici alındı! Zemin temizliği aktif!");
                        break;
                    case UpgradeType.AssistantHelper:
                        hasAssistantHelper = true;
                        Debug.Log("👨‍🍳 [Upgrade] Yardımcı Çırak işe alındı!");
                        break;
                }
                return true;
            }

            Debug.LogWarning($"⚠️ [Upgrade] Yetersiz Bakiye! Gereken: ${cost} | Kasadaki: ${TwoCutEconomyManager.Instance.currentVaultMoney}");
            return false;
        }
    }
}
