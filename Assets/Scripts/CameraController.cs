using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private PlayerController _playerController;
    [SerializeField] private float _rotationSensitivity = 0.3f;
    [SerializeField] private float _minPitch = -80f;
    [SerializeField] private float _maxPitch = 80f;

    private InputSystem_Actions _actions;
    private float _yaw;
    private float _pitch;
    private Quaternion _baseLocalRotation;
    private Transform _playerTransform;
    private Camera _camera;

    private void Awake()
    {
        _actions = new InputSystem_Actions();
        _actions.Player.Enable();
        _actions.UI.Enable();
        _camera = GetComponent<Camera>();

        if (_playerController == null)
        {
            _playerController = FindFirstObjectByType<PlayerController>();
        }
    }

    public void AssignPlayerController(PlayerController playerController)
    {
        _playerController = playerController;
    }

    private void Start()
    {
        if (_playerController == null)
        {
            Debug.LogWarning("CameraController could not find a PlayerController in the scene.");
            enabled = false;
            return;
        }

        _playerTransform = _playerController.transform;
        transform.SetParent(_playerTransform, false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        if (_camera != null)
        {
            _camera.nearClipPlane = 0.01f;
        }

        MeshRenderer playerRenderer = _playerController.GetComponent<MeshRenderer>();
        if (playerRenderer != null)
        {
            playerRenderer.enabled = false;
        }

        _playerController.Teleported += HandlePlayerTeleported;

        HandlePlayerTeleported(_playerController.CurrentSurfaceNormal);
    }

    private void LateUpdate()
    {
        if (_playerTransform == null)
        {
            return;
        }

        if (_actions.UI.RightClick.ReadValue<float>() > 0f)
        {
            Vector2 lookDelta = _actions.Player.Look.ReadValue<Vector2>();
            _yaw += lookDelta.x * _rotationSensitivity;
            _pitch = Mathf.Clamp(_pitch - lookDelta.y * _rotationSensitivity, _minPitch, _maxPitch);
        }

        transform.localPosition = Vector3.zero;
        transform.localRotation = _baseLocalRotation * Quaternion.Euler(_pitch, _yaw, 0f);
    }

    private void HandlePlayerTeleported(Vector3 surfaceNormal)
    {
        Vector3 localAwayFromWall = _playerTransform.InverseTransformDirection(surfaceNormal.normalized);
        Vector3 localUpReference = Vector3.up;

        if (Mathf.Abs(Vector3.Dot(localAwayFromWall.normalized, localUpReference)) > 0.99f)
        {
            localUpReference = Vector3.forward;
        }

        _baseLocalRotation = Quaternion.LookRotation(localAwayFromWall, localUpReference);
        _yaw = 0f;
        _pitch = 0f;
    }

    private void OnDestroy()
    {
        if (_playerController != null)
        {
            _playerController.Teleported -= HandlePlayerTeleported;
        }

        _actions?.Dispose();
    }
}
