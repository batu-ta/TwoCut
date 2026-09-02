using System.Collections.Generic;
using UnityEngine;
using TwoCutGame;

namespace HairSalonGame
{
    /// <summary>
    /// Salon Game Manager for TwoCut.
    /// Manages customer arrivals, queue organization at the entrance,
    /// and station availability tracking.
    /// </summary>
    public class SalonGameManager : MonoBehaviour
    {
        public static SalonGameManager Instance { get; private set; }

        [Header("Customer Spawner")]
        public GameObject customerPrefab;
        public SalonStation[] availableStations;
        public float spawnInterval = 8f;
        public int maxQueueCapacity = 5;
        private float spawnTimer;

        [Header("Customer Queue Line Setup")]
        [Tooltip("Dükkanın kapı giriş noktası (Müşterilerin doğduğu yer)")]
        public Vector3 doorSpawnPoint = new Vector3(-13.5f, 0.2f, -1.0f);

        [Tooltip("Sıranın en başı (Açık alanda 1. müşterinin duracağı yer)")]
        public Vector3 queueStartPos = new Vector3(-5.5f, 0.2f, -1.0f);

        [Tooltip("Sırada arkaya doğru tek sıra dizilme mesafesi")]
        public Vector3 queueOffset = new Vector3(-1.8f, 0f, 0f);

        [Header("Active Queue")]
        public List<TwoCutCustomer> waitingQueue = new List<TwoCutCustomer>();

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            spawnTimer = 0.5f; // İlk müşteriyi hemen kapıdan içeri sok
            FindAllStationsInScene();

            // Ensure UI is active
            if (FindFirstObjectByType<TwoCutGameUI>() == null)
            {
                gameObject.AddComponent<TwoCutGameUI>();
            }

            // Ensure AudioManager is active
            _ = TwoCutAudioManager.Instance;
        }

        public void FindAllStationsInScene()
        {
            if (availableStations == null || availableStations.Length == 0)
            {
                availableStations = FindObjectsByType<SalonStation>(FindObjectsSortMode.None);
            }
        }

        private void Update()
        {
            if (TwoCutEconomyManager.Instance != null && (TwoCutEconomyManager.Instance.isShiftEnded || TwoCutEconomyManager.Instance.isBankrupt))
            {
                return;
            }

            HandleCustomerSpawning();
        }

        private void HandleCustomerSpawning()
        {
            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0f)
            {
                spawnTimer = spawnInterval;
                if (waitingQueue.Count < maxQueueCapacity)
                {
                    TrySpawnCustomer();
                }
            }
        }

        public void TrySpawnCustomer()
        {
            if (customerPrefab == null)
            {
                customerPrefab = Resources.Load<GameObject>("Prefabs/Müşteri");
                if (customerPrefab == null)
                {
                    Debug.LogWarning("[SalonGameManager] customerPrefab atanmamış!");
                    return;
                }
            }

            // Müşteri tam kapıda (doorSpawnPoint) doğar
            GameObject newCustomerObj = Instantiate(customerPrefab, doorSpawnPoint, Quaternion.identity);

            TwoCutCustomer newCustomer = newCustomerObj.GetComponent<TwoCutCustomer>();
            if (newCustomer == null)
            {
                newCustomer = newCustomerObj.AddComponent<TwoCutCustomer>();
            }

            if (newCustomerObj.GetComponent<CustomerWorldUI>() == null)
            {
                newCustomerObj.AddComponent<CustomerWorldUI>();
            }

            // Rastgele hizmet ata
            System.Array services = System.Enum.GetValues(typeof(ServiceType));
            newCustomer.firstServiceNeeded = (ServiceType)services.GetValue(Random.Range(0, services.Length));

            // %35 ihtimalle 2. bir ek hizmet ata (Örn: Masaj)
            if (Random.value > 0.65f && newCustomer.firstServiceNeeded != ServiceType.Massage)
            {
                newCustomer.needsSecondService = true;
                newCustomer.secondServiceNeeded = ServiceType.Massage;
            }

            newCustomer.state = CustomerState.WaitingInQueue;
            // Sırada kendi yerine doğru tek sıra halinde yürür
            newCustomer.targetPosition = queueStartPos + queueOffset * waitingQueue.Count;

            waitingQueue.Add(newCustomer);
            Debug.Log($"[SalonGameManager] Yeni müşteri kapıdan girdi ve sıraya yürüyor: {newCustomer.customerName} | Sırada: {waitingQueue.Count}. kişi");
        }

        /// <summary>
        /// Müşterinin istediği hizmete uygun boş bir istasyon arar.
        /// </summary>
        public SalonStation FindAvailableStation(ServiceType service)
        {
            FindAllStationsInScene();
            if (availableStations == null) return null;

            StationType requiredStationType = StationType.HaircutChair;
            if (service == ServiceType.HairWash) requiredStationType = StationType.HairWashSink;
            else if (service == ServiceType.HairDye) requiredStationType = StationType.HairDyeStation;
            else if (service == ServiceType.Massage) requiredStationType = StationType.MassageChair;

            foreach (var station in availableStations)
            {
                if (station != null && station.stationType == requiredStationType && station.IsAvailable())
                {
                    return station;
                }
            }

            // Eğer özel boya istasyonu yoksa saç kesim koltuğu da kabul edilebilir
            if (service == ServiceType.HairDye)
            {
                foreach (var station in availableStations)
                {
                    if (station != null && station.stationType == StationType.HaircutChair && station.IsAvailable())
                    {
                        return station;
                    }
                }
            }

            return null;
        }

        public void RemoveCustomerFromQueue(TwoCutCustomer customer)
        {
            if (waitingQueue.Contains(customer))
            {
                waitingQueue.Remove(customer);
                UpdateQueuePositions();
            }
        }

        public void UpdateQueuePositions()
        {
            // Sırada kalan tüm müşterileri bir adım öne doğru tek sıra halinde kaydır
            for (int i = 0; i < waitingQueue.Count; i++)
            {
                if (waitingQueue[i] != null && waitingQueue[i].state == CustomerState.WaitingInQueue)
                {
                    waitingQueue[i].targetPosition = queueStartPos + queueOffset * i;
                }
            }
        }
    }
}
