using UnityEngine;
using TwoCutGame;

namespace HairSalonGame
{
    /// <summary>
    /// Hairdresser Player Interaction System for TwoCut with Online Multiplayer support.
    /// </summary>
    public class PlayerInteraction : MonoBehaviour
    {
        [Header("Multiplayer Setup")]
        public bool isLocalPlayer = true;

        [Header("Interaction Configuration")]
        public float interactDistance = 1.5f;
        public float interactRadius = 0.6f;
        public LayerMask interactLayer = ~0;

        [Header("Hold Socket")]
        public Transform holdPoint;

        [Header("Keybindings")]
        public KeyCode grabDropKey = KeyCode.E;
        public KeyCode actionInteractKey = KeyCode.F;
        public KeyCode dropOnGroundKey = KeyCode.G;

        private SalonItem currentHeldItem;
        private SalonStation selectedStation;
        private SalonItem selectedGroundItem;
        private GameObject selectedDirtObject;

        private void Update()
        {
            if (!isLocalPlayer) return;

            DetectInteractable();
            HandleInput();
        }

        private bool IsItemOnGround(SalonItem item)
        {
            if (item == null) return false;
            // Bir oyuncunun elinde veya bir istasyonda durmuyorsa yerdedir
            if (item.GetComponentInParent<PlayerInteraction>() != null) return false;
            if (item.GetComponentInParent<SalonStation>() != null) return false;
            return true;
        }

        private void DetectInteractable()
        {
            // Karakterin çembersel yakınına (tüm yönlerde) gelen objeleri algılar
            Vector3 checkPosition = transform.position;
            checkPosition.y = 0.5f;

            Collider[] hits = Physics.OverlapSphere(checkPosition, interactDistance, interactLayer);

            SalonStation closestStation = null;
            SalonItem closestGroundItem = null;
            GameObject closestDirt = null;
            float minDistance = float.MaxValue;

            foreach (var hit in hits)
            {
                SalonStation station = hit.GetComponentInParent<SalonStation>();
                if (station != null)
                {
                    float dist = Vector3.Distance(transform.position, station.transform.position);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        closestStation = station;
                    }
                }

                SalonItem item = hit.GetComponentInParent<SalonItem>();
                if (item != null && item != currentHeldItem && IsItemOnGround(item))
                {
                    float dist = Vector3.Distance(transform.position, item.transform.position);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        closestGroundItem = item;
                    }
                }

                if (hit.CompareTag("Dirt") || hit.name.Contains("HairClipping") || hit.name.Contains("Dirt"))
                {
                    float dist = Vector3.Distance(transform.position, hit.transform.position);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        closestDirt = hit.gameObject;
                    }
                }
            }

            if (selectedStation != closestStation)
            {
                if (selectedStation != null) selectedStation.SetHighlight(false);
                selectedStation = closestStation;
                if (selectedStation != null) selectedStation.SetHighlight(true);
            }

            selectedGroundItem = closestGroundItem;
            selectedDirtObject = closestDirt;
        }

        private void HandleInput()
        {
            if (Input.GetKeyDown(grabDropKey) || Input.GetKeyDown(dropOnGroundKey))
            {
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
                    if (selectedStation != null && !selectedStation.HasItem())
                    {
                        if (selectedStation.PlaceItem(currentHeldItem))
                        {
                            currentHeldItem = null;
                        }
                    }
                    else if (selectedStation == null)
                    {
                        DropItemOnGround();
                    }
                }
            }

            if (Input.GetKeyDown(actionInteractKey))
            {
                if (selectedDirtObject != null && (currentHeldItem == null || currentHeldItem.itemType == ItemType.Broom))
                {
                    DirtCleanerSystem.Instance?.SweepCleanDirt(selectedDirtObject);
                    selectedDirtObject = null;
                }
                else if (selectedStation != null)
                {
                    selectedStation.Interact(this);
                }
            }
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

        public void PickUpItem(SalonItem item)
        {
            if (item == null) return;

            // Oyuncu ile makas arasındaki çarpışmayı engelle (iç içe geçip havaya fırlatmayı önler)
            IgnoreCollisionsWithItem(item, true);

            currentHeldItem = item;
            item.transform.SetParent(holdPoint != null ? holdPoint : transform);
            item.transform.localPosition = Vector3.zero;
            item.transform.localRotation = Quaternion.identity;

            // FİZİK KAYMASINI (Drift) ÖNLEME:
            Rigidbody itemRb = item.GetComponent<Rigidbody>();
            if (itemRb != null)
            {
                Destroy(itemRb);
            }

            // Mevlana dönme hatasını önlemek için çocuk objeler dahil tüm colliderları kapatıyoruz
            Collider[] colliders = item.GetComponentsInChildren<Collider>();
            foreach (var col in colliders)
            {
                col.enabled = false;
            }

            // Karakterin fiziksel dönme ivmelerini sıfırlıyoruz
            Rigidbody playerRb = GetComponent<Rigidbody>();
            if (playerRb != null)
            {
                playerRb.angularVelocity = Vector3.zero;
            }
        }

        public void DropItemOnGround()
        {
            if (currentHeldItem == null) return;

            // Bırakmadan önce de çarpışmayı yoksaymayı sürdür
            IgnoreCollisionsWithItem(currentHeldItem, true);

            Vector3 dropPos = transform.position + transform.forward * 1.2f;
            dropPos.y = 0.25f; // Masadan veya engelden hafif yukarıda bırakarak sıkışmayı önleriz

            currentHeldItem.transform.SetParent(null);
            currentHeldItem.transform.position = dropPos;
            currentHeldItem.transform.rotation = Quaternion.identity;

            // Yere bırakıldığında tüm colliderları tekrar aktif ediyoruz
            Collider[] colliders = currentHeldItem.GetComponentsInChildren<Collider>();
            foreach (var col in colliders)
            {
                col.enabled = true;
            }

            // Yere bırakıldığında yer çekimiyle düşmesi için Rigidbody bileşenini yeniden oluşturuyoruz
            Rigidbody itemRb = currentHeldItem.GetComponent<Rigidbody>();
            if (itemRb == null)
            {
                itemRb = currentHeldItem.gameObject.AddComponent<Rigidbody>();
                itemRb.interpolation = RigidbodyInterpolation.Interpolate;
            }
            itemRb.isKinematic = false;
            itemRb.useGravity = true; // Yerçekimini aktif et
            itemRb.detectCollisions = true;
            itemRb.linearVelocity = Vector3.zero; // Kalıntı hızları sıfırla
            itemRb.angularVelocity = Vector3.zero; // Kalıntı dönmeleri sıfırla

            currentHeldItem = null;
        }

        public SalonItem GetHeldItem() => currentHeldItem;
        public bool HasHeldItem() => currentHeldItem != null;

        private void OnDrawGizmosSelected()
        {
            if (!isLocalPlayer) return;
            Gizmos.color = Color.cyan;
            Vector3 center = transform.position;
            center.y = 0.5f;
            Gizmos.DrawWireSphere(center, interactDistance);
        }
    }
}
