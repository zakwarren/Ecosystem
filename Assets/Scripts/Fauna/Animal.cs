using UnityEngine;
using AI.GOAP;

namespace Ecosystem.Fauna
{
    [SelectionBase]
    [RequireComponent(typeof(Agent))]
    public class Animal : MonoBehaviour
    {
        [Range(0f, 100f)]
        [SerializeField] float hungerFactor = 1f;
        [Range(0f, 100f)]
        [SerializeField] float hungerPoint = 40f;

        Agent agent;

        const float maxEnergy = 100f;
        const float minEnergy = 0f;
        [Range(0f, 100f)]
        [SerializeField] float energy = 100f;

        private void Awake()
        {
            agent = GetComponent<Agent>();
        }

        private void LateUpdate()
        {
            GetHungry();
        }

        public void RestoreEnergy(float calories)
        {
            energy = Mathf.Clamp(energy + calories, minEnergy, maxEnergy);
            if (energy > hungerPoint)
            {
                agent.RemoveFromState(Effects.Hungry);
            }
        }

        private void GetHungry()
        {
            energy = Mathf.Clamp(energy - (hungerFactor * Time.deltaTime), minEnergy, maxEnergy);
            if (energy <= hungerPoint)
            {
                agent.AddToState(Effects.Hungry);
            }

            if (energy <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            Destroy(gameObject);
        }
    }
}