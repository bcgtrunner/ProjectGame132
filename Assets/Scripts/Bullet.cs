using System;
using System.Collections;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public Rigidbody rb;
    public float Speed;
    public Vector3 Direction = Vector3.zero;

    public Action<Collision> OnHit;

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
        StartCoroutine(Expansion(scale));
    }

    private IEnumerator Expansion(float scale)
    {
        float timeToExpand = 0.1f;
        float t = 0f;
        Debug.Log(scale);
        Vector3 initialScale = transform.localScale;
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

    private void OnCollisionEnter(Collision collision)
    {
        OnHit?.Invoke(collision);
        Destroy(gameObject);
    }
}