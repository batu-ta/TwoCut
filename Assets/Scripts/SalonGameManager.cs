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
        [Tooltip("Sahnede sürükleyip bırakabileceğiniz Doğma Noktası (Boşsa doorSpawnPoint kullanılır)")]
        public Transform doorSpawnTransform;
        [Tooltip("Dükkanın kapı giriş noktası (Müşterilerin doğduğu koordinat)")]
        public Vector3 doorSpawnPoint = new(-13.5f, 0.2f, -1.0f);

        [Tooltip("Sahnede elle taşıyabileceğiniz Sıra Noktaları (Örn: Sira1, Sira2, Sira3). Boş bırakılırsa queueStartPos kullanılır.")]
        public Transform[] queuePointTransforms;

        [Tooltip("Sıranın en başı (1. müşterinin duracağı koordinat)")]
        public Vector3 queueStartPos = new(-5.5f, 0.2f, -1.0f);

        [Tooltip("Sırada arkaya doğru tek sıra dizilme mesafesi")]
        public Vector3 queueOffset = new(-1.8f, 0f, 0f);

        [Header("Active Queue")]
        public List<TwoCutCustomer> waitingQueue = new();

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            AutoDetectLocationTransforms();
        }

        private void Start()
        {
            spawnTimer = 0.5f; // İlk müşteriyi hemen kapıdan içeri sok
            FindAllStationsInScene();
            AutoDetectLocationTransforms();

            // Ensure UI is active
            if (FindFirstObjectByType<TwoCutGameUI>() == null)
            {
                gameObject.AddComponent<TwoCutGameUI>();
            }

            // Ensure AudioManager is active
            _ = TwoCutAudioManager.Instance;
        }

        public void AutoDetectLocationTransforms()
        {
            // Sahnede 'Location' veya 'SpawnPoint' objeleri varsa otomatik olarak bağla
            if (doorSpawnTransform == null)
            {
                GameObject spawnObj = GameObject.Find("SpawnPoint");
                if (spawnObj != null) doorSpawnTransform = spawnObj.transform;
            }

            if (queuePointTransforms == null || queuePointTransforms.Length == 0)
            {
                List<Transform> points = new();
                for (int i = 1; i <= 10; i++)
                {
                    GameObject siraObj = GameObject.Find($"Sira{i}");
                    if (siraObj != null)
                    {
                        points.Add(siraObj.transform);
                    }
                }
                if (points.Count > 0)
                {
                    queuePointTransforms = points.ToArray();
                }
            }
        }

        public Vector3 GetSpawnPosition()
        {
            if (doorSpawnTransform != null) return doorSpawnTransform.position;
            return doorSpawnPoint;
        }

        public Vector3 GetQueuePosition(int queueIndex)
        {
            if (queuePointTransforms != null && queuePointTransforms.Length > 0)
            {
                if (queueIndex < queuePointTransforms.Length && queuePointTransforms[queueIndex] != null)
                {
                    return queuePointTransforms[queueIndex].position;
                }
                else if (queuePointTransforms.Length > 0 && queuePointTransforms[^1] != null)
                {
                    // Sıra tanımlı nokta sayısından uzunsa son noktadan itibaren geriye doğru diz
                    Transform last = queuePointTransforms[^1];
                    return last.position + queueOffset * (queueIndex - queuePointTransforms.Length + 1);
                }
            }
            return queueStartPos + queueOffset * queueIndex;
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

            // Müşteri belirlenen doğma noktasında (doorSpawnTransform veya doorSpawnPoint) doğar
            Vector3 spawnPos = GetSpawnPosition();
            GameObject newCustomerObj = Instantiate(customerPrefab, spawnPos, Quaternion.identity);

            TwoCutCustomer newCustomer = newCustomerObj.GetComponent<TwoCutCustomer>();
            newCustomer ??= newCustomerObj.AddComponent<TwoCutCustomer>();

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
            newCustomer.targetPosition = GetQueuePosition(waitingQueue.Count);

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
                    waitingQueue[i].targetPosition = GetQueuePosition(i);
                }
            }
        }

        private void OnDrawGizmos()
        {
            // Sahnede görsel rehber çizgileri ve küreler çiz (Scene görünümünde kolay hizalama için)
            Vector3 spawnPos = GetSpawnPosition();
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(spawnPos, 0.4f);
            Gizmos.DrawLine(spawnPos, spawnPos + Vector3.up * 1.5f);

            Vector3 firstQueuePos = GetQueuePosition(0);
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(spawnPos, firstQueuePos);

            for (int i = 0; i < 5; i++)
            {
                Vector3 qPos = GetQueuePosition(i);
                Gizmos.color = i == 0 ? Color.green : Color.yellow;
                Gizmos.DrawWireSphere(qPos, 0.35f);
                if (i < 4)
                {
                    Gizmos.DrawLine(qPos, GetQueuePosition(i + 1));
                }
            }
        }
    }
}
