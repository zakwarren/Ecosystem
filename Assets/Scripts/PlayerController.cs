using UnityEngine;

public class PlayerController : MonoBehaviour
{
    Agent agent;

    private void Awake()
    {
        agent = GetComponent<Agent>();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            InteractWithMovement();
        }
    }

    private void InteractWithMovement()
    {
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray.origin, ray.direction, out hit))
        {
            agent.MoveTo(hit.point);
        }
    }
}
