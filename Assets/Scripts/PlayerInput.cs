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

    private Cooldown _shotCooldown;
    private Cooldown _clickCooldown;
    private int _queuedShots;
    private float _lastWheelTime;
    private bool _hasWheelInput;

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
        _controller.SetVirtualAttachment(Vector3.up);
        Vector3 launchDirection = Camera.main != null ? Camera.main.transform.forward : transform.forward;
        _controller.TryLaunch(launchDirection);
    }

    private void OnEnable() => _actions.UI.Enable();
    private void OnDisable() => _actions.UI.Disable();

    private void Update()
    {
        Vector3 direction = Camera.main != null ? Camera.main.transform.forward : transform.forward;

        // Left click = shoot (bigger projectile, rate-limited)
        if (_actions.UI.Click.WasPressedThisFrame() && _clickCooldown.Over())
        {
            _shooter.TryShoot(direction, 2f);
            _clickCooldown.Reset();
        }

        // Right click = launch
        if (_actions.UI.RightClick.WasPressedThisFrame())
        {
            _controller.TryLaunch(direction);
        }

        // Scroll wheel = queue shots
        if (_actions.UI.ScrollWheel.WasPerformedThisFrame())
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

    private void OnDestroy() => _actions?.Dispose();
}