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
    [SerializeField] private float _maxAngularSpeed = 90f;
    [SerializeField] private float _maxAngularAcceleration = 400f;
    [SerializeField] private float _maxJerk = 3000f;
    [SerializeField] private float _targetChangeInterval = 2.5f;
    [SerializeField] [Range(0f, 1f)] private float _playerBias = 0.4f;

    private enum Mode { Idle, Sweep }
    private Mode _mode = Mode.Idle;
    private float _modeTimer;

    private PaintShooter _shooter;
    private PlayerController _controller;
    private AIInput _aiInput;

    private Vector3 _sweepDirection;
    private Vector3 _angularVelocity;
    private Vector3 _angularAcceleration;
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
        // Bias hemisphere toward player; fall back to surface normal; null = omnidirectional
        Vector3? hemisphere = null;
        PlayerController target = _aiInput != null ? _aiInput.Target : null;
        if (target != null)
            hemisphere = (target.transform.position - transform.position).normalized;
        else if (_controller != null && _controller.IsAttached)
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

        Vector3 toTarget = Vector3.ProjectOnPlane(_targetDirection, _sweepDirection);
        Vector3 desiredVelocity = toTarget.sqrMagnitude > Mathf.Epsilon
            ? toTarget.normalized * _maxAngularSpeed
            : Vector3.zero;

        Vector3 desiredAcceleration = desiredVelocity - _angularVelocity;
        if (desiredAcceleration.magnitude > _maxAngularAcceleration)
            desiredAcceleration = desiredAcceleration.normalized * _maxAngularAcceleration;

        Vector3 jerk = desiredAcceleration - _angularAcceleration;
        float maxJerkStep = _maxJerk * Time.deltaTime;
        if (jerk.magnitude > maxJerkStep)
            jerk = jerk.normalized * maxJerkStep;

        _angularAcceleration += jerk;
        if (_angularAcceleration.magnitude > _maxAngularAcceleration)
            _angularAcceleration = _angularAcceleration.normalized * _maxAngularAcceleration;

        _angularVelocity += _angularAcceleration * Time.deltaTime;
        if (_angularVelocity.magnitude > _maxAngularSpeed)
            _angularVelocity = _angularVelocity.normalized * _maxAngularSpeed;

        float angleDelta = _angularVelocity.magnitude * Time.deltaTime;
        if (angleDelta > Mathf.Epsilon)
        {
            _sweepDirection = Quaternion.AngleAxis(angleDelta, _angularVelocity.normalized) * _sweepDirection;
            _sweepDirection.Normalize();
            _angularVelocity = Vector3.ProjectOnPlane(_angularVelocity, _sweepDirection);
            _angularAcceleration = Vector3.ProjectOnPlane(_angularAcceleration, _sweepDirection);
        }

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
