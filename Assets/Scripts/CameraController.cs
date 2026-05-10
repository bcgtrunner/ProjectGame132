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
        _camera = GetComponent<Camera>();
        if (_playerController == null)
            _playerController = FindAnyObjectByType<PlayerController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Start()
    {
        _actions = InputManager.Instance.Actions;
        if (_playerController != null && _playerTransform == null)
            BindToPlayer();
    }

    public void AssignPlayerController(PlayerController playerController)
    {
        _playerController = playerController;
        if (isActiveAndEnabled && _playerTransform == null)
            BindToPlayer();
    }

    private void BindToPlayer()
    {
        _playerTransform = _playerController.transform;
        transform.SetParent(_playerTransform, false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        if (_camera != null)
            _camera.nearClipPlane = 0.01f;

        MeshRenderer playerRenderer = _playerController.GetComponent<MeshRenderer>();
        if (playerRenderer != null)
            playerRenderer.enabled = false;

        _playerController.AttachedToWall += HandlePlayerAttachedToWall;

        HandlePlayerAttachedToWall(_playerController.CurrentSurfaceNormal, _playerTransform.rotation);
    }

    private Quaternion ComposeLocalRotation() => _baseLocalRotation * Quaternion.Euler(_pitch, _yaw, 0f);

    private void LateUpdate()
    {
        if (_playerTransform == null) return;

        Vector2 lookDelta = _actions.Player.Look.ReadValue<Vector2>();
        _yaw += lookDelta.x * _rotationSensitivity;
        _pitch = Mathf.Clamp(_pitch - lookDelta.y * _rotationSensitivity, _minPitch, _maxPitch);

        transform.localPosition = Vector3.zero;
        transform.localRotation = ComposeLocalRotation();
    }

    private void HandlePlayerAttachedToWall(Vector3 surfaceNormal, Quaternion newPlayerRotation)
    {
        // Capture the camera's true world rotation BEFORE the parent snaps to newPlayerRotation,
        // then derive the local rotation that keeps the same world facing under the new parent rotation.
        Quaternion preservedWorldRotation = transform.rotation;
        _baseLocalRotation = Quaternion.Inverse(newPlayerRotation) * preservedWorldRotation;
        _yaw = 0f;
        _pitch = 0f;
        transform.localRotation = ComposeLocalRotation();
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
            _playerController.AttachedToWall -= HandlePlayerAttachedToWall;
    }
}
