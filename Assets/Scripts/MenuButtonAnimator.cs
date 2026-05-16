using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MenuButtonAnimator : MonoBehaviour
{
    [SerializeField] private float _hoverScale = 1.08f;
    [SerializeField] private float _pressScale = 0.94f;
    [SerializeField] private float _smoothing = 14f;
    [SerializeField] private float _hoverBrighten = 0.12f;

    private readonly List<ButtonState> _states = new();

    private class ButtonState
    {
        public RectTransform Transform;
        public Graphic TargetGraphic;
        public Vector3 BaseScale;
        public Color BaseColor;
        public bool Hovered;
        public bool Pressed;
    }

    private void Start()
    {
        Refresh();
    }

    public void Refresh()
    {
        _states.Clear();
        var buttons = GetComponentsInChildren<Button>(true);
        foreach (var btn in buttons)
        {
            if (btn == null) continue;
            var rect = btn.transform as RectTransform;
            if (rect == null) continue;

            var state = new ButtonState
            {
                Transform = rect,
                BaseScale = rect.localScale,
                TargetGraphic = btn.targetGraphic,
                BaseColor = btn.targetGraphic != null ? btn.targetGraphic.color : Color.white
            };
            _states.Add(state);

            var hooks = btn.GetComponent<ButtonHooks>();
            if (hooks == null) hooks = btn.gameObject.AddComponent<ButtonHooks>();
            hooks.Bind(state);
        }
    }

    private void Update()
    {
        float t = 1f - Mathf.Exp(-_smoothing * Time.unscaledDeltaTime);
        for (int i = 0; i < _states.Count; i++)
        {
            var s = _states[i];
            if (s.Transform == null) continue;

            float scale = s.Pressed ? _pressScale : s.Hovered ? _hoverScale : 1f;
            s.Transform.localScale = Vector3.Lerp(s.Transform.localScale, s.BaseScale * scale, t);

            if (s.TargetGraphic != null)
            {
                Color target = s.BaseColor;
                if (s.Hovered && !s.Pressed)
                {
                    target = new Color(
                        Mathf.Clamp01(s.BaseColor.r + _hoverBrighten),
                        Mathf.Clamp01(s.BaseColor.g + _hoverBrighten),
                        Mathf.Clamp01(s.BaseColor.b + _hoverBrighten),
                        s.BaseColor.a);
                }
                s.TargetGraphic.color = Color.Lerp(s.TargetGraphic.color, target, t);
            }
        }
    }

    private void OnDisable()
    {
        for (int i = 0; i < _states.Count; i++)
        {
            var s = _states[i];
            if (s.Transform != null) s.Transform.localScale = s.BaseScale;
            if (s.TargetGraphic != null) s.TargetGraphic.color = s.BaseColor;
        }
    }

    private class ButtonHooks : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        private ButtonState _state;
        public void Bind(ButtonState state) { _state = state; }
        public void OnPointerEnter(PointerEventData e) { if (_state != null) _state.Hovered = true; }
        public void OnPointerExit(PointerEventData e) { if (_state != null) { _state.Hovered = false; _state.Pressed = false; } }
        public void OnPointerDown(PointerEventData e) { if (_state != null) _state.Pressed = true; }
        public void OnPointerUp(PointerEventData e) { if (_state != null) _state.Pressed = false; }
    }
}
