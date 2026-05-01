using UnityEngine;

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
        if (other.TryGetComponent<PlayerController>(out var controller))
        {
            controller.OnPaintContact(true);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.TryGetComponent<PlayerController>(out var controller))
        {
            controller.TakeDamage(Time.deltaTime);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<PlayerController>(out var controller))
        {
            controller.OnPaintContact(false);
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
