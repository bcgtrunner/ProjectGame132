using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class PlayerInput : MonoBehaviour
{
    private PlayerController _controller;
    private PaintShooter _shooter;
    private InputSystem_Actions _actions;

    private void Awake()
    {
        _controller = GetComponent<PlayerController>();
        _shooter = GetComponent<PaintShooter>();
        _actions = new InputSystem_Actions();
    }

    private void OnEnable() => _actions.UI.Enable();
    private void OnDisable() => _actions.UI.Disable();

    private void Update()
    {
        if (_actions.UI.Click.WasPressedThisFrame())
        {
            Vector3 direction = Camera.main.transform.forward;
            _controller.TryLaunch(direction);
        }
        else if (_actions.UI.RightClick.WasPressedThisFrame())
        {
            Vector3 direction = Camera.main.transform.forward;
            _shooter.TryShoot(direction);
        }
    }

    private void OnDestroy() => _actions?.Dispose();
}