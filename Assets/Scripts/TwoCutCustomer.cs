using UnityEngine;
using HairSalonGame;

namespace TwoCutGame
{
    public enum ServiceType
    {
        Haircut,    // Saç Kesimi (Makas ile)
        HairWash,   // Saç Yıkama (Şampuan & Yıkama Koltuğu)
        HairDye,    // Saç Boyama (Boya Şişesi & Masası)
        Massage     // Masaj (Masaj Koltuğu & Rahatlatıcı Masaj Aleti)
    }

    /// <summary>
    /// TwoCut Customer NPC script.
    /// Manages requested services (Haircut, Wash, Dye, Massage), patience meter, dirt spawning, and payment.
    /// </summary>
    public class TwoCutCustomer : MonoBehaviour
    {
        [Header("Customer Identification")]
        public string customerName = "Müşteri";

        [Header("Requested Services")]
        public ServiceType firstServiceNeeded = ServiceType.Haircut;
        public bool needsSecondService = false;
        public ServiceType secondServiceNeeded = ServiceType.Massage;

        [HideInInspector] public bool isFirstServiceDone = false;
        [HideInInspector] public bool isAllServicesDone = false;

        [Header("Patience Countdown")]
        public float maxPatienceTime = 40f;
        public float currentPatience;

        [Header("Action Progress")]
        public int requiredActions = 5;
        public int currentActions = 0;

        [Header("Payment")]
        public int paymentAmount = 60;
        public int tipBonus = 25;

        private Renderer customerRenderer;

        private void Start()
        {
            currentPatience = maxPatienceTime;
            customerRenderer = GetComponent<Renderer>();

            Debug.Log($"[TwoCut Customer] {customerName} dükkana geldi! İstenen İşlem 1: {firstServiceNeeded} | İstenen İşlem 2: {(needsSecondService ? secondServiceNeeded.ToString() : "Yok")}");
        }

        private void Update()
        {
            if (isAllServicesDone) return;

            // Reduce patience (affected by floor dirtiness!)
            float penalty = DirtCleanerSystem.Instance != null ? DirtCleanerSystem.Instance.GetPatiencePenaltyFactor() : 1.0f;
            currentPatience -= Time.deltaTime * penalty;

            if (currentPatience <= 0f)
            {
                LeaveAngry();
            }
        }

        public void PerformServiceStep(SalonItem toolUsed, ServiceType stationService)
        {
            if (isAllServicesDone) return;

            ServiceType targetService = !isFirstServiceDone ? firstServiceNeeded : secondServiceNeeded;

            // Check if station matches required service
            if (stationService != targetService)
            {
                Debug.LogWarning($"[TwoCut Customer] Yanlış koltuktayız! Müşterinin istediği: {targetService}");
                return;
            }

            currentActions++;
            Debug.Log($"[TwoCut Customer] İşlem yapılıyor... ({currentActions}/{requiredActions})");

            // Visual feedback
            if (customerRenderer != null)
            {
                if (targetService == ServiceType.HairDye) customerRenderer.material.color = Color.magenta;
                else if (targetService == ServiceType.Massage) customerRenderer.material.color = Color.cyan;
                else if (targetService == ServiceType.Haircut) transform.localScale = Vector3.one * 0.9f;
            }

            if (currentActions >= requiredActions)
            {
                currentActions = 0;

                if (!isFirstServiceDone)
                {
                    isFirstServiceDone = true;

                    // Spawn hair clipping mess on floor for Haircut/Dye
                    if (targetService == ServiceType.Haircut || targetService == ServiceType.HairDye)
                    {
                        DirtCleanerSystem.Instance?.SpawnHairClippingDirt(transform.position);
                    }

                    if (!needsSecondService)
                    {
                        CompleteAllServices();
                    }
                    else
                    {
                        Debug.Log($"[TwoCut Customer] 1. Hizmet bitti! Şimdi 2. Hizmet: {secondServiceNeeded}");
                    }
                }
                else
                {
                    CompleteAllServices();
                }
            }
        }

        private void CompleteAllServices()
        {
            isAllServicesDone = true;

            int totalPay = paymentAmount;
            if (needsSecondService) totalPay += 45;

            // Add tip if patience > 50%
            if (currentPatience > maxPatienceTime * 0.5f)
            {
                totalPay += tipBonus;
                Debug.Log($"⭐ [TwoCut Customer] Harika hizmet! Bahşişli Ödeme: ${totalPay}");
            }
            else
            {
                Debug.Log($"✅ [TwoCut Customer] Hizmet tamamlandı: ${totalPay}");
            }

            TwoCutEconomyManager.Instance?.AddEarnings(totalPay);
            Destroy(gameObject, 1.2f);
        }

        private void LeaveAngry()
        {
            Debug.LogWarning($"😡 [TwoCut Customer] {customerName} sabrı tükendi ve sinirle dükkanı terk etti!");
            Destroy(gameObject);
        }
    }
}
