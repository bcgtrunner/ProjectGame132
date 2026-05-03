using UnityEngine;

public class SweepAttack : MonoBehaviour
{
    [SerializeField] private float _coneHalfAngle = 15f;
    [SerializeField] private int _shotsPerFrame = 5;
    [SerializeField] private float _scale = 1f;
    [SerializeField] private float _maxAngularSpeed = 90f;        // deg/sec
    [SerializeField] private float _maxAngularAcceleration = 400f; // deg/sec²
    [SerializeField] private float _maxJerk = 3000f;               // deg/sec³
    [SerializeField] private float _targetChangeInterval = 2.5f;   // seconds between new targets

    private PaintShooter _shooter;
    private Vector3 _direction;
    private Vector3 _angularVelocity;
    private Vector3 _angularAcceleration;
    private Vector3 _targetDirection;
    private float _targetTimer;

    private void Awake()
    {
        _shooter = GetComponent<PaintShooter>();
        _direction = Random.onUnitSphere;
        _targetDirection = Random.onUnitSphere;
        _targetTimer = _targetChangeInterval;
    }

    private void Update()
    {
        _targetTimer -= Time.deltaTime;
        if (_targetTimer <= 0f)
        {
            _targetDirection = Random.onUnitSphere;
            _targetTimer = Random.Range(_targetChangeInterval * 0.7f, _targetChangeInterval * 1.3f);
        }

        // Desired velocity: max speed in the tangent-plane direction toward target
        Vector3 toTarget = Vector3.ProjectOnPlane(_targetDirection, _direction);
        Vector3 desiredVelocity = toTarget.sqrMagnitude > Mathf.Epsilon
            ? toTarget.normalized * _maxAngularSpeed
            : Vector3.zero;

        // Desired acceleration: steer velocity toward desired
        Vector3 desiredAcceleration = desiredVelocity - _angularVelocity;
        if (desiredAcceleration.magnitude > _maxAngularAcceleration)
            desiredAcceleration = desiredAcceleration.normalized * _maxAngularAcceleration;

        // Jerk: steer acceleration toward desired — this is what gives C2
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

        // Rotate direction and keep both derivatives in the tangent plane
        float angleDelta = _angularVelocity.magnitude * Time.deltaTime;
        if (angleDelta > Mathf.Epsilon)
        {
            _direction = Quaternion.AngleAxis(angleDelta, _angularVelocity.normalized) * _direction;
            _direction.Normalize();
            _angularVelocity = Vector3.ProjectOnPlane(_angularVelocity, _direction);
            _angularAcceleration = Vector3.ProjectOnPlane(_angularAcceleration, _direction);
        }

        for (int i = 0; i < _shotsPerFrame; i++)
            _shooter.TryShoot(RandomInCone(_direction, _coneHalfAngle), _scale);
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
