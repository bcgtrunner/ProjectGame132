using System;
using UnityEngine;

/// <summary>
/// Marker component for walls the player can attach to and launch from.
/// Attach this to any GameObject with a BoxCollider that should be treated as an attachable wall.
/// </summary>
public class Wall : MonoBehaviour
{
    public int MaxHealth = 5;
    public int Health { get; private set; }

    public Action OnDestroy;

    private Color _initialColor;
    private Color _damageColor = Color.red;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private void Start()
    {
        Health = MaxHealth;

        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            MaterialPropertyBlock propertyBlock = new();
            renderer.GetPropertyBlock(propertyBlock);
            _initialColor = propertyBlock.HasProperty(BaseColorId) ? propertyBlock.GetColor(BaseColorId) : Color.white;
        }
        else
        {
            _initialColor = Color.white;
        }
    }

    public void Damage(int points)
    {
        if (Health <= 0) return;
        Health -= points;
        if (Health <= 0)
        {
            Health = 0;
            Destroy();
        }

        UpdateColor();
    }

    public void SetDamageColor(Color color)
    {
        _damageColor = color;
    }

    private void UpdateColor()
    {
        if (MaxHealth <= 0) return;

        float t = 1f - (float)Health / MaxHealth;
        Color currentColor = Color.Lerp(_initialColor, _damageColor, t);

        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer == null) return;

        MaterialPropertyBlock propertyBlock = new();
        renderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(BaseColorId, currentColor);
        renderer.SetPropertyBlock(propertyBlock);
    }

    private void Destroy()
    {
        OnDestroy?.Invoke();
        Destroy(gameObject);
    }
}