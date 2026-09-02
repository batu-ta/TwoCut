using UnityEngine;
using HairSalonGame;

namespace TwoCutGame
{
    public enum ServiceType
    {
        Haircut,    // Saç Kesimi
        HairWash,   // Saç Yıkama
        HairDye,    // Saç Boyama
        Massage     // Masaj
    }

    public enum CustomerState
    {
        WaitingInQueue,
        WalkingToStation,
        SeatedWaitingForService,
        UndergoingService,
        ServiceCompletedLeaving,
        AngryLeaving
    }

    /// <summary>
    /// TwoCut Customer AI & State Machine.
    /// Manages customer queue waiting, being guided to a chair by the hairdresser player,
    /// receiving hair services, paying with tips, and reaction dialogues.
    /// </summary>
    [RequireComponent(typeof(CustomerWorldUI))]
    public class TwoCutCustomer : MonoBehaviour
    {
        [Header("Customer Identity")]
        public string customerName = "Müşteri";
        public CustomerState state = CustomerState.WaitingInQueue;

        [Header("Desired Services")]
        public ServiceType firstServiceNeeded = ServiceType.Haircut;
        public ServiceType secondServiceNeeded = ServiceType.Massage;
        public bool needsSecondService = false;
        public bool isFirstServiceDone = false;
        public bool isAllServicesDone = false;

        [Header("Assigned Station")]
        public SalonStation assignedStation;

        [Header("Patience Countdown")]
        public float maxPatienceTime = 45f;
        public float currentPatience;

        [Header("Action Progress")]
        public int requiredActions = 4;
        public int currentActions = 0;

        [Header("Payment & Economy")]
        public int paymentAmount = 75;
        public int tipBonus = 30;

        [Header("Movement Settings")]
        public Vector3 targetPosition;
        public float moveSpeed = 4.5f;

        private Renderer customerRenderer;
        private CustomerWorldUI worldUI;
        private static readonly string[] TurkishNames = {
            "Ahmet", "Mehmet", "Can", "Burak", "Emre", "Barış", "Deniz", "Efe", "Murat", "Oğuz"
        };

        private void Awake()
        {
            customerName = TurkishNames[Random.Range(0, TurkishNames.Length)];
            worldUI = GetComponent<CustomerWorldUI>();
            customerRenderer = GetComponentInChildren<Renderer>();

            // Karakterlerin tek sıra halinde yürürken birbirine çarpıp takılmaması için
            CapsuleCollider col = GetComponent<CapsuleCollider>();
            if (col != null)
            {
                col.isTrigger = true;
                col.center = new Vector3(0f, 0.9f, 0f);
                col.height = 1.8f;
                col.radius = 0.35f;
            }
        }

        private void Start()
        {
            currentPatience = maxPatienceTime;
            // Başlangıçta targetPosition hemen transform.position kalmasın, hedef sırasına yürüsün

            if (worldUI != null)
            {
                worldUI.ShowDialogue($"Selam! {GetServiceName(firstServiceNeeded)} istiyorum.", 3f);
            }

            TwoCutAudioManager.Instance?.PlayCustomerBell();
            Debug.Log($"[TwoCut Customer] {customerName} kapıdan girdi! İstenen: {firstServiceNeeded}");
        }

        private void Update()
        {
            HandleMovement();
            HandlePatience();
        }

        private void HandleMovement()
        {
            if (state == CustomerState.SeatedWaitingForService || state == CustomerState.UndergoingService)
            {
                // Koltuğa oturma kilidi
                if (assignedStation != null)
                {
                    Transform seat = assignedStation.itemOrCustomerPoint != null ? assignedStation.itemOrCustomerPoint : assignedStation.transform;
                    transform.position = Vector3.MoveTowards(transform.position, seat.position + Vector3.up * 0.4f, moveSpeed * Time.deltaTime);
                    transform.rotation = Quaternion.Slerp(transform.rotation, seat.rotation, Time.deltaTime * 10f);
                }
                return;
            }

            // Yürüme hareketi (Kapıdan sıraya veya sıradan koltuğa)
            float dist = Vector3.Distance(transform.position, targetPosition);
            if (dist > 0.1f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
                Vector3 dir = (targetPosition - transform.position).normalized;
                dir.y = 0;
                if (dir.sqrMagnitude > 0.001f)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 12f);
                }
            }
            else
            {
                // Hedefe varıldı
                if (state == CustomerState.WaitingInQueue)
                {
                    // Sırada dururken salona doğru baksın (+X yönü veya +Z yönü)
                    Quaternion frontFacing = Quaternion.Euler(0f, 90f, 0f);
                    transform.rotation = Quaternion.Slerp(transform.rotation, frontFacing, Time.deltaTime * 8f);
                }
                else if (state == CustomerState.WalkingToStation && assignedStation != null)
                {
                    SitDownAtStation();
                }
                else if (state == CustomerState.ServiceCompletedLeaving || state == CustomerState.AngryLeaving)
                {
                    Destroy(gameObject);
                }
            }
        }

        private void HandlePatience()
        {
            if (isAllServicesDone || state == CustomerState.ServiceCompletedLeaving || state == CustomerState.AngryLeaving) return;

            float decayRate = 1.0f;
            if (state == CustomerState.WaitingInQueue)
            {
                decayRate = 0.4f; // Sırada beklerken sabır daha yavaş tükenir
            }
            else if (state == CustomerState.SeatedWaitingForService)
            {
                float penalty = DirtCleanerSystem.Instance != null ? DirtCleanerSystem.Instance.GetPatiencePenaltyFactor() : 1.0f;
                decayRate = penalty;
            }

            currentPatience -= Time.deltaTime * decayRate;

            if (currentPatience <= 0f)
            {
                LeaveAngry();
            }
        }

        /// <summary>
        /// Kuaför oyuncu yanına gelip [E] bastığında çağrılır. Müşteriyi uygun koltuğa yönlendirir.
        /// </summary>
        public bool GuideToStation(SalonStation station)
        {
            if (station == null || station.HasCustomer()) return false;
            if (state != CustomerState.WaitingInQueue) return false;

            assignedStation = station;
            assignedStation.SeatCustomer(this);

            state = CustomerState.WalkingToStation;
            Transform seat = station.itemOrCustomerPoint != null ? station.itemOrCustomerPoint : station.transform;
            targetPosition = seat.position + Vector3.up * 0.4f;

            if (worldUI != null)
            {
                worldUI.ShowDialogue("Hemen geliyorum! 💺", 2.5f);
            }

            TwoCutAudioManager.Instance?.PlayPop();

            // Sırada arkasındaki müşterileri bir adım öne kaydır
            if (SalonGameManager.Instance != null)
            {
                SalonGameManager.Instance.RemoveCustomerFromQueue(this);
            }

            Debug.Log($"[TwoCut Customer] {customerName} kuaför tarafından {station.stationType} istasyonuna yönlendirildi!");
            return true;
        }

        private void SitDownAtStation()
        {
            state = CustomerState.SeatedWaitingForService;
            currentPatience = Mathf.Min(currentPatience + 10f, maxPatienceTime); // Koltuğa oturunca biraz sabır tazelenir

            if (worldUI != null)
            {
                string toolPrompt = GetRequiredToolName();
                worldUI.ShowDialogue($"Oturduk! Bekliyorum ({toolPrompt})", 3f);
            }

            Debug.Log($"[TwoCut Customer] {customerName} koltuğa oturdu. Hizmet bekleniyor.");
        }

        public void PerformServiceStep(SalonItem toolUsed, ServiceType stationService)
        {
            if (isAllServicesDone || (state != CustomerState.SeatedWaitingForService && state != CustomerState.UndergoingService)) return;

            ServiceType activeService = !isFirstServiceDone ? firstServiceNeeded : secondServiceNeeded;

            // 1. İstasyon türü eşleşiyor mu?
            if (stationService != activeService)
            {
                worldUI?.ShowDialogue($"Yanlış istasyon! {GetServiceName(activeService)} gerekli.", 2f);
                return;
            }

            // 2. Doğru alet elde mi?
            if (activeService == ServiceType.Haircut)
            {
                if (toolUsed == null || toolUsed.itemType != ItemType.Scissors)
                {
                    worldUI?.ShowDialogue("Lütfen MAKAS ile gelin! ✂️", 2f);
                    return;
                }
            }
            else if (activeService == ServiceType.HairWash)
            {
                if (toolUsed == null || toolUsed.itemType != ItemType.ShampooBottle)
                {
                    worldUI?.ShowDialogue("Lütfen ŞAMPUAN getirin! 🧼", 2f);
                    return;
                }
            }
            else if (activeService == ServiceType.HairDye)
            {
                if (toolUsed == null || (toolUsed.itemType != ItemType.DyeBottle_Red && toolUsed.itemType != ItemType.DyeBottle_Blonde))
                {
                    worldUI?.ShowDialogue("Lütfen BOYA getirin! 🎨", 2f);
                    return;
                }
            }

            state = CustomerState.UndergoingService;

            // Ses ve animasyon efekti
            if (activeService == ServiceType.Haircut)
            {
                TwoCutAudioManager.Instance?.PlayScissors();
            }
            else
            {
                TwoCutAudioManager.Instance?.PlayPop();
            }

            int increment = 1;
            if (activeService == ServiceType.Haircut && TwoCutShopUpgradeManager.Instance != null && TwoCutShopUpgradeManager.Instance.hasGoldenScissors)
            {
                increment = 2;
            }

            currentActions += increment;

            // Görsel efekt
            if (customerRenderer != null)
            {
                if (activeService == ServiceType.HairDye) customerRenderer.material.color = Color.magenta;
                else if (activeService == ServiceType.Haircut) transform.localScale = new Vector3(0.95f, 0.95f, 0.95f);
            }

            if (currentActions >= requiredActions)
            {
                currentActions = 0;
                if (!isFirstServiceDone)
                {
                    isFirstServiceDone = true;

                    // Yere saç döküntüsü oluştur
                    if (activeService == ServiceType.Haircut || activeService == ServiceType.HairDye)
                    {
                        DirtCleanerSystem.Instance?.SpawnHairClippingDirt(transform.position);
                    }

                    if (!needsSecondService)
                    {
                        CompleteAllServices();
                    }
                    else
                    {
                        worldUI?.ShowDialogue($"1. İşlem bitti! Şimdi {GetServiceName(secondServiceNeeded)} lütfen.", 3f);
                        state = CustomerState.SeatedWaitingForService;
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
            state = CustomerState.ServiceCompletedLeaving;

            if (assignedStation != null)
            {
                assignedStation.ClearCustomer();
                assignedStation = null;
            }

            int totalPay = paymentAmount;
            if (needsSecondService) totalPay += 45;

            bool gotTip = currentPatience > maxPatienceTime * 0.45f;
            if (gotTip)
            {
                totalPay += tipBonus;
                worldUI?.ShowDialogue($"Harika oldu! Teşekkürler! (+${totalPay} 💵)", 3f);
                TwoCutAudioManager.Instance?.PlayCheer();
            }
            else
            {
                worldUI?.ShowDialogue($"Fena değil. (+${totalPay} 💵)", 3f);
            }

            TwoCutAudioManager.Instance?.PlayCash();
            TwoCutEconomyManager.Instance?.AddEarnings(totalPay);

            // Çıkış kapısına doğru yürü
            if (SalonGameManager.Instance != null)
            {
                targetPosition = SalonGameManager.Instance.doorSpawnPoint;
            }
            else
            {
                targetPosition = transform.position + Vector3.back * 10f;
            }
        }

        private void LeaveAngry()
        {
            state = CustomerState.AngryLeaving;
            worldUI?.ShowDialogue("Çok bekledim, gidiyorum! 😡", 3f);

            if (assignedStation != null)
            {
                assignedStation.ClearCustomer();
                assignedStation = null;
            }

            if (SalonGameManager.Instance != null && SalonGameManager.Instance.waitingQueue.Contains(this))
            {
                SalonGameManager.Instance.RemoveCustomerFromQueue(this);
            }

            if (SalonGameManager.Instance != null)
            {
                targetPosition = SalonGameManager.Instance.doorSpawnPoint;
            }
            else
            {
                targetPosition = transform.position + Vector3.back * 10f;
            }
        }

        public string GetServiceEmoji()
        {
            ServiceType active = !isFirstServiceDone ? firstServiceNeeded : secondServiceNeeded;
            switch (active)
            {
                case ServiceType.Haircut: return "✂️";
                case ServiceType.HairWash: return "🧼";
                case ServiceType.HairDye: return "🎨";
                case ServiceType.Massage: return "💆";
                default: return "✂️";
            }
        }

        public string GetServiceName(ServiceType s)
        {
            switch (s)
            {
                case ServiceType.Haircut: return "Saç Kesimi";
                case ServiceType.HairWash: return "Saç Yıkama";
                case ServiceType.HairDye: return "Saç Boyama";
                case ServiceType.Massage: return "Masaj";
                default: return "Hizmet";
            }
        }

        public string GetRequiredToolName()
        {
            ServiceType active = !isFirstServiceDone ? firstServiceNeeded : secondServiceNeeded;
            switch (active)
            {
                case ServiceType.Haircut: return "Makas";
                case ServiceType.HairWash: return "Şampuan";
                case ServiceType.HairDye: return "Boya";
                case ServiceType.Massage: return "Masaj";
                default: return "Alet";
            }
        }

        public string GetStatusDescription()
        {
            switch (state)
            {
                case CustomerState.WaitingInQueue: return "Sırada Bekliyor";
                case CustomerState.WalkingToStation: return "Koltuğa Gidiyor...";
                case CustomerState.SeatedWaitingForService: return $"{GetRequiredToolName()} Bekliyor";
                case CustomerState.UndergoingService: return "İşlem Yapılıyor...";
                case CustomerState.ServiceCompletedLeaving: return "Tamamlandı!";
                case CustomerState.AngryLeaving: return "Ayrıldı";
                default: return "";
            }
        }
    }
}
