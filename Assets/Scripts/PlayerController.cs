using UnityEngine;
using System;
using Unity.VisualScripting;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private Vector3 _wallTouchingSideLocalNormal = Vector3.down;
    [SerializeField] private float _flySpeed = 10f;
    [SerializeField] private float _launchClearance = 0.1f;
    [SerializeField] private float _collisionSkin = 0.02f;
    [SerializeField] private float _maxHp = 6f;
    [SerializeField] private float _healRate = 0.1f;
    [SerializeField] private bool _launchOnWallDestroyed = false;
    private float _currentHp;
    private float _previousHp;

    private enum PlayerState { Attached, Flying }

    private Collider _playerCollider;
    private bool _isPlayerControlled;
    private PlayerState _state = PlayerState.Attached;
    private Vector3 _flyingDirection;
    private Collider _attachedWallCollider;
    private Wall _attachedWall;
    private int _paintTouchCount;

    public event Action<Vector3> AttachedToWall;
    public Vector3 CurrentSurfaceNormal { get; private set; } = Vector3.up;
    public bool IsTakeoffOnCooldown => !_takeoffCooldown.Over();
    public bool IsAttached => _state == PlayerState.Attached;
    public bool IsAlive => _currentHp > 0f;
    public float CurrentHp => _currentHp;
    public float MaxHp => _maxHp;
    public bool IsTouchingPaint => _paintTouchCount > 0;
    public Collider AttachedWallCollider => _attachedWallCollider;
    public bool LaunchOnWallDestroyed => _launchOnWallDestroyed;

    public void OnPaintContact(bool entered)
    {
        if (entered) _paintTouchCount++;
        else _paintTouchCount--;
    }

    public void SetMaxHp(float maxHp)
    {
        _maxHp = maxHp;
        _currentHp = maxHp;
    }

    public void TakeDamage(float amount)
    {
        _currentHp -= amount;
        if (_currentHp <= 0)
        {
            if (_isPlayerControlled)
            {
                SceneManager.LoadScene(0);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }

    private void UnsubscribeFromWall()
    {
        if (_attachedWall != null)
        {
            _attachedWall.Destroyed -= OnWallDestroyed;
            _attachedWall = null;
        }
    }

    private Cooldown _takeoffCooldown = new(1f);

    private void Awake()
    {
        _playerCollider = GetComponent<Collider>();
        _isPlayerControlled = TryGetComponent<PlayerInput>(out _);
        _currentHp = _maxHp;
        _previousHp = _maxHp;
    }

    private void OnDestroy()
    {
        UnsubscribeFromWall();
    }

    private void OnGUI()
    {
        if (_isPlayerControlled)
        {
            float hpRatio = _currentHp / _maxHp;
            float barHeight = 20f;
            float barY = Screen.height - barHeight;
            float barWidth = Screen.width;

            Color barColor = Color.white;
            if (_currentHp < _previousHp)
                barColor = Color.red;
            else if (_currentHp > _previousHp)
                barColor = Color.green;

            GUI.color = barColor;
            GUI.DrawTexture(new Rect(0, barY, barWidth * hpRatio, barHeight), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }

    private void Update()
    {
        _previousHp = _currentHp;

        if (_state == PlayerState.Flying)
        {
            UpdateFlying();
        }

        if (IsTouchingPaint)
        {
            TakeDamage(Time.deltaTime);
        }
        else if (_state == PlayerState.Attached && _currentHp < _maxHp)
        {
            _currentHp = Mathf.Min(_currentHp + _healRate * Time.deltaTime, _maxHp);
        }
    }

    private void OnWallDestroyed()
    {
        _attachedWall = null;
        _attachedWallCollider = null;
        if (_launchOnWallDestroyed)
        {
            _flyingDirection = CurrentSurfaceNormal.sqrMagnitude > Mathf.Epsilon
                ? CurrentSurfaceNormal.normalized
                : UnityEngine.Random.onUnitSphere;
            _state = PlayerState.Flying;
        }
        else
        {
            TakeDamage(_currentHp);
        }
    }

    public void TryLaunch(Vector3 direction)
    {
        if (_state != PlayerState.Attached || IsTakeoffOnCooldown) return;

        Vector3 flyingDirection = direction.normalized;
        if (Vector3.Dot(flyingDirection, -CurrentSurfaceNormal.normalized) > 0f)
        {
            return;
        }

        UnsubscribeFromWall();
        _flyingDirection = flyingDirection;
        transform.position += CurrentSurfaceNormal.normalized * _launchClearance;
        _attachedWallCollider = null;
        ResolveWallOverlaps();
        _state = PlayerState.Flying;
    }

    public void SetVirtualAttachment(Vector3 surfaceNormal)
    {
        CurrentSurfaceNormal = surfaceNormal.sqrMagnitude > Mathf.Epsilon
            ? surfaceNormal.normalized
            : Vector3.up;
        UnsubscribeFromWall();
        _attachedWallCollider = null;
        _state = PlayerState.Attached;
        AttachedToWall?.Invoke(CurrentSurfaceNormal);
    }

    private void UpdateFlying()
    {
        ResolveWallOverlaps();

        float moveDistance = _flySpeed * Time.deltaTime;

        if (TryAttach(moveDistance))
        {
            return;
        }

        transform.position += _flyingDirection * moveDistance;
    }

    private bool TryAttach(float moveDistance)
    {
        if (!TryGetClosestWallHit(_flyingDirection, moveDistance + _collisionSkin, out RaycastHit hit))
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
        UnsubscribeFromWall();
        _attachedWallCollider = hit.collider;
        _attachedWall = hit.collider.GetComponent<Wall>();
        if (_attachedWall != null)
        {
            _attachedWall.Destroyed += OnWallDestroyed;
        }
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

    private bool TryGetClosestWallHit(Vector3 direction, float castDistance, out RaycastHit closestHit)
    {
        if (_playerCollider is BoxCollider playerBox)
        {
            Vector3 center = GetBoxColliderWorldCenter(playerBox, transform.position, transform.rotation);
            Vector3 halfExtents = ShrinkHalfExtents(GetBoxColliderHalfExtents(playerBox), _collisionSkin);
            RaycastHit[] hits = Physics.BoxCastAll(center, halfExtents, direction, transform.rotation, castDistance, Physics.AllLayers, QueryTriggerInteraction.Ignore);
            return TryGetClosestValidWallHit(hits, out closestHit);
        }

        float sphereRadius = Mathf.Max(GetProjectedHalfExtent(transform.rotation, direction) - _collisionSkin, 0.001f);
        RaycastHit[] sphereHits = Physics.SphereCastAll(transform.position, sphereRadius, direction, castDistance, Physics.AllLayers, QueryTriggerInteraction.Ignore);
        return TryGetClosestValidWallHit(sphereHits, out closestHit);
    }

    private bool TryGetClosestValidWallHit(RaycastHit[] hits, out RaycastHit closestHit)
    {
        closestHit = default;
        bool foundHit = false;
        float closestDistance = float.MaxValue;

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == _playerCollider ||
                hit.collider == _attachedWallCollider ||
                !hit.collider.TryGetComponent<Wall>(out _))
            {
                continue;
            }

            if (hit.distance >= closestDistance)
            {
                continue;
            }

            closestDistance = hit.distance;
            closestHit = hit;
            foundHit = true;
        }

        return foundHit;
    }

    private void ResolveWallOverlaps()
    {
        if (_playerCollider is not BoxCollider playerBox)
        {
            return;
        }

        for (int iteration = 0; iteration < 3; iteration++)
        {
            Vector3 center = GetBoxColliderWorldCenter(playerBox, transform.position, transform.rotation);
            Vector3 halfExtents = ShrinkHalfExtents(GetBoxColliderHalfExtents(playerBox), _collisionSkin * 0.5f);
            Collider[] overlaps = Physics.OverlapBox(center, halfExtents, transform.rotation, Physics.AllLayers, QueryTriggerInteraction.Ignore);
            Vector3 correction = Vector3.zero;

            foreach (Collider overlap in overlaps)
            {
                if (overlap == _playerCollider ||
                    overlap == _attachedWallCollider ||
                    !overlap.TryGetComponent<Wall>(out _))
                {
                    continue;
                }

                if (!Physics.ComputePenetration(
                        playerBox,
                        transform.position,
                        transform.rotation,
                        overlap,
                        overlap.transform.position,
                        overlap.transform.rotation,
                        out Vector3 direction,
                        out float distance) ||
                    distance <= 0f)
                {
                    continue;
                }

                correction += direction * (distance + _collisionSkin);
            }

            if (correction.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            transform.position += correction;
        }
    }

    private Vector3 GetBoxColliderWorldCenter(BoxCollider boxCollider, Vector3 position, Quaternion rotation)
    {
        return position + rotation * Vector3.Scale(boxCollider.center, transform.lossyScale);
    }

    private Vector3 GetBoxColliderHalfExtents(BoxCollider boxCollider)
    {
        Vector3 absoluteScale = new(
            Mathf.Abs(transform.lossyScale.x),
            Mathf.Abs(transform.lossyScale.y),
            Mathf.Abs(transform.lossyScale.z));
        return Vector3.Scale(boxCollider.size * 0.5f, absoluteScale);
    }

    private static Vector3 ShrinkHalfExtents(Vector3 halfExtents, float amount)
    {
        return new Vector3(
            Mathf.Max(halfExtents.x - amount, 0.001f),
            Mathf.Max(halfExtents.y - amount, 0.001f),
            Mathf.Max(halfExtents.z - amount, 0.001f));
    }
}
