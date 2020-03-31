using UnityEngine;

namespace Ecosystem.Control
{
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] Transform targetToFollow = default;
        [SerializeField] float distance = 6f;
        [SerializeField] float height = 3f;
        [SerializeField] float heightOffset = 0f;
        [SerializeField] float heightDamping = 4f;
        [SerializeField] float rotationDamping = 2f;

        private void LateUpdate()
        {
            ViewThirdPerson();
        }

        private void ViewThirdPerson()
        {
            float wantedRotationAngle = targetToFollow.eulerAngles.y;
            float wantedHeight = targetToFollow.position.y + height;

            float currentRotationAngle = transform.eulerAngles.y;
            float currentHeight = transform.position.y;

            currentRotationAngle = Mathf.LerpAngle(
                currentRotationAngle,
                wantedRotationAngle,
                rotationDamping * Time.deltaTime
            );
            currentHeight = Mathf.Lerp(
                currentHeight,
                wantedHeight,
                heightDamping * Time.deltaTime
            );

            Quaternion currentRotation = Quaternion.Euler(0, currentRotationAngle, 0);

            transform.position = targetToFollow.position;
            transform.position -= currentRotation * Vector3.forward * distance;

            transform.position = new Vector3(
                transform.position.x,
                currentHeight + heightOffset,
                transform.position.z
            );

            transform.LookAt(targetToFollow);
        }
    }
}