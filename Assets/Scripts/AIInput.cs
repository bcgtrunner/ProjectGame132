using UnityEngine;
using System.Collections;

[RequireComponent(typeof(PlayerController))]
public class AIInput : MonoBehaviour
{
    [SerializeField] private float _minWaitTime = 0.5f;
    [SerializeField] private float _maxWaitTime = 2.0f;
    [SerializeField] private float _launchAimJitter = 0.35f;

    private PlayerController _controller;
    private PaintShooter _shooter;
    private bool isLaunching = false;
    private bool isShooting = false;
    public PlayerController Target;
    public event System.Action<AIInput> Destroyed;

    private void Awake()
    {
        _controller = GetComponent<PlayerController>();
        _shooter = GetComponent<PaintShooter>();
    }

    public void LaunchImmediately()
    {
        if (!_controller.IsAttached)
        {
            return;
        }

        Vector3 launchDirection = GetImmediateLaunchDirection();
        _controller.TryLaunch(launchDirection);
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
            _controller.TryLaunch(GetImmediateLaunchDirection());
        }
        isLaunching = false;
    }

    private IEnumerator WaitAndShoot()
    {
        isShooting = true;
        float waitTime = Random.Range(_minWaitTime, _maxWaitTime) * 5;
        yield return new WaitForSeconds(waitTime);

        if (_controller.IsAttached && Target != null)
        {
            Vector3 randomDir = Random.onUnitSphere;
            if (Vector3.Dot(randomDir, Target.transform.position - transform.position) > 0)
            {
                _shooter.TryShoot(randomDir);
            }
        }
        isShooting = false;
    }

    private void OnDestroy()
    {
        Destroyed?.Invoke(this);
    }

    private Vector3 GetImmediateLaunchDirection()
    {
        if (Target != null)
        {
            Vector3 towardTarget = (Target.transform.position - transform.position).normalized;
            if (towardTarget.sqrMagnitude > Mathf.Epsilon)
            {
                Vector3 randomOffset = Random.onUnitSphere * _launchAimJitter;
                Vector3 biasedDirection = (towardTarget + randomOffset).normalized;
                if (biasedDirection.sqrMagnitude > Mathf.Epsilon)
                {
                    return biasedDirection;
                }

                return towardTarget;
            }
        }

        return Random.onUnitSphere;
    }
}
