using UnityEngine;
using UnityEngine.SceneManagement;

public class Paint : MonoBehaviour
{
    private Wall attachedWall;

    public void AttachTo(Wall wall)
    {
        attachedWall = wall;
        wall.OnDestroy += Destroy;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<PlayerInput>(out var player))
        {
            SceneManager.LoadScene(0);
        }
        else if (other.TryGetComponent<PlayerController>(out var controller))
        {
            Destroy(controller.gameObject);
        }
    }

    private void Destroy()
    {
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        attachedWall.OnDestroy -= Destroy;
    }
}
