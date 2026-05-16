using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Paint : MonoBehaviour
{
    private Wall attachedWall;
    private readonly HashSet<PlayerController> _touching = new();
    [SerializeField] private Collider _collider;

    public void ResetState()
    {
        transform.SetParent(null);
        transform.localScale = Vector3.one;
        _collider.enabled = true;
        
        foreach (var controller in _touching)
        {
            if (controller != null)
                controller.OnPaintContact(false);
        }
        _touching.Clear();

        if (attachedWall != null)
        {
            attachedWall.Destroyed -= HandleWallDestroyed;
            attachedWall = null;
        }
    }

    public void AttachTo(Wall wall)
    {
        if (wall == null)
        {
            ReleaseOrDestroy();
            return;
        }
        attachedWall = wall;
        transform.SetParent(wall.transform);
        attachedWall.Destroyed += HandleWallDestroyed;
    }

    private void HandleWallDestroyed()
    {
        _collider.enabled = false;
        ReleaseOrDestroy();
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

    public void ReleaseOrDestroy()
    {
        if (PaintPool.Instance != null && gameObject.activeSelf)
        {
            PaintPool.Instance.Release(this);
        }
        else
        {
            Destroy(gameObject);
        }
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
            attachedWall.Destroyed -= HandleWallDestroyed;
    }
}
