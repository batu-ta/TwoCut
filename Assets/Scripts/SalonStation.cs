using UnityEngine;
using TwoCutGame;

namespace HairSalonGame
{
    public enum StationType
    {
        ClearTable,         // Malzeme masası
        HaircutChair,       // Kesim Koltuğu (Makas kullanılır)
        HairWashSink,       // Yıkama Lavobosu (Şampuan kullanılır)
        HairDyeStation,     // Boya Masası (Saç boyası kullanılır)
        HairStylingChair,   // Fön & Şekillendirme Koltuğu (Fön makinesi kullanılır)
        MassageChair,       // Masaj Koltuğu
        ToolRackContainer,  // Malzeme / Alet Kutusu (Sonsuz Alet Üretir)
        ReceptionDesk,      // Kasa / Karşılama
        TrashBin            // Çöp Kovası
    }

    /// <summary>
    /// Hair Salon Station / Chair script for TwoCut.
    /// Manages room location, customer seating, tool placement, and hairdresser action interactions.
    /// </summary>
    public class SalonStation : MonoBehaviour
    {
        [Header("Room & Station Info")]
        public string roomName = "Oda 1 - Saç Kesim Odası";
        public StationType stationType = StationType.ClearTable;

        [Tooltip("Anchor point where items or customers sit/stand.")]
        public Transform itemOrCustomerPoint;

        [Tooltip("If ToolRackContainer, prefab of tool spawned.")]
        public GameObject toolPrefab;

        private SalonItem currentItem;
        private TwoCutCustomer currentCustomer;

        private Renderer stationRenderer;
        private Color originalColor;

        private void Awake()
        {
            stationRenderer = GetComponent<Renderer>();
            if (stationRenderer != null)
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

        public void SeatCustomer(TwoCutCustomer customer)
        {
            currentCustomer = customer;
            customer.transform.SetParent(itemOrCustomerPoint);
            customer.transform.localPosition = Vector3.up * 0.6f;
            customer.transform.localRotation = Quaternion.identity;
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
            if (stationType == StationType.TrashBin)
            {
                Destroy(item.gameObject);
                return true;
            }

            if (currentItem != null) return false;

            currentItem = item;
            currentItem.transform.SetParent(itemOrCustomerPoint);
            currentItem.transform.localPosition = Vector3.up * 0.5f;
            currentItem.transform.localRotation = Quaternion.identity;

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
            if (stationRenderer != null)
            {
                stationRenderer.material.color = highlight ? Color.cyan : originalColor;
            }
        }
    }
}
