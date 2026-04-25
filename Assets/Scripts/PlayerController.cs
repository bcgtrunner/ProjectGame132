using UnityEngine;
using System;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private Vector3 _wallTouchingSideLocalNormal = Vector3.down;
    [SerializeField] private float _flySpeed = 10f;
    [SerializeField] private float _launchClearance = 0.1f;

    private enum PlayerState { Attached, Flying }

    private InputSystem_Actions _actions;
    private Collider _playerCollider;
    private PlayerState _state = PlayerState.Attached;
    private Vector3 _flyingDirection;
    private Collider _attachedWallCollider;

    public event Action<Vector3> AttachedToWall;
    public Vector3 CurrentSurfaceNormal { get; private set; } = Vector3.up;

    private void Awake()
    {
        _actions = new InputSystem_Actions();
        _actions.UI.Enable();
        _playerCollider = GetComponent<Collider>();
    }

    private void Update()
    {
        switch (_state)
        {
            case PlayerState.Attached:
                UpdateAttached();
                break;
            case PlayerState.Flying:
                UpdateFlying();
                break;
        }
    }

    private void UpdateAttached()
    {
        if (_actions.UI.Click.WasPressedThisFrame())
        {
            _flyingDirection = Camera.main.transform.forward;

            // If the player is aiming back into the wall they're attached to,
            // remain attached instead of trying to launch through the surface.
            if (Vector3.Dot(_flyingDirection, -CurrentSurfaceNormal.normalized) > 0f)
            {
                return;
            }

            // Move slightly away from the surface before flight starts so we don't begin inside the attached wall.
            transform.position += CurrentSurfaceNormal.normalized * _launchClearance;
            _attachedWallCollider = null;
            _state = PlayerState.Flying;
        }
    }

    private void UpdateFlying()
    {
        float moveDistance = _flySpeed * Time.deltaTime;

        // Cast from the player's current position so nearby walls are still detected
        // even when the player launches while facing the surface they are attached to.
        float sphereRadius = GetProjectedHalfExtent(transform.rotation, _flyingDirection) * 0.9f;
        Vector3 castOrigin = transform.position;
        float castDistance = moveDistance + sphereRadius + _launchClearance;

        if (Physics.SphereCast(castOrigin, sphereRadius, _flyingDirection, out RaycastHit hit, castDistance))
        {
            if (hit.collider != _playerCollider &&
                hit.collider.TryGetComponent<Wall>(out _) &&
                hit.collider != _attachedWallCollider)
            {
                TeleportToSurface(hit);
                _attachedWallCollider = hit.collider;
                _state = PlayerState.Attached;
                return;
            }
        }

        transform.position += _flyingDirection * moveDistance;
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
        if (hitCollider is not BoxCollider wallBox)
        {
            return targetPosition;
        }

        Transform wallTransform = wallBox.transform;
        Vector3 localSurfaceNormal = wallTransform.InverseTransformDirection(surfaceNormal).normalized;
        int normalAxis = GetDominantAxis(localSurfaceNormal);
        float normalSign = Mathf.Sign(GetAxis(localSurfaceNormal, normalAxis));

        Vector3 localPoint = wallTransform.InverseTransformPoint(targetPosition) - wallBox.center;
        Vector3 wallHalfSize = wallBox.size * 0.5f;

        for (int axis = 0; axis < 3; axis++)
        {
            if (axis == normalAxis)
            {
                SetAxis(ref localPoint, axis, normalSign * GetAxis(wallHalfSize, axis));
                continue;
            }

            Vector3 wallAxisWorld = wallTransform.TransformDirection(GetUnitAxis(axis)).normalized;
            float wallAxisScale = Mathf.Abs(GetAxis(wallTransform.lossyScale, axis));
            float inset = wallAxisScale > Mathf.Epsilon
                ? GetProjectedHalfExtent(targetRotation, wallAxisWorld) / wallAxisScale
                : 0f;
            float wallExtent = GetAxis(wallHalfSize, axis);
            float min = -wallExtent + inset;
            float max = wallExtent - inset;
            float clampedValue = min <= max
                ? Mathf.Clamp(GetAxis(localPoint, axis), min, max)
                : 0f;
            SetAxis(ref localPoint, axis, clampedValue);
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

            Vector3 scaledLocalPoint = Vector3.Scale(localFacePoint, transform.lossyScale);
            return rotation * scaledLocalPoint;
        }

        if (_playerCollider != null)
        {
            Vector3 worldFaceDirection = rotation * localFaceNormal;
            return worldFaceDirection.normalized * _playerCollider.bounds.extents.magnitude;
        }

        return rotation * localFaceNormal * 0.5f;
    }

    private float GetProjectedHalfExtent(Quaternion rotation, Vector3 worldAxis)
    {
        if (_playerCollider is not BoxCollider playerBox)
        {
            return _playerCollider != null ? _playerCollider.bounds.extents.magnitude : 0.5f;
        }

        Vector3 halfSize = Vector3.Scale(playerBox.size * 0.5f, transform.lossyScale);
        Vector3 axis = worldAxis.normalized;

        return Mathf.Abs(Vector3.Dot(axis, rotation * Vector3.right)) * halfSize.x
            + Mathf.Abs(Vector3.Dot(axis, rotation * Vector3.up)) * halfSize.y
            + Mathf.Abs(Vector3.Dot(axis, rotation * Vector3.forward)) * halfSize.z;
    }

    private static int GetDominantAxis(Vector3 vector)
    {
        Vector3 absolute = new Vector3(Mathf.Abs(vector.x), Mathf.Abs(vector.y), Mathf.Abs(vector.z));

        if (absolute.x > absolute.y && absolute.x > absolute.z)
        {
            return 0;
        }

        return absolute.y > absolute.z ? 1 : 2;
    }

    private static Vector3 GetUnitAxis(int axis)
    {
        return axis switch
        {
            0 => Vector3.right,
            1 => Vector3.up,
            _ => Vector3.forward,
        };
    }

    private static float GetAxis(Vector3 vector, int axis)
    {
        return axis switch
        {
            0 => vector.x,
            1 => vector.y,
            _ => vector.z,
        };
    }

    private static void SetAxis(ref Vector3 vector, int axis, float value)
    {
        switch (axis)
        {
            case 0:
                vector.x = value;
                break;
            case 1:
                vector.y = value;
                break;
            default:
                vector.z = value;
                break;
        }
    }

    private void OnDestroy()
    {
        _actions?.Dispose();
    }
}
