using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private PlayerController _playerController;
    [SerializeField] private float _rotationSensitivity = 0.3f;
    [SerializeField] private float _minPitch = -80f;
    [SerializeField] private float _maxPitch = 80f;
    [SerializeField] private float _attachTransitionDuration = 1f;

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
    private Quaternion _lastWorldRotation;
    private bool _hasLastWorldRotation;

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
        _lastWorldRotation = transform.rotation;
        _hasLastWorldRotation = true;
    }

    private float _transitionTime;

    private Quaternion ComposeLocalRotation() => _baseLocalRotation * Quaternion.Euler(_pitch, _yaw, 0f);

    private void LateUpdate()
    {
        if (_playerTransform == null)
        {
            return;
        }

        bool onCooldown = _playerController.IsTakeoffOnCooldown;

        if (onCooldown)
        {
            _transitionTime += Time.deltaTime;
            float t = _attachTransitionDuration > Mathf.Epsilon
                ? Mathf.Clamp01(_transitionTime / _attachTransitionDuration)
                : 1f;

            // Lerp all components uniformly with same t so they arrive together
            _yaw = Mathf.Lerp(_startYaw, _targetYaw, t);
            _pitch = Mathf.Lerp(_startPitch, _targetPitch, t);
            _baseLocalRotation = Quaternion.Slerp(_startBaseLocalRotation, _targetBaseLocalRotation, t);
        }
        else
        {
            // Smoothly interpolate base rotation toward target
            float rotationStep = _attachTransitionDuration > Mathf.Epsilon
                ? Time.deltaTime / _attachTransitionDuration
                : 1f;
            _baseLocalRotation = Quaternion.Slerp(_baseLocalRotation, _targetBaseLocalRotation, rotationStep);

            Vector2 lookDelta = _actions.Player.Look.ReadValue<Vector2>();
            _yaw += lookDelta.x * _rotationSensitivity;
            _pitch = Mathf.Clamp(_pitch - lookDelta.y * _rotationSensitivity, _minPitch, _maxPitch);

            _targetYaw = _yaw;
            _targetPitch = _pitch;
        }

        transform.localPosition = Vector3.zero;
        transform.localRotation = ComposeLocalRotation();
        _lastWorldRotation = transform.rotation;
        _hasLastWorldRotation = true;
    }

    private void HandlePlayerAttachedToWall(Vector3 surfaceNormal)
    {
        Quaternion preservedWorldRotation = _hasLastWorldRotation ? _lastWorldRotation : transform.rotation;
        Vector3 localAwayFromWall = _playerTransform.InverseTransformDirection(surfaceNormal.normalized);
        Vector3 localUpReference = Vector3.up;

        if (Mathf.Abs(Vector3.Dot(localAwayFromWall.normalized, localUpReference)) > 0.99f)
        {
            localUpReference = Vector3.forward;
        }

        _targetBaseLocalRotation = Quaternion.LookRotation(localAwayFromWall, localUpReference);
        _targetYaw = 0f;
        _targetPitch = 0f;

        // Preserve the camera's current world orientation after the player snaps to the wall,
        // then animate from that local rotation into the new wall-relative target.
        Quaternion preservedLocalRotation = Quaternion.Inverse(_playerTransform.rotation) * preservedWorldRotation;
        Quaternion baseInverse = Quaternion.Inverse(_baseLocalRotation);
        Quaternion lookOffset = baseInverse * preservedLocalRotation;
        Vector3 preservedEuler = lookOffset.eulerAngles;
        float preservedYaw = Mathf.DeltaAngle(0f, preservedEuler.y);
        float preservedPitch = Mathf.DeltaAngle(0f, preservedEuler.x);

        // Apply immediately so the parent snap is canceled before the transition begins.
        transform.localRotation = preservedLocalRotation;

        // Capture current component values as start of uniform transition
        _yaw = preservedYaw;
        _pitch = preservedPitch;
        _startYaw = preservedYaw;
        _startPitch = preservedPitch;
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
