using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace AI.GOAP
{
    [SelectionBase]
    [RequireComponent(typeof(NavMeshAgent))]
    public class Agent : MonoBehaviour
    {
        [SerializeField] float senseRadius = 10f;
        [SerializeField] float searchDistance = 10f;
        [SerializeField] float withinTargetRange = 2f;
        [SerializeField] List<Action> actions = null;

        NavMeshAgent navMeshAgent;
        SphereCollider senseSphere;
        Action currentAction;
        Vector3 currentDestination;

        bool isDoingAction = false;
        bool isSearching = false;
        bool foundTarget = false;

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
            if (currentAction == null)
            {
                SetCurrentAction();
            }

            if (!isDoingAction && !isSearching)
            {
                SearchBehaviour();
            }

            float distanceToTarget = Vector3.Distance(currentDestination, transform.position);
            if (distanceToTarget < withinTargetRange)
            {
                isSearching = false;
                if (foundTarget) {
                    StartCoroutine(DoAction());
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (currentAction == null) { return; }
            if (other.gameObject.tag == currentAction.GetLocationTag())
            {
                foundTarget = true;
                Vector3 dest = other.ClosestPoint(transform.position);
                MoveTo(dest);
            }
        }

        private void SetCurrentAction()
        {
            if (actions.Count > 0)
            {
                currentAction = actions[Random.Range(0, actions.Count)];
                Debug.Log("New action: " + currentAction.name);
            }
        }

        private void SearchBehaviour()
        {
            isSearching = true;
            Vector3 randomDirection = Random.insideUnitSphere * searchDistance;
            randomDirection += transform.position;
            NavMeshHit navHit;
            NavMesh.SamplePosition (randomDirection, out navHit, searchDistance, -1);
            MoveTo(navHit.position);
        }

        private void MoveTo(Vector3 newDestination)
        {
            currentDestination = newDestination;
            navMeshAgent.destination = currentDestination;
            navMeshAgent.isStopped = false;
        }

        private IEnumerator DoAction()
        {
            isDoingAction = true;
            navMeshAgent.isStopped = true;
            yield return new WaitForSeconds(currentAction.GetDuration());
            currentAction = null;
            isDoingAction = false;
            foundTarget = false;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(transform.position, senseRadius);

            if (foundTarget)
            {
                Gizmos.DrawLine(transform.position, currentDestination);
            }
        }
    }
}