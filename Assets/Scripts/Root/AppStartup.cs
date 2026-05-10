using UnityEngine;

public class AppStartup : MonoBehaviour
{
    private void Awake()
    {
        if (GameManager.Instance != null) return;

    }

    private void Start()
    {
        
    }
}
