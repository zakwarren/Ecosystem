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
        [SerializeField] List<string> preconditions = null;
        [SerializeField] List<string> afterEffects = null;

        public string GetLocationTag() { return locationTag; }
        public float GetDuration() { return duration; }
        public float GetCost() { return cost; }
        public List<string> GetPreconditions() { return preconditions; }
        public List<string> GetAfterEffects() { return afterEffects; }

        public bool IsAchievable(List<string> conditions)
        {
            foreach (string condition in preconditions)
            {
                if (!conditions.Contains(condition))
                {
                    return false;
                }
            }
            return true;
        }
    }
}