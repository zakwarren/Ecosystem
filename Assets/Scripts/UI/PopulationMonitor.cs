using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Ecosystem.Fauna;

namespace Ecosystem.UI
{
    public class PopulationMonitor : MonoBehaviour
    {
        [SerializeField] Transform populationParent = null;
        [SerializeField] Text populationText = null;
        [SerializeField] Text birthsText = null;
        [SerializeField] Text deathsText = null;
        [SerializeField] Text genderText = null;
        [SerializeField] Text speedText = null;
        [SerializeField] Text gestationText = null;
        [SerializeField] Text growthText = null;

        List<GameObject> lastPopulation = new List<GameObject>();

        private void LateUpdate()
        {
            int populationCount = 0;
            int birthsCount = 0;
            int deathsCount = 0;
            int femaleCount = 0;
            float cumulativeSpeed = 0f;
            float cumulativeGestationPeriod = 0f;
            float cumulativeGrowthRate = 0f;
            List<GameObject> currentPopulation = new List<GameObject>();

            foreach (Transform child in populationParent)
            {
                currentPopulation.Add(child.gameObject);
                if (!lastPopulation.Contains(child.gameObject))
                {
                    birthsCount++;
                }

                Animal.Genetics genes = child.GetComponent<Animal>().GetGeneset();
                if (genes.isFemale) { femaleCount++; }
                cumulativeSpeed += genes.maxSpeed;
                cumulativeGestationPeriod += genes.gestationPeriod;
                cumulativeGrowthRate += genes.growthRate;

            }

            foreach (GameObject individual in lastPopulation)
            {
                if (!currentPopulation.Contains(individual))
                {
                    deathsCount++;
                }
            }
            lastPopulation = currentPopulation;
            populationCount = currentPopulation.Count;

            populationText.text = populationCount.ToString();
            birthsText.text = birthsCount.ToString();
            deathsText.text = deathsCount.ToString();
            genderText.text = femaleCount.ToString() + " : " + (populationCount - femaleCount).ToString();
            speedText.text = (cumulativeSpeed / populationCount).ToString("F2");
            gestationText.text = (cumulativeGestationPeriod / populationCount).ToString("F2");
            growthText.text = (cumulativeGrowthRate / populationCount).ToString("F2");
        }
    }
}
