using UnityEngine;
using TwoCutGame;

/// <summary>
/// Backward-compatibility bridge for Customer prefab.
/// Automatically forwards logic to TwoCutCustomer.
/// </summary>
public class Customer : MonoBehaviour
{
    private TwoCutCustomer twoCutCustomer;

    private void Awake()
    {
        twoCutCustomer = GetComponent<TwoCutCustomer>();
        if (twoCutCustomer == null)
        {
            twoCutCustomer = gameObject.AddComponent<TwoCutCustomer>();
        }

        if (GetComponent<CustomerWorldUI>() == null)
        {
            gameObject.AddComponent<CustomerWorldUI>();
        }
    }
}
