using UnityEngine;
using Ecosystem;

namespace Ecosystem.Environment
{
    public class Water : MonoBehaviour, IConsumable
    {
        [SerializeField] float waterPerConsumption = 20f;

        public float GetConsumed()
        {
            return waterPerConsumption;
        }
    }
}