using UnityEngine;
using TwoCutGame;

namespace HairSalonGame
{
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

        private void DetectInteractable()
        {
            Vector3 checkPosition = transform.position + transform.forward * interactDistance;
            Collider[] hits = Physics.OverlapSphere(checkPosition, interactRadius, interactLayer);

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
                if (item != null && item != currentHeldItem && item.transform.parent == null)
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

        public void PickUpItem(SalonItem item)
        {
            if (item == null) return;

            currentHeldItem = item;
            item.transform.SetParent(holdPoint != null ? holdPoint : transform);
            item.transform.localPosition = Vector3.zero;
            item.transform.localRotation = Quaternion.identity;

            Rigidbody itemRb = item.GetComponent<Rigidbody>();
            if (itemRb != null) itemRb.isKinematic = true;

            Collider col = item.GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }

        public void DropItemOnGround()
        {
            if (currentHeldItem == null) return;

            Vector3 dropPos = transform.position + transform.forward * 1.0f;
            dropPos.y = 0.2f;

            currentHeldItem.transform.SetParent(null);
            currentHeldItem.transform.position = dropPos;
            currentHeldItem.transform.rotation = Quaternion.identity;

            Collider col = currentHeldItem.GetComponent<Collider>();
            if (col != null) col.enabled = true;

            Rigidbody itemRb = currentHeldItem.GetComponent<Rigidbody>();
            if (itemRb != null) itemRb.isKinematic = false;

            currentHeldItem = null;
        }

        public SalonItem GetHeldItem() => currentHeldItem;
        public bool HasHeldItem() => currentHeldItem != null;

        private void OnDrawGizmosSelected()
        {
            if (!isLocalPlayer) return;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position + transform.forward * interactDistance, interactRadius);
        }
    }
}
