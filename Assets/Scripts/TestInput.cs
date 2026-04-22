using UnityEngine;

public class TestInput : MonoBehaviour
{
    private InputSystem_Actions _actions;
    private void Awake()
    {
        _actions = new InputSystem_Actions();
        _actions.UI.Enable();
    }

    private void Update()
    {
        if (_actions.UI.Click.ReadValue<float>() == 1)
        {
            Vector2 clickPoint = _actions.UI.Point.ReadValue<Vector2>();
            if (Physics.Raycast(Camera.main.ScreenPointToRay(clickPoint), out var hit, 300f))
            {
                Transform hitObject = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
                hitObject.position = hit.point - hit.point.z * Vector3.forward;
                hitObject.GetComponent<MeshRenderer>().material.color = Color.darkSeaGreen;
                Destroy(hitObject.gameObject, 3f);
            }
        }
    }
}
