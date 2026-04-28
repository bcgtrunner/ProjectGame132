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
        if (other.TryGetComponent<AIInput>(out var player))
        {
            Destroy(player.gameObject);
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
