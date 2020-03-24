using UnityEngine;
using UnityEngine.UI;
using AI.GOAP;

namespace Ecosystem.UI
{
    public class ActionDisplay : MonoBehaviour
    {
        [SerializeField] Canvas rootCanvas = null;
        [SerializeField] Text textDisplay = null;
        [SerializeField] Agent agent = null;

        private void LateUpdate()
        {
            Action currentAction = agent.GetCurrentAction();
            if (currentAction == null)
            {
                rootCanvas.enabled = false;
                return;
            }

            rootCanvas.enabled = true;
            textDisplay.text = currentAction.name;
        }
    }
}