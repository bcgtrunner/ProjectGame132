using Unity.Mathematics;
using UnityEngine;

public class PaintShooter : MonoBehaviour
{
    public GameObject Paint;

    public void TryShoot(Vector3 dir)
    {
        if (Physics.Raycast(transform.position, dir, out var hit, 300f, 1))
        {
            if (hit.collider.gameObject.TryGetComponent<Paint>(out var p)) return;
            Instantiate(Paint, hit.point, Quaternion.LookRotation(hit.normal));
        }
    }
}
