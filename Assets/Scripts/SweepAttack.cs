using UnityEngine;

public class SweepAttack : MonoBehaviour
{
    [SerializeField] private float _coneHalfAngle = 15f;
    [SerializeField] private int _shotsPerFrame = 5;
    [SerializeField] private float _scale = 1f;
    [SerializeField] private float _maxAngularSpeed = 120f;   // degrees/sec
    [SerializeField] private float _angularAcceleration = 200f; // degrees/sec²
    [SerializeField] private float _arrivalAngle = 8f;

    private PaintShooter _shooter;
    private Vector3 _direction;
    private Vector3 _angularVelocity; // tangent to sphere, magnitude = degrees/sec
    private Vector3 _targetDirection;

    private void Awake()
    {
        _shooter = GetComponent<PaintShooter>();
        _direction = Random.onUnitSphere;
        _targetDirection = Random.onUnitSphere;
    }

    private void Update()
    {
        if (Vector3.Angle(_direction, _targetDirection) < _arrivalAngle)
            _targetDirection = Random.onUnitSphere;

        // Steer angular velocity toward target (in tangent plane of _direction)
        Vector3 toTarget = Vector3.ProjectOnPlane(_targetDirection, _direction);
        if (toTarget.sqrMagnitude > Mathf.Epsilon)
        {
            Vector3 desiredVelocity = toTarget.normalized * _maxAngularSpeed;
            Vector3 steering = desiredVelocity - _angularVelocity;
            float maxDelta = _angularAcceleration * Time.deltaTime;
            if (steering.magnitude > maxDelta)
                steering = steering.normalized * maxDelta;
            _angularVelocity += steering;
        }

        float speed = _angularVelocity.magnitude;
        if (speed > _maxAngularSpeed)
            _angularVelocity = _angularVelocity.normalized * _maxAngularSpeed;

        // Rotate direction by angular velocity
        float angleDelta = speed * Time.deltaTime;
        if (angleDelta > Mathf.Epsilon)
        {
            _direction = Quaternion.AngleAxis(angleDelta, _angularVelocity.normalized) * _direction;
            _direction.Normalize();
            // Keep angular velocity in the tangent plane after direction rotates
            _angularVelocity = Vector3.ProjectOnPlane(_angularVelocity, _direction);
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
