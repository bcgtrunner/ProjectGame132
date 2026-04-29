using System;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public Rigidbody rb;
    public float Speed;
    private Vector3 direction = Vector3.zero;

    public Action OnHit;

    public void Lauch(Vector3 dir)
    {
        direction = dir;
    }

    private void FixedUpdate()
    {
        if (direction != Vector3.zero)
        {
            rb.position += Speed * direction;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log(collision.collider.gameObject.name);
        OnHit?.Invoke();
        Destroy(gameObject);
    }
}