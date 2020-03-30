using UnityEngine;
using UnityEngine.AI;

namespace Ecosystem.UI
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] float animationSmoothTime = 0.1f;

        NavMeshAgent navMeshAgent;
        Animator animator;

        private void Awake()
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
            animator = GetComponentInChildren<Animator>();
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                InteractWithMovement();
            }
            SetMovementAnimation();
        }

        private void InteractWithMovement()
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray.origin, ray.direction, out hit))
            {
                navMeshAgent.destination = hit.point;
            }
        }

        private void SetMovementAnimation()
        {
            float speedPercent = navMeshAgent.velocity.magnitude / navMeshAgent.speed;
            animator.SetFloat("speedPercent", speedPercent, animationSmoothTime, Time.deltaTime);
        }
    }
}