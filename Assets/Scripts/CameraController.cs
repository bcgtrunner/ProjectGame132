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
    private Quaternion _baseLocalRotation;
    private Quaternion _attachStartLocalRotation;
    private Quaternion _attachTargetLocalRotation;
    private Transform _playerTransform;
    private Camera _camera;
    private Quaternion _lastWorldRotation;
    private bool _hasLastWorldRotation;
    private bool _isAttachTransitionActive;

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

            if (_isAttachTransitionActive)
            {
                transform.localRotation = Quaternion.Slerp(_attachStartLocalRotation, _attachTargetLocalRotation, t);
                _lastWorldRotation = transform.rotation;
                _hasLastWorldRotation = true;
                return;
            }
        }
        else
        {
            if (_isAttachTransitionActive)
            {
                _isAttachTransitionActive = false;
                _baseLocalRotation = _attachTargetLocalRotation;
                _yaw = 0f;
                _pitch = 0f;
            }

            Vector2 lookDelta = _actions.Player.Look.ReadValue<Vector2>();
            _yaw += lookDelta.x * _rotationSensitivity;
            _pitch = Mathf.Clamp(_pitch - lookDelta.y * _rotationSensitivity, _minPitch, _maxPitch);
        }

        transform.localPosition = Vector3.zero;
        transform.localRotation = ComposeLocalRotation();
        _lastWorldRotation = transform.rotation;
        _hasLastWorldRotation = true;
    }

    private void HandlePlayerAttachedToWall(Vector3 surfaceNormal)
    {
        Quaternion preservedWorldRotation = _hasLastWorldRotation ? _lastWorldRotation : transform.rotation;
        Quaternion preservedLocalRotation = Quaternion.Inverse(_playerTransform.rotation) * preservedWorldRotation;
        Vector3 currentForward = preservedWorldRotation * Vector3.forward;
        Vector3 targetForward = surfaceNormal.normalized;
        Quaternion shortestDelta = Quaternion.FromToRotation(currentForward, targetForward);
        Quaternion targetWorldRotation = shortestDelta * preservedWorldRotation;

        _attachStartLocalRotation = preservedLocalRotation;
        _attachTargetLocalRotation = Quaternion.Inverse(_playerTransform.rotation) * targetWorldRotation;
        _isAttachTransitionActive = true;

        // Apply immediately so the parent snap is canceled before the transition begins.
        transform.localRotation = _attachStartLocalRotation;
        _transitionTime = 0f;
    }

    private void OnGUI()
    {
        if (_camera == null) return;

        float centerX = Screen.width / 2f;
        float centerY = Screen.height / 2f;
        float lineLength = 10f;
        float lineWidth = 2f;
        float gap = 4f;

        Color originalColor = GUI.color;
        GUI.color = Color.white;

        GUI.DrawTexture(new Rect(centerX - lineWidth / 2f, centerY - gap - lineLength, lineWidth, lineLength), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(centerX - lineWidth / 2f, centerY + gap, lineWidth, lineLength), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(centerX - gap - lineLength, centerY - lineWidth / 2f, lineLength, lineWidth), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(centerX + gap, centerY - lineWidth / 2f, lineLength, lineWidth), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(centerX - 1f, centerY - 1f, 3f, 3f), Texture2D.whiteTexture);

        GUI.color = originalColor;
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
