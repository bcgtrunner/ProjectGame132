using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private PlayerController _playerController;
    [SerializeField] private float _rotationSensitivity = 0.3f;
    [SerializeField] private float _minPitch = -80f;
    [SerializeField] private float _maxPitch = 80f;
    [SerializeField] private float _attachSmoothingSpeed = 8f;

    private InputSystem_Actions _actions;
    private float _yaw;
    private float _pitch;
    private float _targetYaw;
    private float _targetPitch;
    private float _startYaw;
    private float _startPitch;
    private Quaternion _baseLocalRotation;
    private Quaternion _targetBaseLocalRotation;
    private Quaternion _startBaseLocalRotation;
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
            _playerController = FindAnyObjectByType<PlayerController>();
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
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

        _playerController.AttachedToWall += HandlePlayerAttachedToWall;

        HandlePlayerAttachedToWall(_playerController.CurrentSurfaceNormal);
    }

    private float _transitionTime;

    private void LateUpdate()
    {
        if (_playerTransform == null)
        {
            return;
        }

        bool onCooldown = _playerController.IsTakeoffOnCooldown;

        if (onCooldown)
        {
            _transitionTime += _attachSmoothingSpeed * Time.deltaTime;
            float t = Mathf.Clamp01(_transitionTime);

            // Lerp all components uniformly with same t so they arrive together
            _yaw = Mathf.Lerp(_startYaw, _targetYaw, t);
            _pitch = Mathf.Lerp(_startPitch, _targetPitch, t);
            _baseLocalRotation = Quaternion.Slerp(_startBaseLocalRotation, _targetBaseLocalRotation, t);

            transform.localPosition = Vector3.zero;
            transform.localRotation = _baseLocalRotation * Quaternion.Euler(_pitch, _yaw, 0f);
        }
        else
        {
            // Smoothly interpolate base rotation toward target
            _baseLocalRotation = Quaternion.Slerp(_baseLocalRotation, _targetBaseLocalRotation, _attachSmoothingSpeed * Time.deltaTime);

            Vector2 lookDelta = _actions.Player.Look.ReadValue<Vector2>();
            _yaw += lookDelta.x * _rotationSensitivity;
            _pitch = Mathf.Clamp(_pitch - lookDelta.y * _rotationSensitivity, _minPitch, _maxPitch);

            _targetYaw = _yaw;
            _targetPitch = _pitch;

            transform.localPosition = Vector3.zero;
            transform.localRotation = _baseLocalRotation * Quaternion.Euler(_pitch, _yaw, 0f);
        }
    }

    private void HandlePlayerAttachedToWall(Vector3 surfaceNormal)
    {
        Vector3 localAwayFromWall = _playerTransform.InverseTransformDirection(surfaceNormal.normalized);
        Vector3 localUpReference = Vector3.up;

        if (Mathf.Abs(Vector3.Dot(localAwayFromWall.normalized, localUpReference)) > 0.99f)
        {
            localUpReference = Vector3.forward;
        }

        _targetBaseLocalRotation = Quaternion.LookRotation(localAwayFromWall, localUpReference);
        _targetYaw = 0f;
        _targetPitch = 0f;

        // Capture current component values as start of uniform transition
        _startYaw = _yaw;
        _startPitch = _pitch;
        _startBaseLocalRotation = _baseLocalRotation;
        _transitionTime = 0f;
    }

    private void OnDestroy()
    {
        if (_playerController != null)
        {
            _playerController.AttachedToWall -= HandlePlayerAttachedToWall;
        }

        _actions?.Dispose();
    }
}