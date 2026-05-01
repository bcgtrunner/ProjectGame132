using UnityEngine;

public class Paint : MonoBehaviour
{
    private Wall attachedWall;

    public void AttachTo(Wall wall)
    {
        attachedWall = wall;
        wall.OnDestroy += Destroy;
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.TryGetComponent<PlayerController>(out var controller))
        {
            controller.TakeDamage(Time.deltaTime);
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
