using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Agent : MonoBehaviour
{
    [SerializeField] float senseRadius = 10f;
    [SerializeField] float withinTargetRange = 2f;
    [SerializeField] Action[] actions = null;

    NavMeshAgent navMeshAgent;
    SphereCollider senseSphere;
    Action currentAction;
    Vector3 currentDestination;

    bool isDoingAction = false;

    private void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        senseSphere = GetComponent<SphereCollider>();
    }

    private void Start()
    {
        senseSphere.radius = senseRadius;
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

    private void OnTriggerEnter(Collider other)
    {
        if (currentAction == null) { return; }
        if (other.gameObject.tag == currentAction.GetLocationTag())
        {
            currentDestination = other.transform.position;
            MoveTo(currentDestination);
        }
    }

    private void SetCurrentAction()
    {
        if (actions.Length > 0)
        {
            currentAction = actions[Random.Range(0, actions.Length)];
            isDoingAction = true;
            Debug.Log("New action: " + currentAction.name);
        }
    }

    private void MoveTo(Vector3 newDestination)
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

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, senseRadius);
    }
}
