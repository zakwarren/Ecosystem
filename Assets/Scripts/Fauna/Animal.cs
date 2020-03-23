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

        private void Awake()
        {
            agent = GetComponent<Agent>();
        }

        private void LateUpdate()
        {
            GetHungry();
        }

        private void RestoreEnergy(float calories)
        {
            energy = Mathf.Clamp(energy + calories, 0f, 100f);
            if (energy > hungerPoint)
            {
                agent.RemoveFromState(Effects.Hungry);
            }
        }

        private void GetHungry()
        {
            energy = Mathf.Clamp(energy - (hungerFactor * Time.deltaTime), 0f, 100f);
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