using UnityEngine;

namespace TwoCutGame
{
    /// <summary>
    /// TwoCut Cleaning Mechanics System.
    /// Spawns hair clippings & mess on floor after haircuts.
    /// Manages overall shop cleanliness % and broom sweeping.
    /// </summary>
    public class DirtCleanerSystem : MonoBehaviour
    {
        public static DirtCleanerSystem Instance { get; private set; }

        [Header("Cleanliness Settings")]
        [Tooltip("Overall shop cleanliness percentage (0% = Filthy, 100% = Sparkling clean).")]
        [Range(0f, 100f)]
        public float cleanlinessPercent = 100f;

        [Header("Hair Clipping Spawner")]
        public GameObject hairClippingPrefab;
        public Transform[] floorDirtSpawnPoints;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void SpawnHairClippingDirt(Vector3 position)
        {
            // Reduce cleanliness by 8% per haircut mess
            cleanlinessPercent = Mathf.Clamp(cleanlinessPercent - 8f, 0f, 100f);

            if (hairClippingPrefab != null)
            {
                Vector3 spawnPos = position + new Vector3(Random.Range(-0.5f, 0.5f), 0.05f, Random.Range(-0.5f, 0.5f));
                Instantiate(hairClippingPrefab, spawnPos, Quaternion.identity);
            }

            Debug.Log($"[Cleanliness] Saç döküntüsü birikti! Temizlik Oranı: %{cleanlinessPercent:F0}");
        }

        public void SweepCleanDirt(GameObject dirtObject)
        {
            if (dirtObject != null)
            {
                Destroy(dirtObject);
            }

            // Restore cleanliness by 10% per sweep
            cleanlinessPercent = Mathf.Clamp(cleanlinessPercent + 10f, 0f, 100f);
            Debug.Log($"[Cleanliness] Süpürüldü! Temizlik Oranı: %{cleanlinessPercent:F0}");
        }

        public float GetPatiencePenaltyFactor()
        {
            // If shop is dirty (< 50% clean), customers lose patience faster!
            if (cleanlinessPercent < 50f)
            {
                return 1.4f; // 40% faster patience drop
            }
            return 1.0f;
        }
    }
}
