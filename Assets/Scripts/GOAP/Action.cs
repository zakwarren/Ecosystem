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
        [SerializeField] List<Effects> preconditions = null;
        [SerializeField] List<Effects> afterEffects = null;

        public string GetLocationTag() { return locationTag; }
        public float GetDuration() { return duration; }
        public float GetCost() { return cost; }
        public List<Effects> GetPreconditions() { return preconditions; }
        public List<Effects> GetAfterEffects() { return afterEffects; }

        public bool IsAchievable(List<Effects> conditions)
        {
            foreach (Effects condition in preconditions)
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