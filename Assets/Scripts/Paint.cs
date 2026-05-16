using System.Collections.Generic;
using UnityEngine;

public class Paint : MonoBehaviour
{
    private Wall attachedWall;
    private readonly HashSet<PlayerController> _touching = new();

    public void AttachTo(Wall wall)
    {
        if (wall == null) Destroy(gameObject);
        attachedWall = wall;
        transform.SetParent(wall.transform);
        wall.Destroyed += Destroy;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<PlayerController>(out var controller) && _touching.Add(controller))
        {
            controller.OnPaintContact(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<PlayerController>(out var controller) && _touching.Remove(controller))
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
        foreach (var controller in _touching)
        {
            if (controller != null)
                controller.OnPaintContact(false);
        }
        _touching.Clear();

        if (attachedWall != null)
            attachedWall.Destroyed -= Destroy;
    }
}
