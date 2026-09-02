using UnityEngine;
using TwoCutGame;

namespace HairSalonGame
{
    /// <summary>
    /// Player Interaction System for TwoCut.
    /// Handles customer greeting & chair guiding ([E]),
    /// physics-based tool picking/dropping on tables & floor ([E]/[G]),
    /// and station service execution ([F]).
    /// Supports invisible trigger zone detection.
    /// </summary>
    public class PlayerInteraction : MonoBehaviour
    {
        [Header("Interaction Settings")]
        public float interactDistance = 2.4f;
        public LayerMask interactLayer = ~0;
        public Transform holdPoint;
        public bool isLocalPlayer = true;

        [Header("Control Key Bindings")]
        public KeyCode grabDropKey = KeyCode.E;
        public KeyCode dropOnGroundKey = KeyCode.G;
        public KeyCode actionInteractKey = KeyCode.F;

        [Header("Current Targets")]
        public SalonStation selectedStation;
        public SalonItem selectedGroundItem;
        public TwoCutCustomer selectedCustomer;
        public GameObject selectedDirtObject;

        [Header("Zone Trigger Reference")]
        public SalonStation zoneTriggerStation;

        [Header("Held Tool")]
        public SalonItem currentHeldItem;

        [Header("Contextual Hint Text (Read by UI)")]
        public string contextualHint = "";

        private void Start()
        {
            if (holdPoint == null)
            {
                Transform found = transform.Find("HoldPoint");
                if (found != null) holdPoint = found;
                else
                {
                    GameObject hp = new GameObject("HoldPoint");
                    hp.transform.SetParent(transform);
                    hp.transform.localPosition = new Vector3(0f, 0.6f, 0.9f);
                    holdPoint = hp.transform;
                }
            }
        }

        private void Update()
        {
            if (!isLocalPlayer) return;

            DetectInteractables();
            UpdateContextualHint();
            HandleInput();
        }

        private void DetectInteractables()
        {
            Vector3 checkPosition = transform.position + Vector3.up * 0.5f;
            // QueryTriggerInteraction.Collide enables detecting invisible zone trigger colliders as well
            Collider[] hits = Physics.OverlapSphere(checkPosition, interactDistance, interactLayer, QueryTriggerInteraction.Collide);

            SalonStation closestStation = zoneTriggerStation; // Start with zone trigger station if standing inside one
            SalonItem closestItem = null;
            TwoCutCustomer closestCustomer = null;
            GameObject closestDirt = null;

            float minStationDist = closestStation != null ? Vector3.Distance(transform.position, closestStation.transform.position) : float.MaxValue;
            float minItemDist = float.MaxValue;
            float minCustomerDist = float.MaxValue;
            float minDirtDist = float.MaxValue;

            foreach (var hit in hits)
            {
                // 1. Station detection (either station component on object, in parent, or referenced)
                SalonStation station = hit.GetComponentInParent<SalonStation>();
                if (station != null)
                {
                    float dist = Vector3.Distance(transform.position, station.transform.position);
                    if (dist < minStationDist)
                    {
                        minStationDist = dist;
                        closestStation = station;
                    }
                }

                // 2. Customer detection (prioritize waiting customers in queue)
                TwoCutCustomer customer = hit.GetComponentInParent<TwoCutCustomer>();
                if (customer != null && customer.state == CustomerState.WaitingInQueue)
                {
                    float dist = Vector3.Distance(transform.position, customer.transform.position);
                    if (dist < minCustomerDist)
                    {
                        minCustomerDist = dist;
                        closestCustomer = customer;
                    }
                }

                // 3. Salon Item detection (on ground or table)
                SalonItem item = hit.GetComponentInParent<SalonItem>();
                if (item != null && item != currentHeldItem && item.transform.parent != holdPoint)
                {
                    float dist = Vector3.Distance(transform.position, item.transform.position);
                    if (dist < minItemDist)
                    {
                        minItemDist = dist;
                        closestItem = item;
                    }
                }

                // 4. Dirt detection
                if (hit.name.Contains("HairClipping") || hit.name.Contains("Dirt"))
                {
                    float dist = Vector3.Distance(transform.position, hit.transform.position);
                    if (dist < minDirtDist)
                    {
                        minDirtDist = dist;
                        closestDirt = hit.gameObject;
                    }
                }
            }

            // Update highlight
            if (selectedStation != closestStation)
            {
                if (selectedStation != null) selectedStation.SetHighlight(false);
                selectedStation = closestStation;
                if (selectedStation != null) selectedStation.SetHighlight(true);
            }

            selectedGroundItem = closestItem;
            selectedCustomer = closestCustomer;
            selectedDirtObject = closestDirt;
        }

        private void OnTriggerEnter(Collider other)
        {
            SalonStation st = other.GetComponentInParent<SalonStation>();
            if (st != null)
            {
                zoneTriggerStation = st;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            SalonStation st = other.GetComponentInParent<SalonStation>();
            if (st != null && zoneTriggerStation == st)
            {
                zoneTriggerStation = null;
            }
        }

        private void UpdateContextualHint()
        {
            if (selectedCustomer != null && selectedCustomer.state == CustomerState.WaitingInQueue)
            {
                contextualHint = $"[E] {selectedCustomer.customerName}'i Koltuğa Yönlendir ({selectedCustomer.GetServiceEmoji()})";
                return;
            }

            if (currentHeldItem == null)
            {
                if (selectedStation != null && selectedStation.HasItem())
                {
                    contextualHint = $"[E] Masadaki {selectedStation.currentItem.itemName}'ı Al";
                }
                else if (selectedGroundItem != null)
                {
                    contextualHint = $"[E] {selectedGroundItem.itemName}'ı Al";
                }
                else if (selectedStation != null && selectedStation.HasCustomer())
                {
                    contextualHint = $"[F] Hizmet Ver (Alet Gerekli: {selectedStation.currentCustomer.GetRequiredToolName()})";
                }
                else if (selectedDirtObject != null)
                {
                    contextualHint = "[F] Yeri Temizle / Süpür";
                }
                else
                {
                    contextualHint = "";
                }
            }
            else
            {
                // Holding an item
                if (selectedStation != null && selectedStation.HasCustomer())
                {
                    contextualHint = $"[F] {currentHeldItem.itemName} ile Hizmet Ver";
                }
                else if (selectedStation != null && !selectedStation.HasItem())
                {
                    contextualHint = $"[E] {currentHeldItem.itemName}'ı Masaya / İstasyona Koy | [G] Masaya Bırak";
                }
                else if (selectedDirtObject != null && currentHeldItem.itemType == ItemType.Broom)
                {
                    contextualHint = "[F] Süpürge ile Yeri Temizle";
                }
                else
                {
                    contextualHint = $"[G] {currentHeldItem.itemName}'ı Masaya / Yere Bırak";
                }
            }
        }

        private void HandleInput()
        {
            // --- 1. [E] TUŞU: MÜŞTERİ YÖNLENDİRME / EŞYA ALMA-KOYMA ---
            if (Input.GetKeyDown(grabDropKey))
            {
                // A) Müşteriyi Koltuğa Yönlendirme
                if (selectedCustomer != null && selectedCustomer.state == CustomerState.WaitingInQueue)
                {
                    if (SalonGameManager.Instance != null)
                    {
                        SalonStation availableStation = SalonGameManager.Instance.FindAvailableStation(selectedCustomer.firstServiceNeeded);
                        if (availableStation != null)
                        {
                            selectedCustomer.GuideToStation(availableStation);
                            selectedCustomer = null;
                            return;
                        }
                        else
                        {
                            selectedCustomer.GetComponent<CustomerWorldUI>()?.ShowDialogue("Uygun koltuk dolu! Lütfen bekleyin ⏳", 2.5f);
                            return;
                        }
                    }
                }

                // B) Eşya Alma / Koyma
                if (currentHeldItem == null)
                {
                    if (selectedStation != null && selectedStation.HasItem())
                    {
                        SalonItem item = selectedStation.TakeItem();
                        PickUpItem(item);
                    }
                    else if (selectedGroundItem != null)
                    {
                        PickUpItem(selectedGroundItem);
                    }
                }
                else
                {
                    // Eldeki eşyayı istasyona yerleştir
                    if (selectedStation != null && !selectedStation.HasItem() && !selectedStation.HasCustomer())
                    {
                        if (selectedStation.PlaceItem(currentHeldItem))
                        {
                            currentHeldItem = null;
                        }
                    }
                    else
                    {
                        // İstasyonsuz alanda E'ye basılırsa masaya/yere fiziksel bırak
                        DropItemWithPhysics();
                    }
                }
            }

            // --- 2. [G] TUŞU: FİZİKSEL BIRAKMA (MASAYA VEYA YERE) ---
            if (Input.GetKeyDown(dropOnGroundKey))
            {
                if (currentHeldItem != null)
                {
                    DropItemWithPhysics();
                }
            }

            // --- 3. [F] TUŞU: HİZMET YAPMA (SAÇ KESME / YIKAMA / SÜPÜRME) ---
            if (Input.GetKeyDown(actionInteractKey))
            {
                if (selectedDirtObject != null && (currentHeldItem == null || currentHeldItem.itemType == ItemType.Broom))
                {
                    DirtCleanerSystem.Instance?.SweepCleanDirt(selectedDirtObject);
                    TwoCutAudioManager.Instance?.PlaySweep();
                    selectedDirtObject = null;
                }
                else if (selectedStation != null)
                {
                    selectedStation.Interact(this);
                }
            }
        }

        private void LateUpdate()
        {
            // Eldeki eşyanın oyuncunun elinden kaymasını veya uzaklaşmasını %100 engelleyen sabitleme
            if (currentHeldItem != null && holdPoint != null)
            {
                currentHeldItem.transform.position = holdPoint.position;
                currentHeldItem.transform.rotation = holdPoint.rotation;
            }
        }

        public void PickUpItem(SalonItem item)
        {
            if (item == null) return;

            // Oyuncu ile çarpışmaları engelle
            IgnoreCollisionsWithItem(item, true);

            currentHeldItem = item;

            // Fiziği ve Rigidbody bileşenini tamamen kaldır (Unity'de ebeveyn-çocuk Rigidbody kayma bugını önler)
            Rigidbody itemRb = item.GetComponent<Rigidbody>();
            if (itemRb != null)
            {
                Destroy(itemRb);
            }

            // Eldeyken tüm collider'ları kapat ki hiçbir nesneye takılıp sürüklenmesin
            Collider[] colliders = item.GetComponentsInChildren<Collider>();
            foreach (var col in colliders)
            {
                col.enabled = false;
            }

            // HoldPoint'e bağla ve sıfırla
            Transform targetHold = holdPoint != null ? holdPoint : transform;
            item.transform.SetParent(targetHold);
            item.transform.localPosition = Vector3.zero;
            item.transform.localRotation = Quaternion.identity;

            TwoCutAudioManager.Instance?.PlayPop();
            Debug.Log($"[TwoCut Player] {item.itemName} ele alındı ve HoldPoint'e sabitlendi.");
        }

        public void DropItemWithPhysics()
        {
            if (currentHeldItem == null) return;

            SalonItem itemToDrop = currentHeldItem;
            currentHeldItem = null;

            // Masanın veya zeminin üzerine doğru ileri hafif yay şeklinde bırak
            Vector3 dropPosition = transform.position + transform.forward * 0.9f + Vector3.up * 0.35f;

            itemToDrop.transform.SetParent(null);
            itemToDrop.transform.position = dropPosition;
            itemToDrop.transform.rotation = Quaternion.identity;

            // Collider'ları aktif et
            Collider[] colliders = itemToDrop.GetComponentsInChildren<Collider>();
            foreach (var col in colliders)
            {
                col.enabled = true;
            }

            // Rigidbody ile fizik motorunu ve yerçekimini yeniden ekle
            Rigidbody itemRb = itemToDrop.GetComponent<Rigidbody>();
            if (itemRb == null)
            {
                itemRb = itemToDrop.gameObject.AddComponent<Rigidbody>();
            }
            
            itemRb.mass = 0.4f;
            itemRb.interpolation = RigidbodyInterpolation.Interpolate;
            itemRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            itemRb.isKinematic = false;
            itemRb.useGravity = true;
            itemRb.detectCollisions = true;

            // Masaya doğal düşmesi için yumuşak ileri ivme
            itemRb.linearVelocity = transform.forward * 1.5f + Vector3.down * 0.5f;
            itemRb.angularVelocity = Random.insideUnitSphere * 1.5f;

            // Oyuncuyla anlık iç içe geçmeyi engelle
            IgnoreCollisionsWithItem(itemToDrop, true);

            TwoCutAudioManager.Instance?.PlayPop();
            Debug.Log($"[TwoCut Player] {itemToDrop.itemName} masaya/yere fiziksel olarak bırakıldı.");
        }

        private void IgnoreCollisionsWithItem(SalonItem item, bool ignore)
        {
            Collider playerCol = GetComponent<Collider>();
            if (playerCol == null || item == null) return;

            Collider[] itemCols = item.GetComponentsInChildren<Collider>();
            foreach (var col in itemCols)
            {
                if (col != null)
                {
                    Physics.IgnoreCollision(playerCol, col, ignore);
                }
            }
        }

        public SalonItem GetHeldItem() => currentHeldItem;
        public bool HasHeldItem() => currentHeldItem != null;

        private void OnDrawGizmosSelected()
        {
            if (!isLocalPlayer) return;
            Gizmos.color = Color.cyan;
            Vector3 center = transform.position + Vector3.up * 0.5f;
            Gizmos.DrawWireSphere(center, interactDistance);
        }
    }
}
