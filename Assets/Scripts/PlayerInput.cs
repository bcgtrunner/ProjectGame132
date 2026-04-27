using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class PlayerInput : MonoBehaviour
{
    private PlayerController _controller;
    private InputSystem_Actions _actions;

    private void Awake()
    {
        _controller = GetComponent<PlayerController>();
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
    }

    private void OnDestroy() => _actions?.Dispose();
}