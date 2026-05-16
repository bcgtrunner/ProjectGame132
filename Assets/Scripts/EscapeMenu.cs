using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class EscapeMenu : MonoBehaviour
{
    [SerializeField] private GameObject _panel;
    [SerializeField] private Button _resumeButton;
    [SerializeField] private Button _quitButton;
    [SerializeField] private bool _freezeTime = false;

    private bool _isOpen;
    private CursorLockMode _restoreLockState;
    private bool _restoreCursorVisible;

    private void Awake()
    {
        if (_panel != null) _panel.SetActive(false);
        if (_resumeButton != null) _resumeButton.onClick.AddListener(Close);
        if (_quitButton != null) _quitButton.onClick.AddListener(QuitToMenu);
    }

    private void Update()
    {
        if (InputManager.Instance.Actions.UI.Escape.WasPerformedThisFrame())
        {
            if (_isOpen) Close();
            else Open();
        }
    }

    private void Open()
    {
        _isOpen = true;
        if (_panel != null) _panel.SetActive(true);
        _restoreLockState = Cursor.lockState;
        _restoreCursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (_freezeTime) Time.timeScale = 0f;
    }

    private void Close()
    {
        _isOpen = false;
        if (_panel != null) _panel.SetActive(false);
        Cursor.lockState = _restoreLockState;
        Cursor.visible = _restoreCursorVisible;
        if (_freezeTime) Time.timeScale = 1f;
    }

    private void QuitToMenu()
    {
        if (_freezeTime) Time.timeScale = 1f;

        var nm = NetworkManager.Singleton;
        if (nm != null && nm.IsListening) nm.Shutdown();

        if (GameManager.Instance != null) GameManager.Instance.GoToMenu();
        else UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }

    private void OnDisable()
    {
        if (_freezeTime && _isOpen) Time.timeScale = 1f;
    }
}
