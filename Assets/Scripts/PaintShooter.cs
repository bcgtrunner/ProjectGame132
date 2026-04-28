using Unity.Mathematics;
using UnityEngine;

public class PaintShooter : MonoBehaviour
{
    public int WallDamage = 1;
    public GameObject Paint;

    public void TryShoot(Vector3 dir)
    {
        if (Physics.Raycast(transform.position, dir, out var hit, 300f, 1))
        {
            GameObject hitGameObject = hit.collider.gameObject;
            if (hitGameObject.TryGetComponent<Paint>(out var p))
            {
                return;
            }
            
            if (hitGameObject.TryGetComponent<Wall>(out var wall))
            {
                var paintObject = Instantiate(Paint, hit.point, Quaternion.LookRotation(hit.normal) * Quaternion.LookRotation(Vector3.up));
                paintObject.GetComponent<Paint>().AttachTo(wall);
                wall.Damage(WallDamage);
            }
        }
    }
}
