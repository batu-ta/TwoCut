using UnityEngine;

namespace TwoCutGame
{
    public class TwoCutEconomyManager : MonoBehaviour
    {
        public static TwoCutEconomyManager Instance { get; private set; }

        [Header("Debt & Bank Loan Settings")]
        public int totalLoanDebt = 5000;
        public int currentDay = 1;
        public int dailyInstallmentTarget = 250;

        [Header("Daily Revenue & Cash")]
        public int currentVaultMoney = 100;
        public int todayEarnings = 0;

        [Header("Shift Timer")]
        public float shiftDurationSeconds = 180f;
        public float timeRemaining;

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

            if (currentVaultMoney >= dailyInstallmentTarget)
            {
                currentVaultMoney -= dailyInstallmentTarget;
                totalLoanDebt -= dailyInstallmentTarget;
                if (totalLoanDebt < 0) totalLoanDebt = 0;
            }
            else
            {
                int deficit = dailyInstallmentTarget - currentVaultMoney;
                currentVaultMoney = 0;
                totalLoanDebt += deficit;

                if (totalLoanDebt >= 8000)
                {
                    isBankrupt = true;
                }
            }
        }

        public void StartNextDay()
        {
            currentDay++;
            todayEarnings = 0;
            dailyInstallmentTarget += 75;
            timeRemaining = shiftDurationSeconds;
            isShiftEnded = false;
        }
    }
}
