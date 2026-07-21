using UnityEngine;

namespace TwoCutGame
{
    public class DirtCleanerSystem : MonoBehaviour
    {
        public static DirtCleanerSystem Instance { get; private set; }

        [Range(0f, 100f)]
        public float cleanlinessPercent = 100f;
        public GameObject hairClippingPrefab;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void SpawnHairClippingDirt(Vector3 position)
        {
            cleanlinessPercent = Mathf.Clamp(cleanlinessPercent - 8f, 0f, 100f);

            if (hairClippingPrefab != null)
            {
                Vector3 spawnPos = position + new Vector3(Random.Range(-0.5f, 0.5f), 0.05f, Random.Range(-0.5f, 0.5f));
                Instantiate(hairClippingPrefab, spawnPos, Quaternion.identity);
            }
        }

        public void SweepCleanDirt(GameObject dirtObject)
        {
            if (dirtObject != null)
            {
                Destroy(dirtObject);
            }
            cleanlinessPercent = Mathf.Clamp(cleanlinessPercent + 10f, 0f, 100f);
        }

        public float GetPatiencePenaltyFactor()
        {
            if (cleanlinessPercent < 50f)
            {
                return 1.4f;
            }
            return 1.0f;
        }
    }
}
