using Unity.Mathematics;
using UnityEngine;

public class PaintShooter : MonoBehaviour
{
    public int WallDamage = 1;
    public Paint Paint;
    public Bullet Bullet;

    public void TryShoot(Vector3 dir)
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
                bullet.Lauch(dir);
                bullet.OnHit += () =>
                {
                    var paint = Instantiate(Paint, hit.point, Quaternion.LookRotation(hit.normal) * Quaternion.LookRotation(Vector3.up));
                    paint.AttachTo(wall);
                    wall.SetDamageColor(paint.GetComponent<MeshRenderer>()?.sharedMaterial?.GetColor("_BaseColor") ?? Color.red);
                    wall.Damage(WallDamage);
                };
            }
        }
    }
}
