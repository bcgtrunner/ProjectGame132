using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Menu : MonoBehaviour
{
    [SerializeField] private Button playButton;

    public void OnPlayButtonClicked()
    {
        SceneManager.LoadScene(1);
    }
}
