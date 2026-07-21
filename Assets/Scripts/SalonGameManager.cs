using UnityEngine;
using TwoCutGame;

namespace HairSalonGame
{
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

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            timeRemaining = shiftDuration;
            spawnTimer = 2f;
        }

        private void Update()
        {
            if (timeRemaining > 0)
            {
                timeRemaining -= Time.deltaTime;
                HandleCustomerSpawning();
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
            if (customerPrefab == null || availableChairs == null || availableChairs.Length == 0) return;

            foreach (var chair in availableChairs)
            {
                if (chair != null && !chair.HasCustomer())
                {
                    GameObject newCustomerObj = Instantiate(customerPrefab);
                    TwoCutCustomer newCustomer = newCustomerObj.GetComponent<TwoCutCustomer>();

                    if (newCustomer != null)
                    {
                        System.Array services = System.Enum.GetValues(typeof(ServiceType));
                        newCustomer.firstServiceNeeded = (ServiceType)services.GetValue(Random.Range(0, services.Length));

                        chair.SeatCustomer(newCustomer);
                        Debug.Log($"[SalonGameManager] Yeni müşteri {chair.roomName} koltuğuna oturdu!");
                        break;
                    }
                }
            }
        }
    }
}
