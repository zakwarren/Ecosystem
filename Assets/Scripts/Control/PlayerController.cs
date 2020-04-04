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
        [SerializeField] float jumpHeight = 2f;

        Animator animator;
        CharacterController controller;

        Vector3 velocity;

        private void Awake()
        {
            animator = GetComponentInChildren<Animator>();
            controller = GetComponent<CharacterController>();
        }

        private void Update()
        {
            
            ProcessTranslation();
            SetMovementAnimation();
            ProcessRotation();
            ProcessJump();
        }

        private void ProcessTranslation()
        {
            Vector3 forward = transform.TransformDirection(Vector3.forward);
            float translationSpeed = Input.GetAxis("Vertical") * movementSpeed * Time.deltaTime;
            controller.Move(translationSpeed * forward);
        }

        private void ProcessRotation()
        {
            float yThrow = Input.GetAxis("Horizontal");
            float yaw = yThrow * turnSpeed * Time.deltaTime;
            transform.Rotate(0, yaw, 0);
        }

        private void ProcessJump()
        {
            if (controller.isGrounded && velocity.y < 0)
            {
                velocity.y = 0f;
            }

            if (Input.GetButtonDown("Jump") && controller.isGrounded)
            {
                velocity.y += Mathf.Sqrt(jumpHeight * -2f * Physics.gravity.y);
                animator.SetTrigger("jump");
            }

            velocity.y += Physics.gravity.y * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);
        }

        private void SetMovementAnimation()
        {
            float speedPercent = controller.velocity.magnitude / movementSpeed;
            animator.SetFloat("speedPercent", speedPercent, animationSmoothTime, Time.deltaTime);
        }
    }
}