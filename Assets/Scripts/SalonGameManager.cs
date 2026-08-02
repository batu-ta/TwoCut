using UnityEngine;
using TwoCutGame;

namespace HairSalonGame
{
    /// <summary>
    /// Game Manager for Hair Salon game.
    /// Controls shift timer, salon money earned, customer spawn interval, and score UI.
    /// </summary>
    public class SalonGameManager : MonoBehaviour
    {
        public static SalonGameManager Instance { get; private set; }

        [Header("Salon Shift Settings")]
        public float shiftDuration = 180f;
        public float timeRemaining;

        [Header("Customer Spawner")]
        public GameObject customerPrefab;
        public SalonStation[] availableChairs;
        public float spawnInterval = 10f;
        private float spawnTimer;

        [Header("Customer Queue Setup")]
        public Vector3 entrancePos = new Vector3(0f, 0.5f, -8f); // Dükkan kapısı önü
        public Vector3 queueOffset = new Vector3(0f, 0f, -1.8f); // Sıradaki mesafe (güneye doğru uzanır)
        
        [System.NonSerialized]
        public System.Collections.Generic.List<TwoCutCustomer> waitingQueue = new System.Collections.Generic.List<TwoCutCustomer>();

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            timeRemaining = shiftDuration;
            spawnTimer = 2f; // Spawn first customer quickly
        }

        private void Update()
        {
            if (timeRemaining > 0)
            {
                timeRemaining -= Time.deltaTime;
                HandleCustomerSpawning();
                HandleQueueSeating();
            }
        }

        private void HandleCustomerSpawning()
        {
            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0f)
            {
                spawnTimer = spawnInterval;
                TrySpawnCustomer();
            }
        }

        private void TrySpawnCustomer()
        {
            if (customerPrefab == null) return;

            // Kapı önünde müşteriyi oluştur
            Vector3 spawnPosition = entrancePos + queueOffset * waitingQueue.Count;
            GameObject newCustomerObj = Instantiate(customerPrefab, spawnPosition, Quaternion.identity);
            
            TwoCutCustomer newCustomer = newCustomerObj.GetComponent<TwoCutCustomer>();
            if (newCustomer != null)
            {
                // Rastgele hizmet ata
                System.Array services = System.Enum.GetValues(typeof(ServiceType));
                newCustomer.firstServiceNeeded = (ServiceType)services.GetValue(Random.Range(0, services.Length));
                
                // Masaj hizmetini %40 ihtimalle ikinci hizmet olarak ata
                if (Random.value > 0.6f && newCustomer.firstServiceNeeded != ServiceType.Massage)
                {
                    newCustomer.needsSecondService = true;
                    newCustomer.secondServiceNeeded = ServiceType.Massage;
                }

                // Hedef sıradaki yerini ata
                newCustomer.targetPosition = spawnPosition;
                newCustomer.isSeated = false;

                // Sıraya ekle
                waitingQueue.Add(newCustomer);
                Debug.Log($"[SalonGameManager] Yeni müşteri geldi! Sıra Boyutu: {waitingQueue.Count}");
            }
        }

        private void HandleQueueSeating()
        {
            if (waitingQueue.Count == 0 || availableChairs == null || availableChairs.Length == 0) return;

            // Boş koltuk ara
            foreach (var chair in availableChairs)
            {
                if (chair != null && !chair.HasCustomer())
                {
                    // Sıranın en önündeki müşteriyi al
                    TwoCutCustomer customerToSeat = waitingQueue[0];
                    if (customerToSeat == null)
                    {
                        waitingQueue.RemoveAt(0);
                        continue;
                    }

                    waitingQueue.RemoveAt(0);

                    // Koltuğa oturt (Ebeveyn ataması yapılır)
                    chair.SeatCustomer(customerToSeat);

                    // Koltuktaki yerel hedef pozisyonunu ata
                    customerToSeat.targetPosition = new Vector3(0f, 0.6f, 0f);
                    customerToSeat.isSeated = true;

                    // Geri kalan müşterileri sırada bir adım öne kaydır
                    UpdateQueuePositions();
                    break;
                }
            }
        }

        public void UpdateQueuePositions()
        {
            for (int i = 0; i < waitingQueue.Count; i++)
            {
                if (waitingQueue[i] != null)
                {
                    waitingQueue[i].targetPosition = entrancePos + queueOffset * i;
                }
            }
        }
    }
}
