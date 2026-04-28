using UnityEngine;
using System.Collections;

[RequireComponent(typeof(PlayerController))]
public class AIInput : MonoBehaviour
{
    [SerializeField] private float _minWaitTime = 0.5f;
    [SerializeField] private float _maxWaitTime = 2.0f;

    private PlayerController _controller;
    private PaintShooter _shooter;
    private bool isLaunching = false;
    private bool isShooting = false;
    public PlayerController Target;

    private void Awake()
    {
        _controller = GetComponent<PlayerController>();
        _shooter = GetComponent<PaintShooter>();
    }

    private void Update()
    {
        if (_controller.IsAttached && !isLaunching && !isShooting)
        {
            StartCoroutine(WaitAndLaunch());
            StartCoroutine(WaitAndShoot());
        }
    }

    private IEnumerator WaitAndLaunch()
    {
        isLaunching = true;
        float waitTime = Random.Range(_minWaitTime, _maxWaitTime);
        yield return new WaitForSeconds(waitTime);

        if (_controller.IsAttached)
        {
            Vector3 randomDir = Random.onUnitSphere;

            _controller.TryLaunch(randomDir);
        }
        isLaunching = false;
    }

    private IEnumerator WaitAndShoot()
    {
        isShooting = true;
        float waitTime = Random.Range(_minWaitTime, _maxWaitTime) * 5;
        yield return new WaitForSeconds(waitTime);

        if (_controller.IsAttached)
        {
            Vector3 randomDir = Random.onUnitSphere;
            if (Vector3.Dot(randomDir, Target.transform.position - transform.position) > 0)
            {
                _shooter.TryShoot(randomDir);
            }
        }
        isShooting = false;
    }
}
