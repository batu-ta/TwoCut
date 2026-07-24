using UnityEngine;

public class Customer : MonoBehaviour
{
    public float maxPatience = 30f;
    private float currentPatience;
    public bool isWaiting = true;
    public string requestedService = "Haircut";

    public float moveSpeed = 3f; // Yürüme hýzýný buradan ayarlayabilirsin
    private Vector3 targetPosition; // Müþterinin gitmeye çalýþtýðý nokta

    void Start()
    {
        currentPatience = maxPatience;
        targetPosition = transform.position; // Doðduðu an olduðu yerde dursun
    }

    void Update()
    {
        // Hedefe doðru yavaþça yürüme kodu
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

        // Sýradayken sabýr azalmaya devam ediyor
        if (isWaiting)
        {
            currentPatience -= Time.deltaTime;

            if (currentPatience <= 0)
            {
                LeaveAngry();
            }
        }
    }

    // Manager'ýn müþteriye "þuraya git" demesi için kullanacaðýmýz komut
    public void MoveTo(Vector3 newPosition)
    {
        targetPosition = newPosition;
    }

    void LeaveAngry()
    {
        isWaiting = false;
        Debug.Log("Müþterinin sabrý taþtý ve gitti!");
        Destroy(gameObject);
    }

    public void StartService()
    {
        isWaiting = false;
        Debug.Log("Müþteri koltuða oturdu: " + requestedService);
    }
}