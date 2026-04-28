using System;
using UnityEngine;

/// <summary>
/// Marker component for walls the player can attach to and launch from.
/// Attach this to any GameObject with a BoxCollider that should be treated as an attachable wall.
/// </summary>
public class Wall : MonoBehaviour
{
    public int MaxHealth = 20;
    public int Health { get; private set; }

    public Action OnDestroy;

    private void Start()
    {
        Health = MaxHealth;
    }

    public void Damage(int points)
    {
        Health -= points;
        if (Health <= 0)
        {
            Health = 0;
            Destroy();
        }
    }

    private void Destroy()
    {
        OnDestroy?.Invoke();
        Destroy(gameObject);
    }
}