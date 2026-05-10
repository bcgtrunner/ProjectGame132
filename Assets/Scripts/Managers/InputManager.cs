using NUnit.Framework;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance;
    private InputSystem_Actions _actions;

    public InputSystem_Actions Actions => _actions;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        _actions = new InputSystem_Actions();
        _actions.Enable();
    }

    void OnEnable()
    {
        if (_actions == null)
        {
            _actions.Enable();
        }
    }

    private void OnDisable()
    {
        if (_actions != null)
        {
            _actions.Disable();
        }
    }

    private void OnDestroy()
    {
        if (_actions != null)
        {
            _actions.Dispose();
        }
    }
}
