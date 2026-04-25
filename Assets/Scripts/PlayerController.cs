using UnityEngine;
using System;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private Vector3 _wallTouchingSideLocalNormal = Vector3.down;

    private InputSystem_Actions _actions;
    private Collider _playerCollider;

    public event Action<Vector3> Teleported;
    public Vector3 CurrentSurfaceNormal { get; private set; } = Vector3.up;

    private void Awake()
    {
        _actions = new InputSystem_Actions();
        _actions.UI.Enable();
        _playerCollider = GetComponent<Collider>();
    }

    private void Update()
    {
        if (_actions.UI.Click.WasPressedThisFrame())
        {
            Vector2 clickPoint = _actions.UI.Point.ReadValue<Vector2>();
            Ray ray = Camera.main.ScreenPointToRay(clickPoint);

            if (Physics.Raycast(ray, out RaycastHit hit, 300f))
            {
                TeleportToSurface(hit.point, hit.normal);
            }
        }
    }

    public void TeleportToSurface(Vector3 targetPosition, Vector3 surfaceNormal)
    {
        Vector3 localFaceNormal = _wallTouchingSideLocalNormal.sqrMagnitude > 0f
            ? _wallTouchingSideLocalNormal.normalized
            : Vector3.down;

        Quaternion targetRotation = Quaternion.FromToRotation(transform.rotation * localFaceNormal, -surfaceNormal) * transform.rotation;
        transform.rotation = targetRotation;
        transform.position = targetPosition - GetWorldOffsetToTouchingFace(localFaceNormal, targetRotation);

        CurrentSurfaceNormal = surfaceNormal;
        Teleported?.Invoke(surfaceNormal);
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

    private void OnDestroy()
    {
        _actions?.Dispose();
    }
}
