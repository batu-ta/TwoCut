using UnityEngine;
using TwoCutGame;

namespace HairSalonGame
{
    public enum StationType
    {
        HaircutChair,       // Saç Kesim Koltuğu & Masası
        HairWashSink,       // Kuaför Lavabo Masası
        HairDyeStation,     // Saç Boyama Masası
        MassageChair,       // Masaj Koltuğu
        TableSurface,       // Eşya Masası / Tezgah
        ToolRackContainer,  // Sınırsız Alet Rafı (Örn: Havlu/Şampuan Rafı)
        TrashBin            // Çöp Kutusu
    }

    /// <summary>
    /// Represents any functional station or table in TwoCut salon.
    /// Manages customer seating, placed tools on table surfaces, and service interactions.
    /// </summary>
    public class SalonStation : MonoBehaviour
    {
        [Header("Station Configuration")]
        public StationType stationType = StationType.HaircutChair;
        public string stationName = "Kuaför İstasyonu";

        [Header("Placement Points")]
        [Tooltip("Point where customer sits or item rests.")]
        public Transform itemOrCustomerPoint;
        [Tooltip("Optional separate point for tools resting on table surface next to the chair.")]
        public Transform tableItemPoint;

        [Header("Prefab Generator (For Infinite Tool Racks)")]
        public GameObject toolPrefab;

        [Header("State Status")]
        public SalonItem currentItem;
        public TwoCutCustomer currentCustomer;

        [Header("Visual Highlighting")]
        public Renderer stationRenderer;
        private Color originalColor = Color.white;

        private void Awake()
        {
            if (stationRenderer == null)
            {
                stationRenderer = GetComponentInChildren<Renderer>();
            }

            if (stationRenderer != null && stationRenderer.material != null)
            {
                originalColor = stationRenderer.material.color;
            }

            if (itemOrCustomerPoint == null)
            {
                itemOrCustomerPoint = transform;
            }
        }

        public bool HasItem() => currentItem != null || stationType == StationType.ToolRackContainer;
        public bool HasCustomer() => currentCustomer != null;
        public bool IsAvailable() => currentCustomer == null;

        public void SeatCustomer(TwoCutCustomer customer)
        {
            currentCustomer = customer;
        }

        public void ClearCustomer()
        {
            currentCustomer = null;
        }

        public SalonItem TakeItem()
        {
            if (stationType == StationType.ToolRackContainer && toolPrefab != null)
            {
                GameObject newObj = Instantiate(toolPrefab);
                return newObj.GetComponent<SalonItem>();
            }

            if (currentItem != null)
            {
                SalonItem itemToReturn = currentItem;
                currentItem = null;
                return itemToReturn;
            }

            return null;
        }

        public bool PlaceItem(SalonItem item)
        {
            if (item == null) return false;

            if (stationType == StationType.TrashBin)
            {
                Destroy(item.gameObject);
                TwoCutAudioManager.Instance?.PlayPop();
                return true;
            }

            if (currentItem != null) return false;

            currentItem = item;
            Transform targetPoint = tableItemPoint != null ? tableItemPoint : itemOrCustomerPoint;

            item.transform.SetParent(targetPoint);
            item.transform.localPosition = Vector3.up * 0.15f;
            item.transform.localRotation = Quaternion.identity;

            // Make kinematic while slotted in station
            Rigidbody rb = item.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
            }

            Collider[] cols = item.GetComponentsInChildren<Collider>();
            foreach (var col in cols)
            {
                col.enabled = true;
            }

            TwoCutAudioManager.Instance?.PlayPop();
            return true;
        }

        public void Interact(PlayerInteraction player)
        {
            if (currentCustomer != null && !currentCustomer.isAllServicesDone)
            {
                SalonItem heldItem = player.GetHeldItem();
                
                // Map station type to TwoCut ServiceType
                ServiceType currentService = ServiceType.Haircut;
                if (stationType == StationType.HairWashSink) currentService = ServiceType.HairWash;
                else if (stationType == StationType.HairDyeStation) currentService = ServiceType.HairDye;
                else if (stationType == StationType.MassageChair) currentService = ServiceType.Massage;

                currentCustomer.PerformServiceStep(heldItem, currentService);

                if (currentCustomer.isAllServicesDone)
                {
                    ClearCustomer();
                }
            }
        }

        public void SetHighlight(bool highlight)
        {
            if (stationRenderer != null && stationRenderer.material != null)
            {
                stationRenderer.material.color = highlight ? (originalColor * 1.3f + Color.cyan * 0.4f) : originalColor;
            }
        }
    }
}
