using System.Collections.Generic;
using UnityEngine;

namespace AI.GOAP
{
    [CreateAssetMenu(fileName = "Action", menuName = "GOAP/Create New Action", order = 0)]
    public class Action : ScriptableObject
    {
        [SerializeField] string locationTag = null;
        [SerializeField] float duration = 0f;
        [SerializeField] float cost = 1f;
        [SerializeField] Dictionary<string, int> preconditions = null;
        [SerializeField] Dictionary<string, int> afterEffects = null;

        public string GetLocationTag() { return locationTag; }
        public float GetDuration() { return duration; }
        public float GetCost() { return cost; }
        public Dictionary<string, int> GetPreconditions() { return preconditions; }
        public Dictionary<string, int> GetAfterEffects() { return afterEffects; }

        public bool IsAchievable(Dictionary<string, int> conditions)
        {
            foreach (KeyValuePair<string, int> condition in preconditions)
            {
                if (!conditions.ContainsKey(condition.Key))
                {
                    return false;
                }
            }
            return true;
        }
    }
}