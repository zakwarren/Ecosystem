using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using AI.GOAP;
using Ecosystem;

namespace Ecosystem.Fauna
{
    [SelectionBase]
    [RequireComponent(typeof(Agent))]
    public class Animal : MonoBehaviour, IFood
    {
        [Header("Metabolism")]
        [Range(0f, 1f)]
        [SerializeField] float metabolicRate = 0.1f;
        [Range(0f, 100f)]
        [SerializeField] float comfortPoint = 90f;
        [Range(0f, 100f)]
        [SerializeField] float discomfortPoint = 60f;
        [SerializeField] float maxSpeed = 8f;
        [SerializeField] float calories = 60f;
        [Header("Reproduction")]
        [SerializeField] bool isFemale = true;
        [Tooltip("Time to gestate a baby for females and cooldown period for males")]
        [SerializeField] float gestationPeriod = 20f;
        [Range(0f, 1f)]
        [SerializeField] float growthRate = 0.05f;
        [SerializeField] Animal animalPrefab = null;

        Agent agent;
        NavMeshAgent navMeshAgent;
        Animal currentMate;
        Genetics geneset;

        const float maxStorage = 100f;
        const float minStorage = 0f;
        float hydration = 100f;
        float energy = 100f;
        float gestation = 0f;
        bool isGestating = false;
        bool isReceptive = false;
        bool isAdult = true;
        float babySizeProportion = 0.2f;

        public class Genetics
        {
            public float metabolicRate = 0.1f;
            public float comfortPoint = 90f;
            public float discomfortPoint = 60f;
            public float maxSpeed = 8f;
            public bool isFemale = true;
            public float gestationPeriod = 20f;
            public float growthRate = 0.05f;

            public Genetics(float metabolicRate, float comfortPoint, float discomfortPoint, float maxSpeed, bool isFemale, float gestationPeriod, float growthRate)
            {
                this.metabolicRate = metabolicRate;
                this.comfortPoint = comfortPoint;
                this.discomfortPoint = discomfortPoint;
                this.maxSpeed = maxSpeed;
                this.isFemale = isFemale;
                this.gestationPeriod = gestationPeriod;
                this.growthRate = growthRate;
            }

            public Genetics Recombinate(Genetics otherGenes)
            {
                float newMetabolicRate = (this.metabolicRate + otherGenes.metabolicRate) / 2;
                float newComfortPoint = (this.comfortPoint + otherGenes.comfortPoint) / 2;
                float newDiscomfortPoint = (this.discomfortPoint + otherGenes.discomfortPoint) / 2;
                float newMaxSpeed = (this.maxSpeed + otherGenes.maxSpeed) / 2;
                bool newIsFemale = (Random.value > 0.5f);
                float newGestationPeriod = (this.gestationPeriod + otherGenes.gestationPeriod) / 2;
                float newGrowthRate = (this.growthRate + otherGenes.growthRate) / 2;

                return new Genetics(newMetabolicRate, newComfortPoint, newDiscomfortPoint, newMaxSpeed, newIsFemale, newGestationPeriod, newGrowthRate);
            }
        }

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

            if (geneset == null)
            {
                geneset = new Genetics(metabolicRate, comfortPoint, discomfortPoint, maxSpeed, isFemale, gestationPeriod, growthRate);
                babySizeProportion = gestationPeriod / 100f;
            }
        }

        private void LateUpdate()
        {
            Dehydrate();
            GetHungry();
            Gestate();
            CheckState();
            if (!isAdult)
            {
                GrowToAdulthood();
            }
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

            if (isAdult && !isGestating && hydration > discomfortPoint && energy > discomfortPoint)
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
            Vector3 matePostion = new Vector3(
                currentMate.transform.position.x,
                currentMate.transform.position.y,
                currentMate.transform.position.z - (transform.localScale.z / 2)
            );
            agent.MoveTo(matePostion);
            if (!isFemale && currentMate.AcceptsMate(this))
            {
                currentMate.ProduceBaby(geneset);
                gestation = gestationPeriod;
                isGestating = true;
                energy = Mathf.Clamp(energy - maxSpeed, minStorage, maxStorage);
            }
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

        private IEnumerator GiveBirth(Genetics newGenes)
        {
            yield return new WaitForSeconds(gestationPeriod);
            if (animalPrefab != null) {
                Animal baby = Instantiate(animalPrefab, transform.position, Quaternion.identity);
                baby.transform.parent = transform.parent;
                baby.BeBorn(newGenes);
            }
            else
            {
                Debug.LogError("No animal prefab assigned to " + gameObject.name);
            }
            metabolicRate = geneset.metabolicRate;
            navMeshAgent.speed = geneset.maxSpeed;
            energy = Mathf.Clamp(energy - (babySizeProportion * 100f), minStorage, maxStorage);
        }

        private void BeBorn(Genetics newGenes)
        {
            babySizeProportion = newGenes.gestationPeriod / 100f;
            navMeshAgent.speed = newGenes.maxSpeed * babySizeProportion;
            geneset = newGenes;

            metabolicRate = newGenes.metabolicRate + growthRate;
            comfortPoint = newGenes.comfortPoint;
            discomfortPoint = newGenes.discomfortPoint + (babySizeProportion * 100f);
            maxSpeed = newGenes.maxSpeed;
            isFemale = newGenes.isFemale;
            gestationPeriod = newGenes.gestationPeriod;
            growthRate = newGenes.growthRate;

            isAdult = false;
            transform.localScale = new Vector3(babySizeProportion, babySizeProportion, babySizeProportion);
            if (isFemale) {
                gameObject.name = gameObject.tag + " Female";
            }
            else
            {
                gameObject.name = gameObject.tag + " Male";
            }
        }

        private void GrowToAdulthood()
        {
            float growthFactor = growthRate * Time.deltaTime;
            Vector3 growthScale = new Vector3(growthFactor, growthFactor, growthFactor);
            transform.localScale += growthScale;

            float sizeDiff = Vector3.Distance(Vector3.one, transform.localScale);
            if (sizeDiff <= 0.1f)
            {
                isAdult = true;
                metabolicRate = geneset.metabolicRate;
                discomfortPoint = geneset.discomfortPoint;
                navMeshAgent.speed = geneset.maxSpeed;
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
            Destroy(gameObject, 1f);
        }

        public Genetics GetGeneset()
        {
            return geneset;
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

        public void ProduceBaby(Genetics donorGenes)
        {
            Genetics newGenes = geneset.Recombinate(donorGenes);
            StartCoroutine(GiveBirth(newGenes));

            metabolicRate = metabolicRate + (metabolicRate * babySizeProportion);
            navMeshAgent.speed = maxSpeed * babySizeProportion;
            gestation = gestationPeriod;
            isGestating = true;
            isReceptive = false;
            currentMate = null;
            energy = Mathf.Clamp(energy - maxSpeed, minStorage, maxStorage);
        }

        public float GetEaten()
        {
            Die();
            return calories;
        }
    }
}