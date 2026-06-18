using System.Collections;
using UnityEngine;

public class GuidePanelController : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField] private float displayDuration = 5f;
    [SerializeField] private GameObject guidePanel;
    private Coroutine hideCoroutine;

    private void Start()
    {
        ShowAndStartTimer();
    }

    private void ShowAndStartTimer()
    {
        guidePanel.SetActive(true);

        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
        }

        hideCoroutine = StartCoroutine(HideAfterDelay(displayDuration));
    }

    public void OpenPanelManual()
    {
        if (guidePanel.activeInHierarchy) 
        {
            ClosePanelManual();
            return;
        }
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }

        guidePanel.SetActive(true);
    }

    public void ClosePanelManual()
    {
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }

        guidePanel.SetActive(false);
    }

    private IEnumerator HideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        guidePanel.SetActive(false);
        hideCoroutine = null;
    }
}