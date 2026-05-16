using System;
using System.Collections;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public Rigidbody rb;
    public float Speed;
    public Vector3 Direction = Vector3.zero;
    public float CurrentScale { get; private set; } = 1f;

    public Action<Collision> OnHit;
    private bool _consumed;

    public void ResetState()
    {
        StopAllCoroutines();
        Direction = Vector3.zero;
        Speed = 0f;
        CurrentScale = 1f;
        transform.localScale = Vector3.one;
        _consumed = false;
        OnHit = null;
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    public void Launch(Vector3 dir, float speed = 0f)
    {
        Direction = dir;
        Speed = speed != 0 ? speed : Speed;
    }

    private void FixedUpdate()
    {
        if (Direction != Vector3.zero)
        {
            rb.position += Speed * Direction * Time.fixedDeltaTime;
        }
    }

    public void Expand(float scale)
    {
        CurrentScale = scale;
        StartCoroutine(Expansion(scale));
    }

    private IEnumerator Expansion(float scale)
    {
        float timeToExpand = 0.25f;
        float t = 0f;
        Vector3 initialScale = transform.localScale * 0.5f;
        transform.localScale = initialScale;
        Vector3 targetScale = initialScale * scale;

        while (t < timeToExpand)
        {
            t += Time.deltaTime;

            float progress = t / timeToExpand;

            transform.localScale = Vector3.Lerp(initialScale, targetScale, progress);

            yield return null;
        }

        transform.localScale = targetScale;
    }

    public void Resize(float newScale)
    {
        if (newScale <= 0f || CurrentScale <= 0f) return;
        StopAllCoroutines();
        float ratio = newScale / CurrentScale;
        transform.localScale *= ratio;
        CurrentScale = newScale;
    }

    public void Pierce()
    {
        _consumed = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        _consumed = true;
        OnHit?.Invoke(collision);
        if (_consumed)
        {
            if (BulletPool.Instance != null)
                BulletPool.Instance.Release(this);
            else
                Destroy(gameObject);
        }
    }
}