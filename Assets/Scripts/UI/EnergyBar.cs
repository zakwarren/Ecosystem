using UnityEngine;
using UnityEngine.UI;
using Ecosystem.Fauna;

namespace Ecosystem.UI
{
    public class EnergyBar : MonoBehaviour
    {
        [SerializeField] Canvas rootCanvas = null;
        [SerializeField] RectTransform foreground = null;
        [SerializeField] Animal animal = null;

        private void LateUpdate()
        {
            float energyProportion = animal.GetEnergyProportion();
            if (
                !animal.GetIsAlive()
                || Mathf.Approximately(energyProportion, 0)
                || Mathf.Approximately(energyProportion, 1)
            )
            {
                rootCanvas.enabled = false;
                return;
            }

            rootCanvas.enabled = true;
            foreground.localScale = new Vector3(energyProportion, 1f, 1f);
        }
    }
}