using System.Collections.Generic;
using UnityEngine;
using AI.GOAP;
using Ecosystem;

namespace Ecosystem.Fauna
{
    [SelectionBase]
    [RequireComponent(typeof(Agent))]
    public class Animal : MonoBehaviour
    {
        [Range(0f, 100f)]
        [SerializeField] float comfortPoint = 90f;

        [Header("Hydration")]
        [Range(0f, 100f)]
        [SerializeField] float thirstFactor = 1f;
        [Range(0f, 100f)]
        [SerializeField] float thirstPoint = 80f;
        [Header("Energy")]
        [Range(0f, 100f)]
        [SerializeField] float hungerFactor = 0.5f;
        [Range(0f, 100f)]
        [SerializeField] float hungerPoint = 60f;

        Agent agent;

        const float maxStorage = 100f;
        const float minStorage = 0f;
        float hydration = 100f;
        float energy = 100f;

        private void Awake()
        {
            agent = GetComponent<Agent>();
        }

        private void OnEnable()
        {
            agent.onDoingAction += HandleAction;
        }
    
        private void OnDisable()
        {
            agent.onDoingAction -= HandleAction;
        }

        private void LateUpdate()
        {
            Dehydrate();
            GetHungry();
            CheckState();
        }

        private void CheckState()
        {
            if (hydration < comfortPoint)
            {
                agent.RemoveFromState(Effects.Quenched);
            }
            if (energy < comfortPoint)
            {
                agent.RemoveFromState(Effects.Sated);
            }
        }

        private void HandleAction(GameObject consumable, List<Effects> afterEffects)
        {
            if (afterEffects.Contains(Effects.Quenched))
            {
                Drink(consumable);
            }
            if (afterEffects.Contains(Effects.Sated))
            {
                Eat(consumable);
            }
        }

        private void Drink(GameObject consumable)
        {
            IConsumable water = consumable.GetComponent<IConsumable>();
            if (water != null)
            {
                float waterDrunk = water.GetConsumed();
                Rehydrate(waterDrunk);
            }
        }

        private void Eat(GameObject food)
        {
            IFood foodItem = food.GetComponent<IFood>();
            if (foodItem != null)
            {
                float calories = foodItem.GetEaten();
                RestoreEnergy(calories);
            }
        }

        private void Dehydrate()
        {
            hydration = Mathf.Clamp(hydration - (thirstFactor * Time.deltaTime), minStorage, maxStorage);
            if (hydration <= thirstPoint)
            {
                agent.AddToState(Effects.Thirsty);
            }

            if (hydration <= 0)
            {
                Die();
            }
        }

        private void GetHungry()
        {
            energy = Mathf.Clamp(energy - (hungerFactor * Time.deltaTime), minStorage, maxStorage);
            if (energy <= hungerPoint)
            {
                agent.AddToState(Effects.Hungry);
            }

            if (energy <= 0)
            {
                Die();
            }
        }

        private void Rehydrate(float waterDrunk)
        {
            hydration = Mathf.Clamp(hydration + waterDrunk, minStorage, maxStorage);
        }

        private void RestoreEnergy(float calories)
        {
            energy = Mathf.Clamp(energy + calories, minStorage, maxStorage);
        }

        private void Die()
        {
            Destroy(gameObject);
        }

        public float GetHydrationProportion()
        {
            return hydration / maxStorage;
        }

        public float GetEnergyProportion()
        {
            return energy / maxStorage;
        }
    }
}