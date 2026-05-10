using UnityEngine;

public class PaintShooter : MonoBehaviour
{
    public float BulletSpeed = 0f;
    public float PlayerDamage = 0.5f;
    public int WallDamage = 1;
    public Paint Paint;
    public Bullet Bullet;

    private Collider _collider;

    private void Awake()
    {
        _collider = GetComponent<Collider>();
    }

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
            if (hitGameObject.TryGetComponent<Paint>(out _))
            {
                return;
            }

            {
                float spawnOffset = _collider != null
                    ? _collider.bounds.extents.magnitude + 0.5f
                    : 2f;
                spawnOffset = Mathf.Max(spawnOffset, 2f);
                var bullet = Instantiate(Bullet, transform.position + dir * spawnOffset, Quaternion.identity);
                bullet.transform.localScale *= scale;
                bullet.Launch(dir, BulletSpeed);
                bullet.OnHit += (collision) =>
                {
                    var collisionObject = collision.collider.gameObject;

                    if (collisionObject.TryGetComponent<PlayerController>(out var player))
                    {
                        player.TakeDamage(PlayerDamage * scale * scale);
                    }

                    if (!collisionObject.TryGetComponent<Wall>(out var hitWall))
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

                    var paintRenderer = Paint.GetComponent<MeshRenderer>();
                    Color paintColor = paintRenderer != null && paintRenderer.sharedMaterial != null
                        ? paintRenderer.sharedMaterial.GetColor("_BaseColor")
                        : Color.red;

                    var nm = Unity.Netcode.NetworkManager.Singleton;
                    var localPlayer = nm != null ? nm.LocalClient?.PlayerObject : null;
                    if (nm != null && nm.IsListening && localPlayer != null && localPlayer.TryGetComponent<NetworkPlayerSetup>(out var localNet))
                    {
                        localNet.RequestSpawnPaintServerRpc(bestContact.point, bestContact.normal, scale, paintColor);
                    }
                    else
                    {
                        SpawnPaintAt(bestContact.point, bestContact.normal, scale, paintColor);
                    }

                    float areaDamage = WallDamage * scale * scale;
                    hitWall.Damage(areaDamage);
                };
            }
        }
    }

    public void SpawnPaintAt(Vector3 point, Vector3 normal, float scale, Color color)
    {
        if (Paint == null) return;

        var paint = Instantiate(Paint, point, Quaternion.LookRotation(normal) * Quaternion.LookRotation(Vector3.up));
        paint.transform.localScale *= scale;

        Wall wall = FindWallAt(point, normal);
        if (wall != null)
        {
            paint.AttachTo(wall);
            wall.SetDamageColor(color);
        }
    }

    private static Wall FindWallAt(Vector3 point, Vector3 normal)
    {
        if (Physics.Raycast(point + normal * 0.1f, -normal, out var hit, 0.5f, Physics.AllLayers, QueryTriggerInteraction.Ignore))
            return hit.collider.GetComponent<Wall>();
        return null;
    }

    public void ShootAllDirections(int count = 100, float scale = 1f)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 randomDir = UnityEngine.Random.onUnitSphere;
            TryShoot(randomDir, scale);
        }
    }

    public void ShootDeathBurst(int count, float scale, Vector3? surfaceNormal = null)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 dir = surfaceNormal.HasValue
                ? RandomInCone(surfaceNormal.Value, 80f)
                : Random.onUnitSphere;
            TryShoot(dir, scale);
        }
    }

    private static Vector3 RandomInCone(Vector3 normal, float halfAngleDeg)
    {
        float halfRad = halfAngleDeg * Mathf.Deg2Rad;
        float z = Random.Range(Mathf.Cos(halfRad), 1f);
        float theta = Random.Range(0f, 2f * Mathf.PI);
        float r = Mathf.Sqrt(1f - z * z);
        Vector3 local = new Vector3(r * Mathf.Cos(theta), r * Mathf.Sin(theta), z);
        return Quaternion.FromToRotation(Vector3.forward, normal) * local;
    }
}