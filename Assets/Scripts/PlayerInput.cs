using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerController))]
public class PlayerInput : MonoBehaviour
{
    private PlayerController _controller;
    private PaintShooter _shooter;
    private InputSystem_Actions _actions;

    [SerializeField] private float _shotCooldownDuration = 0.05f;
    [SerializeField] private float _clickCooldownDuration = 0.3f;
    [SerializeField] private float _idleTimeLimit = 0.4f;
    [SerializeField] private float _maxChargeDuration = 1.5f;
    [SerializeField] private float _minClickScale = 2f;
    [SerializeField] private float _maxClickScale = 4f;

    private Cooldown _shotCooldown;
    private Cooldown _clickCooldown;
    private int _queuedShots;
    private float _lastWheelTime;
    private bool _hasWheelInput;
    private bool _isCharging;
    private float _chargeStartTime;
    private Camera _camera;

    private void Awake()
    {
        _controller = GetComponent<PlayerController>();
        _shooter = GetComponent<PaintShooter>();
        _actions = new InputSystem_Actions();
        _shotCooldown = new Cooldown(_shotCooldownDuration);
        _clickCooldown = new Cooldown(_clickCooldownDuration);
    }

    private void Start()
    {
        _camera = Camera.main;
        _controller.SetVirtualAttachment(Vector3.up);
        Vector3 launchDirection = _camera != null ? _camera.transform.forward : transform.forward;
        _controller.TryLaunch(launchDirection);
    }

    private void OnEnable() => _actions.UI.Enable();
    private void OnDisable() => _actions.UI.Disable();

    private void Update()
    {
        Vector3 direction = _camera != null ? _camera.transform.forward : transform.forward;
        if (_actions.UI.Escape.WasPerformedThisFrame())
        {
            GameManager.Instance.GoToMenu();
        }
        // Left click = charge shot (hold to ramp scale 2→4, release to fire)
        if (_actions.UI.Click.WasPressedThisFrame() && _clickCooldown.Over())
        {
            _isCharging = true;
            _chargeStartTime = Time.time;
        }

        if (_isCharging && _actions.UI.Click.WasReleasedThisFrame())
        {
            float chargeProgress = Mathf.Clamp01((Time.time - _chargeStartTime) / _maxChargeDuration);
            float scale = Mathf.Lerp(_minClickScale, _maxClickScale, chargeProgress);
            _shooter.TryShoot(direction, scale);
            _clickCooldown.Reset();
            _isCharging = false;
        }

        // Right click = launch
        if (_actions.UI.RightClick.WasPressedThisFrame())
        {
            _controller.TryLaunch(direction);
        }

        // Scroll wheel = queue shots
        if (_actions.UI.ScrollWheel.ReadValue<Vector2>() != Vector2.zero)
        {
            _queuedShots++;
            _lastWheelTime = Time.time;
            _hasWheelInput = true;
        }

        // Process queued shots
        if (_queuedShots > 0)
        {
            // If idle timeout has expired, discard remaining queue
            if (_hasWheelInput && Time.time - _lastWheelTime >= _idleTimeLimit)
            {
                _queuedShots = 0;
                _hasWheelInput = false;
            }
            else if (_shotCooldown.Over())
            {
                _shooter.TryShoot(direction);
                _queuedShots--;
                _shotCooldown.Reset();
            }
        }
    }

    private void OnGUI()
    {
        float barHeight = 8f;
        float barWidth = 100f;
        float x = 0f;
        float y = Screen.height - 40f;

        // Takeoff cooldown bar
        float takeoffProgress = Mathf.Clamp01(_controller.TakeoffCooldownProgress);
        float takeoffY = y - barHeight - 2f;
        GUI.color = Color.white;
        GUI.DrawTexture(new Rect(x, takeoffY, barWidth, barHeight), Texture2D.whiteTexture);
        GUI.color = Color.gray;
        GUI.DrawTexture(new Rect(x, takeoffY, barWidth * takeoffProgress, barHeight), Texture2D.whiteTexture);

        // Click cooldown bar / charge bar
        if (_isCharging)
        {
            float chargeProgress = Mathf.Clamp01((Time.time - _chargeStartTime) / _maxChargeDuration);
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(x, y, barWidth, barHeight), Texture2D.whiteTexture);
            GUI.color = Color.yellow;
            GUI.DrawTexture(new Rect(x, y, barWidth * chargeProgress, barHeight), Texture2D.whiteTexture);
        }
        else
        {
            float clickProgress = Mathf.Clamp01(_clickCooldown.Progress());
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(x, y, barWidth, barHeight), Texture2D.whiteTexture);
            GUI.color = Color.gray;
            GUI.DrawTexture(new Rect(x, y, barWidth * clickProgress, barHeight), Texture2D.whiteTexture);
        }

        // Shot (spam) cooldown bar
        float shotProgress = Mathf.Clamp01(_shotCooldown.Progress());
        float shotY = y + barHeight + 2f;
        GUI.color = Color.white;
        GUI.DrawTexture(new Rect(x, shotY, barWidth, barHeight), Texture2D.whiteTexture);
        GUI.color = Color.gray;
        GUI.DrawTexture(new Rect(x, shotY, barWidth * shotProgress, barHeight), Texture2D.whiteTexture);

        GUI.color = Color.white;
    }

    private void OnDestroy() => _actions?.Dispose();
}