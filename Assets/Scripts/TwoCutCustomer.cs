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

        [Header("Movement & Queue Settings")]
        [HideInInspector] public Vector3 targetPosition;
        [HideInInspector] public float moveSpeed = 5f;
        [HideInInspector] public bool isSeated = false;

        private Renderer customerRenderer;

        private void Start()
        {
            currentPatience = maxPatienceTime;
            customerRenderer = GetComponent<Renderer>();

            Debug.Log($"[TwoCut Customer] {customerName} dükkana geldi! İstenen İşlem 1: {firstServiceNeeded} | İstenen İşlem 2: {(needsSecondService ? secondServiceNeeded.ToString() : "Yok")}");
        }

        private void Update()
        {
            // Yumuşak Yürüme Mekaniği
            if (transform.parent == null)
            {
                // Sırada beklerken dünya koordinatlarında hareket et
                if (Vector3.Distance(transform.position, targetPosition) > 0.05f)
                {
                    transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
                    Vector3 dir = (targetPosition - transform.position).normalized;
                    dir.y = 0;
                    if (dir.sqrMagnitude > 0.001f)
                    {
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 8f);
                    }
                }
            }
            else
            {
                // Koltuğa atandıktan sonra koltuğun yerel pozisyonuna (Y=0.6f) yürü
                if (Vector3.Distance(transform.localPosition, targetPosition) > 0.05f)
                {
                    transform.localPosition = Vector3.MoveTowards(transform.localPosition, targetPosition, moveSpeed * Time.deltaTime);
                    transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.identity, Time.deltaTime * 8f);
                }
            }

            if (isAllServicesDone) return;

            // Sabır Azalma Mekaniği
            if (isSeated)
            {
                // Koltuktayken kirlilik oranına göre sabır düşer
                float penalty = DirtCleanerSystem.Instance != null ? DirtCleanerSystem.Instance.GetPatiencePenaltyFactor() : 1.0f;
                currentPatience -= Time.deltaTime * penalty;
            }
            else
            {
                // Sırada beklerken sabır çok daha yavaş düşer (örneğin 0.3x hızda)
                currentPatience -= Time.deltaTime * 0.3f;
            }

            if (currentPatience <= 0f)
            {
                LeaveAngry();
            }
        }

        public void PerformServiceStep(SalonItem toolUsed, ServiceType stationService)
        {
            if (isAllServicesDone) return;

            ServiceType targetService = !isFirstServiceDone ? firstServiceNeeded : secondServiceNeeded;

            // 1. Koltuk ile müşterinin istediği hizmet uyuşuyor mu?
            if (stationService != targetService)
            {
                Debug.LogWarning($"[TwoCut Customer] Yanlış koltuktayız! Müşterinin istediği: {targetService}");
                return;
            }

            // 2. Doğru alet elinizde mi kontrolü
            if (targetService == ServiceType.Haircut)
            {
                if (toolUsed == null || toolUsed.itemType != ItemType.Scissors)
                {
                    Debug.LogWarning("[TwoCut Customer] Saç kesmek için elinizde MAKAS (Scissors) olmalı!");
                    return;
                }
            }
            else if (targetService == ServiceType.HairWash)
            {
                if (toolUsed == null || toolUsed.itemType != ItemType.ShampooBottle)
                {
                    Debug.LogWarning("[TwoCut Customer] Saç yıkamak için elinizde ŞAMPUAN (ShampooBottle) olmalı!");
                    return;
                }
            }
            else if (targetService == ServiceType.HairDye)
            {
                if (toolUsed == null || (toolUsed.itemType != ItemType.DyeBottle_Red && toolUsed.itemType != ItemType.DyeBottle_Blonde))
                {
                    Debug.LogWarning("[TwoCut Customer] Boyama yapmak için elinizde BOYA (DyeBottle) olmalı!");
                    return;
                }
            }

            // Altın Makas yükseltmesi varsa saç kesim hızını 2 kat yap
            int progressIncrement = 1;
            if (targetService == ServiceType.Haircut && TwoCutShopUpgradeManager.Instance != null && TwoCutShopUpgradeManager.Instance.hasGoldenScissors)
            {
                progressIncrement = 2;
            }

            currentActions += progressIncrement;
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
            
            // Eğer sıradayken sabrı bittiyse sıradan çıkar ve arkadaki sırayı kaydır
            if (SalonGameManager.Instance != null && SalonGameManager.Instance.waitingQueue.Contains(this))
            {
                SalonGameManager.Instance.waitingQueue.Remove(this);
                SalonGameManager.Instance.UpdateQueuePositions();
            }

            Destroy(gameObject);
        }
    }
}
