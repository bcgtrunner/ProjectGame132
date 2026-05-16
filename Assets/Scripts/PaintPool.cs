using UnityEngine;
using UnityEngine.Pool;

public class PaintPool : MonoBehaviour
{
    public static PaintPool Instance { get; private set; }

    [SerializeField] private Paint _paintPrefab;

    private ObjectPool<Paint> _pool;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        _pool = new ObjectPool<Paint>(
            createFunc: () => Instantiate(_paintPrefab),
            actionOnGet: (paint) => paint.gameObject.SetActive(true),
            actionOnRelease: (paint) =>
            {
                paint.gameObject.SetActive(false);
                paint.ResetState();
            },
            actionOnDestroy: (paint) => Destroy(paint.gameObject),
            collectionCheck: false,
            defaultCapacity: 100,
            maxSize: 2000
        );
    }

    public void SetPrefab(Paint prefab)
    {
        _paintPrefab = prefab;
    }

    public Paint Get(Vector3 position, Quaternion rotation)
    {
        if (_paintPrefab == null)
        {
            Debug.LogWarning("PaintPool missing prefab.");
            return null;
        }
        var paint = _pool.Get();
        paint.transform.position = position;
        paint.transform.rotation = rotation;
        return paint;
    }

    public void Release(Paint paint)
    {
        if (_pool != null)
        {
            _pool.Release(paint);
        }
        else
        {
            Destroy(paint.gameObject);
        }
    }
}
