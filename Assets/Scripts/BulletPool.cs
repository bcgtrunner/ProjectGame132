using UnityEngine;
using UnityEngine.Pool;

public class BulletPool : MonoBehaviour
{
    public static BulletPool Instance { get; private set; }

    [SerializeField] private Bullet _bulletPrefab;

    private ObjectPool<Bullet> _pool;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        _pool = new ObjectPool<Bullet>(
            createFunc: () => Instantiate(_bulletPrefab),
            actionOnGet: (bullet) => bullet.gameObject.SetActive(true),
            actionOnRelease: (bullet) =>
            {
                bullet.gameObject.SetActive(false);
                bullet.ResetState();
            },
            actionOnDestroy: (bullet) => Destroy(bullet.gameObject),
            collectionCheck: false,
            defaultCapacity: 50,
            maxSize: 500
        );
    }

    public void SetPrefab(Bullet prefab)
    {
        _bulletPrefab = prefab;
    }

    public Bullet Get(Vector3 position, Quaternion rotation)
    {
        if (_bulletPrefab == null)
        {
            Debug.LogWarning("BulletPool missing prefab.");
            return null;
        }
        var bullet = _pool.Get();
        bullet.transform.position = position;
        bullet.transform.rotation = rotation;
        return bullet;
    }

    public void Release(Bullet bullet)
    {
        if (_pool != null)
        {
            _pool.Release(bullet);
        }
        else
        {
            Destroy(bullet.gameObject);
        }
    }
}
