using UnityEngine;

[CreateAssetMenu(fileName = "Action", menuName = "GOAP/Create New Action", order = 0)]
public class Action : ScriptableObject
{
    [SerializeField] string locationTag = null;
    [SerializeField] float duration = 0f;

    public float GetDuration() { return duration; }

    public Transform GetLocation() {
        if (locationTag == null) { return null; }

        GameObject[] locations = GameObject.FindGameObjectsWithTag(locationTag);
        if (locations.Length <= 0) { return null; }

        GameObject location = locations[Random.Range(0, locations.Length - 1)];
        return location.transform;
    }
}