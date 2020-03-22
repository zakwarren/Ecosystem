using UnityEngine;

namespace Ecosystem.Flora
{
    [SelectionBase]
    public class Plant : MonoBehaviour
    {
        [SerializeField] float growthRate = 0.1f;
        [SerializeField] float sizeError = 0.01f;

        string myTag;
        Vector3 fullSize;
        bool isFullSize = true;

        private void Awake()
        {
            myTag = gameObject.tag;
            fullSize = transform.localScale;
        }

        private void Update()
        {
            if (!isFullSize)
            {
                Grow();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.tag == "Herbivore")
            {
                GetEaten();
            }
        }

        private void GetEaten()
        {
            transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            GetComponent<Collider>().enabled = false;
            gameObject.tag = "Untagged";
            isFullSize = false;
        }

        private void Grow()
        {
            float growthFactor = growthRate * Time.deltaTime;
            Vector3 growthScale = new Vector3(growthFactor, growthFactor, growthFactor);
            transform.localScale += growthScale;

            float sizeDiff = Vector3.Distance(fullSize, transform.localScale);
            if (sizeDiff < sizeError)
            {
                isFullSize = true;
                GetComponent<Collider>().enabled = true;
                gameObject.tag = myTag;
            }
        }
    }
}