using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ecosystem.UI
{
    public class PopulationMonitor : MonoBehaviour
    {
        [SerializeField] Transform populationParent = null;
        [SerializeField] Text populationText = null;
        [SerializeField] Text birthsText = null;
        [SerializeField] Text deathsText = null;

        List<GameObject> lastPopulation = new List<GameObject>();
        int populationCount = 0;
        int birthsCount = 0;
        int deathsCount = 0;

        private void LateUpdate()
        {
            List<GameObject> currentPopulation = new List<GameObject>();
            foreach (Transform child in populationParent)
            {
                currentPopulation.Add(child.gameObject);
                if (!lastPopulation.Contains(child.gameObject))
                {
                    birthsCount++;
                }
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
        }
    }
}
