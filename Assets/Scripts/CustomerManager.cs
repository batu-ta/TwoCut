using System.Collections.Generic;
using UnityEngine;

public class CustomerManager : MonoBehaviour
{
    public GameObject customerPrefab;
    public Transform spawnPoint;
    public Transform[] queuePositions;

    private List<Customer> queue = new List<Customer>();
    public float spawnInterval = 5f;
    private float spawnTimer;

    void Start()
    {
        spawnTimer = spawnInterval;
    }

    void Update()
    {
        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0 && queue.Count < queuePositions.Length)
        {
            SpawnCustomer();
            spawnTimer = spawnInterval;
        }
    }

    void SpawnCustomer()
    {
        // Müþteri spawnPoint'te doðuyor
        GameObject newObj = Instantiate(customerPrefab, spawnPoint.position, Quaternion.identity);
        Customer newCustomer = newObj.GetComponent<Customer>();

        queue.Add(newCustomer);
        UpdateQueuePositions();
    }

    public void UpdateQueuePositions()
    {
        for (int i = 0; i < queue.Count; i++)
        {
            // Iþýnlamak yerine, ilgili sýradaki pozisyonu hedef olarak veriyoruz
            queue[i].MoveTo(queuePositions[i].position);
        }
    }

    public void ServeFirstCustomer()
    {
        if (queue.Count > 0)
        {
            Customer firstCustomer = queue[0];
            queue.RemoveAt(0);

            firstCustomer.StartService();
            UpdateQueuePositions(); // Sýradakiler bir öndeki noktaya doðru yürümeye baþlar
        }
    }
}