using UnityEngine;

public class Paint : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("something");
        Destroy(other.gameObject);
    }
}
