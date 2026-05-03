using System;
using UnityEngine;

/// <summary>
/// Marker component for walls the player can attach to and launch from.
/// Attach this to any GameObject with a BoxCollider that should be treated as an attachable wall.
/// </summary>
public class Wall : MonoBehaviour
{
    public float MaxHealth = 5;
    public float Health { get; private set; }

    public Action Destroyed;

    private Color _initialColor;
    private Color _damageColor = Color.red;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private MeshRenderer _renderer;
    private MaterialPropertyBlock _propertyBlock;

    private void Start()
    {
        Health = MaxHealth;

        _renderer = GetComponent<MeshRenderer>();
        _propertyBlock = new MaterialPropertyBlock();
        if (_renderer != null)
        {
            _renderer.GetPropertyBlock(_propertyBlock);
            _initialColor = _propertyBlock.HasProperty(BaseColorId) ? _propertyBlock.GetColor(BaseColorId) : Color.white;
        }
        else
        {
            _initialColor = Color.white;
        }
    }

    public void Damage(float points)
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
        if (MaxHealth <= 0 || _renderer == null) return;

        float t = 1f - Mathf.Clamp01(Health / MaxHealth);
        Color currentColor = Color.Lerp(_initialColor, _damageColor, t);

        _propertyBlock.SetColor(BaseColorId, currentColor);
        _renderer.SetPropertyBlock(_propertyBlock);
    }

    private void Destroy()
    {
        Destroyed?.Invoke();
        Destroy(gameObject);
    }
}