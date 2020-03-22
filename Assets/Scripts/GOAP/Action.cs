using UnityEngine;

namespace AI.GOAP
{
    [CreateAssetMenu(fileName = "Action", menuName = "GOAP/Create New Action", order = 0)]
    public class Action : ScriptableObject
    {
        [SerializeField] string locationTag = null;
        [SerializeField] float duration = 0f;

        public string GetLocationTag() { return locationTag; }
        public float GetDuration() { return duration; }
    }
}