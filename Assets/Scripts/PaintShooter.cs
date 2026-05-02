using System.Linq;
using Unity.Mathematics;
using UnityEngine;

public class PaintShooter : MonoBehaviour
{
    public float BulletSpeed = 0f;
    public float PlayerDamage = 0.5f;
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
                bullet.Lauch(dir, BulletSpeed);
                Vector3 pos = transform.position;
                bullet.OnHit += (collision) =>
                {
                    var hitGameObject = collision.collider.gameObject;

                    if (hitGameObject.TryGetComponent<PlayerController>(out var player))
                    {
                        player.TakeDamage(PlayerDamage * scale * scale);
                    }

                    if (!hitGameObject.TryGetComponent<Wall>(out var wall))
                    {
                        return;
                    }

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
                    float areaDamage = WallDamage * scale * scale;
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

    /// <summary>
    /// Fires paint bursts in directions appropriate for a death explosion.
    /// If attached to a wall (surfaceNormal provided), only fires into the
    /// outward-facing hemisphere so paint lands on the player's side.
    /// If flying (surfaceNormal is null), fires in all directions.
    /// </summary>
    public void ShootDeathBurst(int count, float scale, Vector3? surfaceNormal = null)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 randomDir = UnityEngine.Random.onUnitSphere;

            // If attached, constrain to the hemisphere facing away from the wall
            if (surfaceNormal.HasValue && Vector3.Dot(randomDir, surfaceNormal.Value) < 0f)
            {
                randomDir = -randomDir;
            }

            TryShoot(randomDir, scale);
        }
    }
}