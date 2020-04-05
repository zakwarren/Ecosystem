using UnityEngine;
using UnityEngine.AI;

namespace Ecosystem.Characters
{
    public class Follower : MonoBehaviour
    {
        [SerializeField] float animationSmoothTime = 0.1f;
        [SerializeField] float closenessToPlayer = 2f;

        NavMeshAgent navMeshAgent;
        Animator animator;
        GameObject player;

        private void Awake()
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
            animator = GetComponentInChildren<Animator>();
            player = GameObject.FindWithTag("Player");
        }

        private void LateUpdate()
        {
            FollowPlayer();
            SetMovementAnimation();
        }

        private void FollowPlayer()
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
            if (distanceToPlayer > closenessToPlayer && CanMoveTo(player.transform.position))
            {
                navMeshAgent.destination = player.transform.position;
                navMeshAgent.isStopped = false;
            }
            else
            {
                navMeshAgent.isStopped = true;
            }
        }

        private bool CanMoveTo(Vector3 destination)
        {
            NavMeshPath path = new NavMeshPath();
            bool hasPath = NavMesh.CalculatePath(transform.position, destination, NavMesh.AllAreas, path);
            if (!hasPath) { return false; }
            if (path.status != NavMeshPathStatus.PathComplete) { return false; }
            return true;
        }

        private void SetMovementAnimation()
        {
            float speedPercent = navMeshAgent.velocity.magnitude / navMeshAgent.speed;
            animator.SetFloat("speedPercent", speedPercent, animationSmoothTime, Time.deltaTime);
        }
    }
}