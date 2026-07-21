using UnityEngine;

namespace TwoCutGame
{
    public enum UpgradeType
    {
        GoldenScissors,
        MassageChair,
        FloorCleaningBot,
        AssistantHelper
    }

    public class TwoCutShopUpgradeManager : MonoBehaviour
    {
        public static TwoCutShopUpgradeManager Instance { get; private set; }

        public bool hasGoldenScissors = false;
        public bool hasMassageChair = false;
        public bool hasFloorCleaningBot = false;
        public bool hasAssistantHelper = false;

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
                    case UpgradeType.GoldenScissors: hasGoldenScissors = true; break;
                    case UpgradeType.MassageChair: hasMassageChair = true; break;
                    case UpgradeType.FloorCleaningBot: hasFloorCleaningBot = true; break;
                    case UpgradeType.AssistantHelper: hasAssistantHelper = true; break;
                }
                return true;
            }

            return false;
        }
    }
}
