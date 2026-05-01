using System.Linq;
using Unity.Mathematics;
using UnityEngine;

public class PaintShooter : MonoBehaviour
{
    public int WallDamage = 1;
    public Paint Paint;
    public Bullet Bullet;

    public void TryShoot(Vector3 dir, float scale = 1f)
    {
        if (Paint == null || Bullet == null)
        {
            Debug.LogWarning($"PaintShooter on {name} cannot shoot because prefab is not assigned.", this);
            return;
        }

        if (Physics.Raycast(transform.position, dir, out var hit, 300f, 1))
        {
            GameObject hitGameObject = hit.collider.gameObject;
            if (hitGameObject.TryGetComponent<Paint>(out var p))
            {
                return;
            }
            
            if (hitGameObject.TryGetComponent<Wall>(out var wall))
            {
                var bullet = Instantiate(Bullet, transform.position + dir * 2, Quaternion.identity);
                bullet.transform.localScale *= scale;
                bullet.Lauch(dir);
                Vector3 pos = transform.position;
                bullet.OnHit += (collision) =>
                {
                    var hitGameObject = collision.collider.gameObject;

                    if (!hitGameObject.TryGetComponent<Wall>(out var wall))
                        return;

                    ContactPoint bestContact = collision.contacts[0];
                    float bestDot = -Mathf.Infinity;

                    foreach (var contact in collision.contacts)
                    {
                        float dot = Vector3.Dot(contact.normal, -bullet.Direction);

                        if (dot > bestDot)
                        {
                            bestDot = dot;
                            bestContact = contact;
                        }
                    }

                    var paint = Instantiate(Paint, bestContact.point, Quaternion.LookRotation(bestContact.normal) * Quaternion.LookRotation(Vector3.up));
                    paint.transform.localScale *= scale;
                    paint.AttachTo(wall);

                    wall.SetDamageColor(
                        paint.GetComponent<MeshRenderer>()?.sharedMaterial?.GetColor("_BaseColor") ?? Color.red
                    );
                    int areaDamage = Mathf.RoundToInt(WallDamage * scale * scale);
                    wall.Damage(areaDamage);
                };
            }
        }
    }

    public void ShootAllDirections(int count = 100, float scale = 1f)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 randomDir = UnityEngine.Random.onUnitSphere;
            TryShoot(randomDir, scale);
        }
    }
}
