using UnityEngine;
using UnityEngine.UI;
using Ecosystem.Fauna;

namespace Ecosystem.UI
{
    public class HydrationBar : MonoBehaviour
    {
        [SerializeField] Canvas rootCanvas = null;
        [SerializeField] RectTransform foreground = null;
        [SerializeField] Animal animal = null;

        private void LateUpdate()
        {
            float hydrationProportion = animal.GetHydrationProportion();
            if (
                !animal.GetIsAlive()
                || Mathf.Approximately(hydrationProportion, 0)
                || Mathf.Approximately(hydrationProportion, 1)
            )
            {
                rootCanvas.enabled = false;
                return;
            }

            rootCanvas.enabled = true;
            foreground.localScale = new Vector3(hydrationProportion, 1f, 1f);
        }
    }
}