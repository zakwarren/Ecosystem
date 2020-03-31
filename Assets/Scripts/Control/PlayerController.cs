using UnityEngine;
using UnityEngine.AI;

namespace Ecosystem.Control
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] float animationSmoothTime = 0.1f;
        [SerializeField] float movementSpeed = 250f;
        [SerializeField] float turnSpeed = 60f;

        // NavMeshAgent navMeshAgent;
        Animator animator;
        CharacterController controller;

        private void Awake()
        {
            animator = GetComponentInChildren<Animator>();
            controller = GetComponent<CharacterController>();
        }

        private void Update()
        {
            
            ProcessTranslation();
            ProcessRotation();

            SetMovementAnimation();
        }

        private void ProcessTranslation()
        {
            Vector3 forward = transform.TransformDirection(Vector3.forward);
            float speed = Input.GetAxis("Vertical") * movementSpeed;
            controller.SimpleMove(forward * speed);
        }

        private void ProcessRotation()
        {
            float yThrow = Input.GetAxis("Horizontal");
            float yaw = yThrow * turnSpeed * Time.deltaTime;
            transform.Rotate(0, yaw, 0);
        }

        private void SetMovementAnimation()
        {
            float speedPercent = controller.velocity.magnitude / movementSpeed;
            animator.SetFloat("speedPercent", speedPercent, animationSmoothTime, Time.deltaTime);
        }
    }
}