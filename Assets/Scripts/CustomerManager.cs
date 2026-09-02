using UnityEngine;
using HairSalonGame;

/// <summary>
/// Backward-compatibility bridge for legacy CustomerManager in scene.
/// Delegates spawning and queue management to SalonGameManager.
/// </summary>
public class CustomerManager : MonoBehaviour
{
    private void Start()
    {
        // If SalonGameManager exists, this legacy component is not needed
        if (SalonGameManager.Instance != null && SalonGameManager.Instance.gameObject != this.gameObject)
        {
            Destroy(this);
        }
    }
}
