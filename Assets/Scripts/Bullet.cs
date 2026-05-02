using System;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public Rigidbody rb;
    public float Speed;
    public Vector3 Direction = Vector3.zero;

    public Action<Collision> OnHit;

    public void Lauch(Vector3 dir, float speed = 0f)
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

    private void OnCollisionEnter(Collision collision)
    {
        OnHit?.Invoke(collision);
        Destroy(gameObject);
    }
}