using System.Collections.Generic;
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
        [SerializeField] float discomfortPoint = 60f;
        [SerializeField] float maxSpeed = 8f;
        [Header("Reproduction")]
        [SerializeField] bool isFemale = true;
        [Tooltip("Time to gestate a baby for females and cooldown period for males")]
        [SerializeField] float gestationPeriod = 20f;

        Agent agent;
        NavMeshAgent navMeshAgent;
        Animal currentMate;

        const float maxStorage = 100f;
        const float minStorage = 0f;
        float hydration = 100f;
        float energy = 100f;
        float gestation = 0f;
        bool isGestating = false;
        bool isReceptive = false;

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
            if (comfortPoint < discomfortPoint) {
                Debug.LogError("Comfort Point should be set above Discomfort Point");
                comfortPoint = discomfortPoint;
            }
        }

        private void LateUpdate()
        {
            Dehydrate();
            GetHungry();
            Gestate();
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

            if (hydration > discomfortPoint && energy > discomfortPoint && !isGestating)
            {
                agent.AddToState(Effects.DesireForMate);
            }
            else
            {
                agent.RemoveFromState(Effects.DesireForMate);
                agent.RemoveFromState(Effects.FoundMate);
                if (agent.GetCurrentGoal() == Effects.FoundMate)
                {
                    agent.CancelCurrentGoal();
                }
            }
        }

        private void HandleAction(GameObject target, List<Effects> afterEffects)
        {
            if (afterEffects.Contains(Effects.Quenched))
            {
                Drink(target);
            }
            if (afterEffects.Contains(Effects.Sated))
            {
                Eat(target);
            }

            if (afterEffects.Contains(Effects.FoundMate))
            {
                isReceptive = true;
                currentMate = target.GetComponent<Animal>();
                if (currentMate == null || !currentMate.AcceptsMate(this))
                {
                    agent.RemoveFromState(Effects.FoundMate);
                    agent.CancelCurrentGoal();
                }
            }
            else if (afterEffects.Contains(Effects.Mated) && currentMate != null && !isFemale)
            {
                Mate();
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

        private void Mate()
        {
            agent.MoveTo(currentMate.transform.position);
            if (!isFemale && currentMate.AcceptsMate(this))
            {
                currentMate.ProduceBaby(true);
                gestation = gestationPeriod;
                isGestating = true;
            }
            isReceptive = false;
            currentMate = null;
        }

        public void ProduceBaby(bool genes)
        {
            // Instantiate baby with combined gene set

            gestation = gestationPeriod;
            isGestating = true;
            isReceptive = false;
            currentMate = null;
        }

        private void Gestate()
        {
            if (gestation > 0f)
            {
                gestation -= Time.deltaTime;
            }
            else if (isGestating && gestation <= 0f)
            {
                isGestating = false;
                gestation = 0f;
                agent.AddNewGoal(Effects.Mated, 1, true);
            }
            else {
                agent.RemoveFromState(Effects.Mated);
            }
        }

        private void Dehydrate()
        {
            Vector3 localVelocity = transform.InverseTransformDirection(navMeshAgent.velocity);
            float thirstFactor = Mathf.Abs((localVelocity.z * metabolicRate) * Time.deltaTime);
            if (thirstFactor == 0) { thirstFactor = metabolicRate; }
            hydration = Mathf.Clamp(hydration - thirstFactor, minStorage, maxStorage);

            if (hydration <= discomfortPoint)
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

            if (energy <= discomfortPoint)
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

        public bool GetIsFemale()
        {
            return isFemale;
        }

        public bool AcceptsMate(Animal potentialMate)
        {
            if (potentialMate.GetIsFemale() == isFemale || !isReceptive)
            {
                return false;
            }

            agent.AddToState(Effects.Mated);
            currentMate = potentialMate;
            return true;
        }
    }
}