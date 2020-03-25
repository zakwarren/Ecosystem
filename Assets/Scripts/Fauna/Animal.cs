﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using AI.GOAP;
using Ecosystem;

namespace Ecosystem.Fauna
{
    [SelectionBase]
    [RequireComponent(typeof(Agent))]
    public class Animal : MonoBehaviour
    {
        [Header("Metabolism")]
        [Range(0f, 1f)]
        [SerializeField] float metabolicRate = 0.1f;
        [Range(0f, 100f)]
        [SerializeField] float comfortPoint = 90f;
        [Range(0f, 100f)]
        [SerializeField] float thirstPoint = 80f;
        [Range(0f, 100f)]
        [SerializeField] float hungerPoint = 60f;
        [SerializeField] float maxSpeed = 8f;

        Agent agent;
        NavMeshAgent navMeshAgent;

        const float maxStorage = 100f;
        const float minStorage = 0f;
        float hydration = 100f;
        float energy = 100f;

        private void Awake()
        {
            agent = GetComponent<Agent>();
            navMeshAgent = GetComponent<NavMeshAgent>();
        }

        private void OnEnable()
        {
            agent.onDoingAction += HandleAction;
        }
    
        private void OnDisable()
        {
            agent.onDoingAction -= HandleAction;
        }

        private void Start()
        {
            navMeshAgent.speed = maxSpeed;
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
            Vector3 localVelocity = transform.InverseTransformDirection(navMeshAgent.velocity);
            float thirstFactor = Mathf.Abs((localVelocity.z * metabolicRate) * Time.deltaTime);
            if (thirstFactor == 0) { thirstFactor = metabolicRate; }
            hydration = Mathf.Clamp(hydration - thirstFactor, minStorage, maxStorage);

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
            Vector3 localVelocity = transform.InverseTransformDirection(navMeshAgent.velocity);
            float hungerFactor = Mathf.Abs((localVelocity.z * metabolicRate) * Time.deltaTime);
            if (hungerFactor == 0) { hungerFactor = metabolicRate; }
            energy = Mathf.Clamp(energy - hungerFactor, minStorage, maxStorage);

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