using UnityEngine;

public class Customer : MonoBehaviour
{
    public float maxPatience = 30f;
    private float currentPatience;
    public bool isWaiting = true;
    public string requestedService = "Haircut";

    public float moveSpeed = 3f;
    private Vector3 targetPosition;
    private Vector3 spawnPosition;

    void Start()
    {
        currentPatience = maxPatience;
        spawnPosition = transform.position;
        targetPosition = transform.position;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) playerTransform = playerObj.transform;

        GameObject targetObj = GameObject.Find("TeleportTarget");
        if (targetObj != null) teleportTarget = targetObj.transform;

        customerRenderer = GetComponentInChildren<MeshRenderer>();
        if (customerRenderer != null)
            originalColor = customerRenderer.material.color;

        // MANAGER'I BEKLEMEDEN KENDÝMÝZ ÇAÐIRIYORUZ:
        // Doðduðu an sahnendeki manager'ý bulup pozisyonunu hemen güncelletiyoruz.
        Invoke("ForceInitialQueuePosition", 0.05f);
    }

    void ForceInitialQueuePosition()
    {
        CustomerManager manager = FindFirstObjectByType<CustomerManager>();
        if (manager != null)
        {
            manager.UpdateQueuePositions();
        }
    }

    void Update()
    {
        if (isInChair && teleportTarget != null)
        {
            targetPosition = teleportTarget.position;
            transform.position = teleportTarget.position;
        }

        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

        if (isWaiting && !isInChair && !isLeavingAngry && !isLeavingHappy)
        {
            currentPatience -= Time.deltaTime;
            if (currentPatience <= 0)
            {
                LeaveAngry();
            }
        }

        if (playerTransform != null)
        {
            float distance = Vector3.Distance(transform.position, playerTransform.position);

            if (!isInChair && !isLeavingAngry && !isLeavingHappy)
            {
                if (distance <= interactionDistance)
                {
                    if (!isPlayerNearby)
                    {
                        isPlayerNearby = true;
                        ChangeColor(interactiveColor);
                    }

                    if (Input.GetKeyDown(KeyCode.F))
                    {
                        TeleportToChair();
                    }
                }
                else
                {
                    if (isPlayerNearby)
                    {
                        isPlayerNearby = false;
                        ChangeColor(originalColor);
                    }
                }
            }
            else if (isInChair)
            {
                if (distance <= interactionDistance * 1.5f)
                {
                    if (Input.GetKey(KeyCode.F))
                    {
                        currentHoldTimer += Time.deltaTime;
                        Debug.Log("Saç kesiliyor... Süre: " + currentHoldTimer.ToString("F1"));

                        if (currentHoldTimer >= requiredHoldTime)
                        {
                            CompleteHaircut();
                        }
                    }
                }
            }
        }

        if ((isLeavingAngry || isLeavingHappy) && Vector3.Distance(transform.position, spawnPosition) < 0.3f)
        {
            Destroy(gameObject);
        }
    }

    public void MoveTo(Vector3 newPosition)
    {
        if (!isInChair && !isLeavingAngry && !isLeavingHappy)
        {
            targetPosition = newPosition;
            isWaiting = true;
        }
    }

    void LeaveAngry()
    {
        isLeavingAngry = true;
        isWaiting = false;
        isPlayerNearby = false;
        ChangeColor(originalColor);
        targetPosition = spawnPosition;
    }

    public void StartService()
    {
        isWaiting = false;
    }

    [Header("Etkilesim Ayarlari")]
    public float interactionDistance = 2.5f;
    private Transform playerTransform;
    private Transform teleportTarget;
    private bool isInChair = false;
    private bool isPlayerNearby = false;
    private bool isLeavingAngry = false;
    private bool isLeavingHappy = false;

    [Header("Color Settings")]
    public MeshRenderer customerRenderer;
    private Color originalColor;
    public Color interactiveColor = new Color(0.12f, 0.65f, 0.58f);

    [Header("Haircut Ayarlari")]
    public float requiredHoldTime = 3f;
    private float currentHoldTimer = 0f;

    void ChangeColor(Color newColor)
    {
        if (customerRenderer != null)
        {
            customerRenderer.material.color = newColor;
        }
    }

    void TeleportToChair()
    {
        if (teleportTarget != null)
        {
            targetPosition = teleportTarget.position;
            transform.position = teleportTarget.position;
            transform.rotation = teleportTarget.rotation;

            isInChair = true;
            isWaiting = false;
            ChangeColor(originalColor);

            CustomerManager manager = FindFirstObjectByType<CustomerManager>();
            if (manager != null)
            {
                manager.ServeFirstCustomer();
            }
        }
    }

    void CompleteHaircut()
    {
        Debug.Log("Saç kesimi bitti, müþteri baþladýðý yere geri dönüyor!");
        isInChair = false;
        isLeavingHappy = true;
        isPlayerNearby = false;
        ChangeColor(originalColor);
        targetPosition = spawnPosition;
    }
}