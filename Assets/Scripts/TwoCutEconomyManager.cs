using UnityEngine;

namespace TwoCutGame
{
    /// <summary>
    /// TwoCut Economy, Debt & Daily Installment Manager.
    /// Manages total shop debt, daily rent/installment target, daily shift timer, and bankruptcy check.
    /// </summary>
    public class TwoCutEconomyManager : MonoBehaviour
    {
        public static TwoCutEconomyManager Instance { get; private set; }

        [Header("Debt & Bank Loan Settings")]
        [Tooltip("Total remaining bank loan debt to pay off.")]
        public int totalLoanDebt = 5000;

        [Tooltip("Current day number (Day 1, Day 2...).")]
        public int currentDay = 1;

        [Tooltip("Target money to pay for today's debt installment.")]
        public int dailyInstallmentTarget = 250;

        [Header("Daily Revenue & Cash")]
        public int currentVaultMoney = 100; // Starting capital
        public int todayEarnings = 0;

        [Header("Shift Timer")]
        [Tooltip("Length of 1 work day shift in seconds (e.g. 180s = 3 mins).")]
        public float shiftDurationSeconds = 180f;
        public float timeRemaining;

        [Header("Bankrupt Warning")]
        public bool isBankrupt = false;
        public bool isShiftEnded = false;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            timeRemaining = shiftDurationSeconds;
            Debug.Log($"[TwoCut] Gün {currentDay} başladı! Bugunku Taksit Hedefi: ${dailyInstallmentTarget} | Kalan Toplam Borç: ${totalLoanDebt}");
        }

        private void Update()
        {
            if (isShiftEnded || isBankrupt) return;

            if (timeRemaining > 0)
            {
                timeRemaining -= Time.deltaTime;
            }
            else
            {
                EndDayShift();
            }
        }

        public void AddEarnings(int amount)
        {
            todayEarnings += amount;
            currentVaultMoney += amount;
            Debug.Log($"[TwoCut] Gelir: +${amount} | Bugunku Toplam: ${todayEarnings} | Kasa: ${currentVaultMoney}");
        }

        public void EndDayShift()
        {
            isShiftEnded = true;
            Debug.Log($"=== GÜN {currentDay} KAPANIŞI ===");
            Debug.Log($"Bugünün Kazancı: ${todayEarnings}");
            Debug.Log($"Ödenmesi Gereken Taksit: ${dailyInstallmentTarget}");

            // Deduct daily installment
            if (currentVaultMoney >= dailyInstallmentTarget)
            {
                currentVaultMoney -= dailyInstallmentTarget;
                totalLoanDebt -= dailyInstallmentTarget;
                if (totalLoanDebt < 0) totalLoanDebt = 0;

                Debug.Log($"✅ Taksit Ödendi! Kalan Borç: ${totalLoanDebt} | Kasa Bakiyesi: ${currentVaultMoney}");
            }
            else
            {
                // Failed to pay installment!
                int deficit = dailyInstallmentTarget - currentVaultMoney;
                currentVaultMoney = 0;
                totalLoanDebt += deficit; // Interest penalty added to debt
                Debug.LogWarning($"⚠️ TAKSİT ÖDENEMEDİ! Açık: ${deficit} Borca Eklendi! Toplam Borç: ${totalLoanDebt}");

                if (totalLoanDebt >= 8000) // Bankruptcy threshold
                {
                    isBankrupt = true;
                    Debug.LogError("❌ İFLAS EDİLDİ! Borçlar ödenemedi. Oyun Bitti!");
                }
            }
        }

        public void StartNextDay()
        {
            currentDay++;
            todayEarnings = 0;
            dailyInstallmentTarget += 75; // Increasing difficulty
            timeRemaining = shiftDurationSeconds;
            isShiftEnded = false;

            Debug.Log($"[TwoCut] Gün {currentDay} Başladı! Yeni Taksit: ${dailyInstallmentTarget}");
        }
    }
}
