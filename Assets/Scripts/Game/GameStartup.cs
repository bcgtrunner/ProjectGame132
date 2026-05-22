using UnityEngine;

public class GameStartup : MonoBehaviour
{
    [SerializeField] private PlayerController _player;
    private void Start()
    {
        GameManager.Instance.Player = _player;
    }

    private void OnDestroy()
    {
        GameManager.Instance.Player = null;
    }
}
