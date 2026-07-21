using UnityEngine;
using HairSalonGame;

namespace TwoCutGame
{
    public enum ServiceType
    {
        Haircut,
        HairWash,
        HairDye,
        Massage
    }

    public class TwoCutCustomer : MonoBehaviour
    {
        [Header("Customer Identification")]
        public string customerName = "Müşteri";

        [Header("Requested Services")]
        public ServiceType firstServiceNeeded = ServiceType.Haircut;
        public bool needsSecondService = false;
        public ServiceType secondServiceNeeded = ServiceType.Massage;

        public bool isFirstServiceDone = false;
        public bool isAllServicesDone = false;

        [Header("Patience Countdown")]
        public float maxPatienceTime = 40f;
        public float currentPatience;

        [Header("Action Progress")]
        public int requiredActions = 5;
        public int currentActions = 0;

        [Header("Payment")]
        public int paymentAmount = 60;
        public int tipBonus = 25;

        private Renderer customerRenderer;

        private void Start()
        {
            currentPatience = maxPatienceTime;
            customerRenderer = GetComponent<Renderer>();
        }

        private void Update()
        {
            if (isAllServicesDone) return;

            float penalty = DirtCleanerSystem.Instance != null ? DirtCleanerSystem.Instance.GetPatiencePenaltyFactor() : 1.0f;
            currentPatience -= Time.deltaTime * penalty;

            if (currentPatience <= 0f)
            {
                LeaveAngry();
            }
        }

        public void PerformServiceStep(SalonItem toolUsed, ServiceType stationService)
        {
            if (isAllServicesDone) return;

            ServiceType targetService = !isFirstServiceDone ? firstServiceNeeded : secondServiceNeeded;

            if (stationService != targetService)
            {
                return;
            }

            currentActions++;

            if (customerRenderer != null)
            {
                if (targetService == ServiceType.HairDye) customerRenderer.material.color = Color.magenta;
                else if (targetService == ServiceType.Massage) customerRenderer.material.color = Color.cyan;
                else if (targetService == ServiceType.Haircut) transform.localScale = Vector3.one * 0.9f;
            }

            if (currentActions >= requiredActions)
            {
                currentActions = 0;

                if (!isFirstServiceDone)
                {
                    isFirstServiceDone = true;

                    if (targetService == ServiceType.Haircut || targetService == ServiceType.HairDye)
                    {
                        DirtCleanerSystem.Instance?.SpawnHairClippingDirt(transform.position);
                    }

                    if (!needsSecondService)
                    {
                        CompleteAllServices();
                    }
                }
                else
                {
                    CompleteAllServices();
                }
            }
        }

        private void CompleteAllServices()
        {
            isAllServicesDone = true;

            int totalPay = paymentAmount;
            if (needsSecondService) totalPay += 45;

            if (currentPatience > maxPatienceTime * 0.5f)
            {
                totalPay += tipBonus;
            }

            TwoCutEconomyManager.Instance?.AddEarnings(totalPay);
            Destroy(gameObject, 1.2f);
        }

        private void LeaveAngry()
        {
            Destroy(gameObject);
        }
    }
}
