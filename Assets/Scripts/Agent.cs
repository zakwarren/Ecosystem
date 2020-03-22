using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Agent : MonoBehaviour
{
    [SerializeField] float withinTargetRange = 2f;
    [SerializeField] Action[] actions = null;

    NavMeshAgent navMeshAgent;
    Action currentAction;
    Vector3 currentDestination;

    bool isDoingAction = false;

    private void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
    }

    private void LateUpdate()
    {
        if (!isDoingAction && currentAction == null)
        {
            SetCurrentAction();
        }

        float distanceToTarget = Vector3.Distance(currentDestination, this.transform.position);
        if (isDoingAction && distanceToTarget < withinTargetRange)
        {
            StartCoroutine(DoAction());
        }
    }

    private void SetCurrentAction()
    {
        if (actions.Length > 0)
        {
            isDoingAction = true;
            currentAction = actions[Random.Range(0, actions.Length)];
            Transform newLocation = currentAction.GetLocation();
            if (newLocation == null) {
                EndAction();
                return;
            }

            Debug.Log("New action: " + currentAction.name);
            currentDestination = newLocation.position;
            MoveTo(currentDestination);
        }
    }

    public void MoveTo(Vector3 newDestination)
    {
        navMeshAgent.destination = newDestination;
        navMeshAgent.isStopped = false;
    }

    private IEnumerator DoAction()
    {
        navMeshAgent.isStopped = true;
        yield return new WaitForSeconds(currentAction.GetDuration());
        EndAction();
    }

    private void EndAction()
    {
        currentAction = null;
        isDoingAction = false;
    }
}
