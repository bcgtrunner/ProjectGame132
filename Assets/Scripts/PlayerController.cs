using UnityEngine;
using System;

public class Cooldown
{
    private readonly float _duration;
    private float _lastReset;

    public Cooldown(float duration)
    {
        _duration = duration;
        _lastReset = -duration;
    }

    public void Reset()
    {
        _lastReset = Time.time;
    }

    public bool Over()
    {
        return Time.time - _lastReset >= _duration;
    }
}

public class PlayerController : MonoBehaviour
{
    [SerializeField] private Vector3 _wallTouchingSideLocalNormal = Vector3.down;
    [SerializeField] private float _flySpeed = 10f;
    [SerializeField] private float _launchClearance = 0.1f;

    private enum PlayerState { Attached, Flying }

    private Collider _playerCollider;
    private PlayerState _state = PlayerState.Attached;
    private Vector3 _flyingDirection;
    private Collider _attachedWallCollider;

    public event Action<Vector3> AttachedToWall;
    public Vector3 CurrentSurfaceNormal { get; private set; } = Vector3.up;
    public bool IsTakeoffOnCooldown => !_takeoffCooldown.Over();
    public bool IsAttached => _state == PlayerState.Attached;

    private Cooldown _takeoffCooldown = new(1f);

    private void Awake()
    {
        _playerCollider = GetComponent<Collider>();
    }

    private void Update()
    {
        if (_state == PlayerState.Flying)
        {
            UpdateFlying();
        }
    }

    public void TryLaunch(Vector3 direction)
    {
        if (_state != PlayerState.Attached || IsTakeoffOnCooldown) return;

        _flyingDirection = direction.normalized;

        if (Vector3.Dot(_flyingDirection, -CurrentSurfaceNormal.normalized) > 0f)
        {
            return;
        }

        transform.position += CurrentSurfaceNormal.normalized * _launchClearance;
        _attachedWallCollider = null;
        _state = PlayerState.Flying;
    }

    private void UpdateFlying()
    {
        if (TryAttach())
        {
            return;
        }

        transform.position += _flyingDirection * (_flySpeed * Time.deltaTime);
    }

    private bool TryAttach()
    {
        float sphereRadius = GetProjectedHalfExtent(transform.rotation, _flyingDirection) * 0.9f;
        float castDistance = _flySpeed * Time.deltaTime + sphereRadius + _launchClearance;

        if (!Physics.SphereCast(transform.position, sphereRadius, _flyingDirection, out RaycastHit hit, castDistance))
        {
            return false;
        }

        if (hit.collider == _playerCollider ||
            !hit.collider.TryGetComponent<Wall>(out _) ||
            hit.collider == _attachedWallCollider)
        {
            return false;
        }

        Attach(hit);
        return true;
    }

    private void Attach(RaycastHit hit)
    {
        _takeoffCooldown.Reset();
        TeleportToSurface(hit);
        _attachedWallCollider = hit.collider;
        _state = PlayerState.Attached;
    }

    private void TeleportToSurface(RaycastHit hit)
    {
        TeleportToSurface(hit.point, hit.normal, hit.collider);
    }

    private void TeleportToSurface(Vector3 targetPosition, Vector3 surfaceNormal, Collider hitCollider)
    {
        Vector3 localFaceNormal = _wallTouchingSideLocalNormal.sqrMagnitude > 0f
            ? _wallTouchingSideLocalNormal.normalized
            : Vector3.down;

        Quaternion targetRotation = Quaternion.FromToRotation(transform.rotation * localFaceNormal, -surfaceNormal) * transform.rotation;
        Vector3 adjustedTargetPosition = GetSafeSurfacePoint(targetPosition, surfaceNormal, targetRotation, hitCollider);

        transform.rotation = targetRotation;
        transform.position = adjustedTargetPosition - GetWorldOffsetToTouchingFace(localFaceNormal, targetRotation);

        CurrentSurfaceNormal = surfaceNormal;
        AttachedToWall?.Invoke(surfaceNormal);
    }

    private Vector3 GetSafeSurfacePoint(Vector3 targetPosition, Vector3 surfaceNormal, Quaternion targetRotation, Collider hitCollider)
    {
        if (hitCollider is not BoxCollider wallBox) return targetPosition;

        Transform wallTransform = wallBox.transform;
        Vector3 localSurfaceNormal = wallTransform.InverseTransformDirection(surfaceNormal).normalized;
        int normalAxis = AxisUtils.GetDominantAxis(localSurfaceNormal);
        float normalSign = Mathf.Sign(AxisUtils.GetAxis(localSurfaceNormal, normalAxis));

        Vector3 localPoint = wallTransform.InverseTransformPoint(targetPosition) - wallBox.center;
        Vector3 wallHalfSize = wallBox.size * 0.5f;

        for (int axis = 0; axis < 3; axis++)
        {
            if (axis == normalAxis)
            {
                AxisUtils.SetAxis(ref localPoint, axis, normalSign * AxisUtils.GetAxis(wallHalfSize, axis));
                continue;
            }

            Vector3 wallAxisWorld = wallTransform.TransformDirection(AxisUtils.GetUnitAxis(axis)).normalized;
            float wallAxisScale = Mathf.Abs(AxisUtils.GetAxis(wallTransform.lossyScale, axis));
            float inset = wallAxisScale > Mathf.Epsilon
                ? GetProjectedHalfExtent(targetRotation, wallAxisWorld) / wallAxisScale
                : 0f;
            float wallExtent = AxisUtils.GetAxis(wallHalfSize, axis);
            float min = -wallExtent + inset;
            float max = wallExtent - inset;
            float clampedValue = min <= max
                ? Mathf.Clamp(AxisUtils.GetAxis(localPoint, axis), min, max)
                : 0f;
            AxisUtils.SetAxis(ref localPoint, axis, clampedValue);
        }
        return wallTransform.TransformPoint(localPoint + wallBox.center);
    }

    private Vector3 GetWorldOffsetToTouchingFace(Vector3 localFaceNormal, Quaternion rotation)
    {
        if (_playerCollider is BoxCollider boxCollider)
        {
            Vector3 localFacePoint = boxCollider.center + new Vector3(
                Mathf.Sign(localFaceNormal.x) * boxCollider.size.x * 0.5f * Mathf.Abs(localFaceNormal.x),
                Mathf.Sign(localFaceNormal.y) * boxCollider.size.y * 0.5f * Mathf.Abs(localFaceNormal.y),
                Mathf.Sign(localFaceNormal.z) * boxCollider.size.z * 0.5f * Mathf.Abs(localFaceNormal.z));
            return rotation * Vector3.Scale(localFacePoint, transform.lossyScale);
        }
        return (_playerCollider != null) ? (rotation * localFaceNormal).normalized * _playerCollider.bounds.extents.magnitude : rotation * localFaceNormal * 0.5f;
    }

    private float GetProjectedHalfExtent(Quaternion rotation, Vector3 worldAxis)
    {
        if (_playerCollider is not BoxCollider playerBox) return _playerCollider != null ? _playerCollider.bounds.extents.magnitude : 0.5f;
        Vector3 halfSize = Vector3.Scale(playerBox.size * 0.5f, transform.lossyScale);
        Vector3 axis = worldAxis.normalized;
        return Mathf.Abs(Vector3.Dot(axis, rotation * Vector3.right)) * halfSize.x + Mathf.Abs(Vector3.Dot(axis, rotation * Vector3.up)) * halfSize.y + Mathf.Abs(Vector3.Dot(axis, rotation * Vector3.forward)) * halfSize.z;
    }
}
