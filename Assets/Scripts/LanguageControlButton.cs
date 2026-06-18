using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

[RequireComponent(typeof(Button))]
public class LanguageControlButton : MonoBehaviour
{
    [SerializeField] private TMP_Text buttonText;
    
    private Button button;
    private bool isChanging = false;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnButtonClick); 
    }

    private IEnumerator Start()
    {
        yield return LocalizationSettings.InitializationOperation;
        UpdateButtonText(LocalizationSettings.SelectedLocale);
    }

    private void OnButtonClick()
    {
        if (isChanging) return; 
        StartCoroutine(ToggleLanguageRoutine());
    }

    private IEnumerator ToggleLanguageRoutine()
    {
        isChanging = true;

        var locales = LocalizationSettings.AvailableLocales.Locales;
        if (locales.Count <= 1) 
        {
            isChanging = false;
            yield break;
        }

        int currentLocaleIndex = locales.IndexOf(LocalizationSettings.SelectedLocale);
        
        int nextLocaleIndex = (currentLocaleIndex + 1) % locales.Count;
        
        Locale nextLocale = locales[nextLocaleIndex];
        LocalizationSettings.SelectedLocale = nextLocale;

        UpdateButtonText(nextLocale);

        isChanging = false;
    }

    private void UpdateButtonText(Locale locale)
    {
        if (buttonText != null && locale != null)
        {
            string languageName = locale.Identifier.CultureInfo.NativeName;
            
            if (!string.IsNullOrEmpty(languageName))
            {
                languageName = char.ToUpper(languageName[0]) + languageName.Substring(1);
            }

            buttonText.text = languageName;
        }
    }
}