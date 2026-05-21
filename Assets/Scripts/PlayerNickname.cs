using UnityEngine;

public static class PlayerNickname
{
    private const string Key = "PlayerNickname";

    public static string Value
    {
        get => PlayerPrefs.GetString(Key, "");
        set { PlayerPrefs.SetString(Key, value); PlayerPrefs.Save(); }
    }
}
