using UnityEngine;

public class SweepAttack : MonoBehaviour
{
    [Header("Mode timing")]
    [SerializeField] private float _idleDuration = 5f;
    [SerializeField] private float _activeDuration = 8f;

    [Header("Idle (shrapnel)")]
    [SerializeField] private int _shrapnelShotsPerFrame = 1;
    [SerializeField] private float _shrapnelScale = 0.6f;

    [Header("Active (sweep)")]
    [SerializeField] private float _coneHalfAngle = 15f;
    [SerializeField] private int _sweepShotsPerFrame = 5;
    [SerializeField] private float _sweepScale = 1f;
    [SerializeField] private float _rotationSpeed = 60f;
    [SerializeField] private float _targetChangeInterval = 2.5f;
    [SerializeField] [Range(0f, 1f)] private float _playerBias = 0.7f;

    private enum Mode { Idle, Sweep }
    private Mode _mode = Mode.Idle;
    private float _modeTimer;

    private PaintShooter _shooter;
    private PlayerController _controller;
    private AIInput _aiInput;

    private Vector3 _sweepDirection;
    private Vector3 _targetDirection;
    private float _targetTimer;

    private void Awake()
    {
        _shooter = GetComponent<PaintShooter>();
        _controller = GetComponent<PlayerController>();
        _aiInput = GetComponent<AIInput>();
        _sweepDirection = Random.onUnitSphere;
        _modeTimer = _idleDuration;
        _targetTimer = _targetChangeInterval;
    }

    private void Update()
    {
        _modeTimer -= Time.deltaTime;
        if (_modeTimer <= 0f)
        {
            _mode = _mode == Mode.Idle ? Mode.Sweep : Mode.Idle;
            _modeTimer = _mode == Mode.Idle ? _idleDuration : _activeDuration;
            if (_mode == Mode.Sweep)
                _targetDirection = PickTarget();
        }

        if (_mode == Mode.Idle)
            UpdateIdle();
        else
            UpdateSweep();
    }

    private void UpdateIdle()
    {
        Vector3? hemisphere = null;
        PlayerController target = _aiInput != null ? _aiInput.Target : null;
        if (target != null)
            hemisphere = (target.transform.position - transform.position).normalized;
        else if (_controller != null && _controller.HasSurfaceNormal)
            hemisphere = _controller.CurrentSurfaceNormal;

        _shooter.ShootDeathBurst(_shrapnelShotsPerFrame, _shrapnelScale, hemisphere);
    }

    private void UpdateSweep()
    {
        _targetTimer -= Time.deltaTime;
        if (_targetTimer <= 0f)
        {
            _targetDirection = PickTarget();
            _targetTimer = Random.Range(_targetChangeInterval * 0.7f, _targetChangeInterval * 1.3f);
        }

        // Smoothly rotate sweep direction toward target at a fixed rotation speed
        _sweepDirection = Vector3.RotateTowards(
            _sweepDirection,
            _targetDirection,
            _rotationSpeed * Mathf.Deg2Rad * Time.deltaTime,
            1f
        );

        for (int i = 0; i < _sweepShotsPerFrame; i++)
            _shooter.TryShoot(RandomInCone(_sweepDirection, _coneHalfAngle), _sweepScale);
    }

    private Vector3 PickTarget()
    {
        Vector3 random = Random.onUnitSphere;
        PlayerController target = _aiInput != null ? _aiInput.Target : null;
        if (target == null || _playerBias <= 0f)
            return random;
        Vector3 toPlayer = (target.transform.position - transform.position).normalized;
        return Vector3.Slerp(random, toPlayer, _playerBias).normalized;
    }

    private static Vector3 RandomInCone(Vector3 direction, float halfAngleDegrees)
    {
        float halfRad = halfAngleDegrees * Mathf.Deg2Rad;
        float z = Random.Range(Mathf.Cos(halfRad), 1f);
        float theta = Random.Range(0f, 2f * Mathf.PI);
        float r = Mathf.Sqrt(1f - z * z);
        Vector3 local = new Vector3(r * Mathf.Cos(theta), r * Mathf.Sin(theta), z);
        return Quaternion.FromToRotation(Vector3.forward, direction) * local;
    }
}