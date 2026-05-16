using UnityEngine;
using System.Collections;

[RequireComponent(typeof(PlayerController))]
public class AIInput : MonoBehaviour
{
    [Header("Timings & Shooting")]
    [SerializeField] private float _minWaitTime = 0.5f;
    [SerializeField] private float _maxWaitTime = 2.0f;
    [SerializeField] private float _minShootDelay = 2.0f;
    [SerializeField] private float _maxShootDelay = 2.0f;
    [SerializeField] private bool _shootAttached = false;
    [SerializeField] private float _shootAccuracy = 30f;
    [SerializeField] private float _launchAimJitter = 0.1f;
    [SerializeField] private bool _emitShrapnel = false;
    [SerializeField] private int _shrapnelShotsPerFrame = 10;
    [SerializeField] private float _shrapnelScale = 0.3f;

    [Header("Movement & AI Behavior")]
    [Tooltip("��������� ������ ��� ����� ������������ � ����. ��� ���� ��������, ��� ������������ ��� ���� �����, ����������� ����� � ������.")]
    [SerializeField] private float _followStrength = 1.0f;
    [Tooltip("������������ ���������, �� ������� ��� ����� ������������� �� ���� ������.")]
    [SerializeField] private float _maxTravelDistance = 10f;
    [Tooltip("����������� ���������� HP �����, �� ������� ��� ���������� ��������.")]
    [SerializeField] private float _minWallHpThreshold = 2.0f;

    private PlayerController _controller;
    private PaintShooter _shooter;
    private bool isLaunching = false;
    private bool isShooting = false;
    private PlayerController _target;
    public PlayerController Target
    {
        get
        {
            if (_target == null && GameManager.Instance != null)
                return GameManager.Instance.Player;
            return _target;
        }
        set => _target = value;
    }
    public event System.Action<AIInput> Destroyed;

    private void Awake()
    {
        _controller = GetComponent<PlayerController>();
        _shooter = GetComponent<PaintShooter>();
        if (_emitShrapnel)
            StartCoroutine(ShrapnelLoop());
    }

    private IEnumerator ShrapnelLoop()
    {
        while (true)
        {
            Vector3? normal = _controller.HasSurfaceNormal ? _controller.CurrentSurfaceNormal : (Vector3?)null;
            _shooter.ShootDeathBurst(_shrapnelShotsPerFrame, _shrapnelScale, normal);
            yield return null;
        }
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

    public void SetVirtualAttachment(Vector3 surfaceNormal)
    {
        _controller.SetVirtualAttachment(surfaceNormal);
    }

    private void Update()
    {
        if (_controller.IsAttached && !isLaunching && !isShooting)
        {
            StartCoroutine(WaitAndLaunch());
        }
        if ((_controller.IsAttached || !_shootAttached) && !isShooting)
        {
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
            _controller.TryLaunch(GetSmartLaunchDirection());
        }
        isLaunching = false;
    }

    private IEnumerator WaitAndShoot()
    {
        isShooting = true;
        float waitTime = Random.Range(_minShootDelay, _maxShootDelay);
        yield return new WaitForSeconds(waitTime);

        while (Target != null)
        {
            Vector3 randomDir = Random.onUnitSphere;
            if (Vector3.Angle(randomDir, Target.transform.position - transform.position) < _shootAccuracy)
            {
                _shooter.TryShoot(randomDir);
                break;
            }
            yield return null;
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

    private Vector3 GetSmartLaunchDirection()
    {
        int samples = 20;
        Vector3 bestDir = Vector3.zero;
        float bestScore = float.MinValue;

        for (int i = 0; i < samples; i++)
        {
            Vector3 dir = Random.onUnitSphere;

            // ���������� ������������� ���������� _maxTravelDistance ������ 10f
            if (Physics.Raycast(transform.position, dir, out RaycastHit hit, _maxTravelDistance))
            {
                // ��������: �� ������������ �� ����� � ������ HP
                Wall wall = hit.collider.GetComponent<Wall>();
                if (wall != null)
                {
                    // �����: �������� .CurrentHp �� ���������� �������� �������� � ����� ������� Wall
                    if (wall.Health < _minWallHpThreshold)
                    {
                        continue; // ���������� ��� �����, ���� � �� ���� ��������
                    }
                }

                float score = 0;
                if (Target != null)
                {
                    float dist = Vector3.Distance(hit.point, Target.transform.position);
                    // �������� ��������� �� ���� ����������
                    score -= dist * _followStrength;
                }

                score += hit.distance;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestDir = dir;
                }
            }
        }

        return bestDir != Vector3.zero ? bestDir : Random.onUnitSphere;
    }

    Vector3 PredictTargetPosition()
    {
        Rigidbody targetRb = Target.GetComponent<Rigidbody>();
        if (targetRb == null) return Target.transform.position;

        float predictionTime = 0.5f;
        return Target.transform.position + targetRb.linearVelocity * predictionTime;
    }
}