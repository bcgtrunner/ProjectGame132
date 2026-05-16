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
                Vector3 spawnPos = transform.position + dir * spawnOffset;
                var bullet = Instantiate(Bullet, spawnPos, Quaternion.identity);
                bullet.Expand(scale);
                bullet.Launch(dir, BulletSpeed);

                if (IsLocalNetworkedPlayer(out var localNet))
                {
                    localNet.RequestSpawnBulletServerRpc(spawnPos, dir, scale, BulletSpeed);
                }

                if (TryGetComponent<PlayerController>(out var shooterController))
                {
                    float effectiveSpeed = BulletSpeed != 0f ? BulletSpeed : Bullet.Speed;
                    shooterController.ApplyRecoil(dir, effectiveSpeed, scale);
                }
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

                    float areaDamage = WallDamage * scale * scale;
                    if (IsLocalNetworkedPlayer(out var localNetForPaint))
                    {
                        localNetForPaint.RequestSpawnPaintAndDamageWallServerRpc(bestContact.point, bestContact.normal, scale, paintColor, hitWall.GridPos, (int)hitWall.Direction, areaDamage);
                    }
                    else
                    {
                        SpawnPaintAt(bestContact.point, bestContact.normal, scale, paintColor);
                        hitWall.DamageAt(bestContact.point, areaDamage);
                    }
                };
            }
        }
    }

    public void SpawnVisualBullet(Vector3 point, Vector3 dir, float scale, float speed)
    {
        if (Bullet == null) return;
        var bullet = Instantiate(Bullet, point, Quaternion.identity);
        bullet.transform.localScale *= scale;
        bullet.Launch(dir, speed);
    }

    private bool IsLocalNetworkedPlayer(out NetworkPlayerSetup localNet)
    {
        localNet = null;
        var nm = Unity.Netcode.NetworkManager.Singleton;
        if (nm == null || !nm.IsListening) return false;
        var localPlayer = nm.LocalClient?.PlayerObject;
        if (localPlayer == null || localPlayer.gameObject != gameObject) return false;
        return localPlayer.TryGetComponent(out localNet);
    }

    public Paint SpawnPaintAt(Vector3 point, Vector3 normal, float scale, Color color)
    {
        var paint = Instantiate(Paint, point, Quaternion.LookRotation(normal) * Quaternion.LookRotation(Vector3.up));
        paint.transform.localScale *= scale;
        Wall wall = FindWallAt(point, normal);
        if (wall != null)
        {
            paint.AttachTo(wall);
            wall.SetDamageColor(color);
        }
        else
        {
            Destroy(paint.gameObject);
        }
        return paint;
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