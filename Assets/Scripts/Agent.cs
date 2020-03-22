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

    private void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        if (actions.Length > 0)
        {
            currentAction = actions[0];
            MoveTo(actions[0].GetLocation().position);
        }
    }

    private void LateUpdate()
    {
        if (currentDestination == Vector3.zero) { return; }

        float distanceToTarget = Vector3.Distance(currentDestination, this.transform.position);
        if (distanceToTarget < withinTargetRange)
        {
            StartCoroutine(DoAction());
        }
    }

    public void MoveTo(Vector3 newDestination)
    {
        currentDestination = newDestination;
        navMeshAgent.destination = newDestination;
        navMeshAgent.isStopped = false;
    }

    private IEnumerator DoAction()
    {
        navMeshAgent.isStopped = true;
        yield return new WaitForSeconds(currentAction.GetDuration());
        currentAction = null;
        currentDestination = Vector3.zero;
    }
}
