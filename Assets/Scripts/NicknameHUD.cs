using Unity.Netcode;
using UnityEngine;

public class NicknameHUD : MonoBehaviour
{
    private GUIStyle _labelStyle;
    private GUIStyle _indicatorStyle;

    private void OnGUI()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsListening) return;

        var cam = Camera.main;
        if (cam == null) return;

        EnsureStyles();

        ulong localId = nm.LocalClientId;

        foreach (var client in nm.ConnectedClientsList)
        {
            if (client.ClientId == localId) continue;

            var playerObj = client.PlayerObject;
            if (playerObj == null) continue;
            if (!playerObj.TryGetComponent<NetworkPlayerSetup>(out var setup)) continue;
            if (setup.NicknameHidden.Value) continue;
            if (setup.SyncedHp.Value <= 0f) continue;

            string nick = setup.Nickname.Value.ToString();
            if (string.IsNullOrEmpty(nick)) continue;

            Vector3 worldPos = playerObj.transform.position + Vector3.up * 2f;
            Vector3 screenPos = cam.WorldToScreenPoint(worldPos);
            if (screenPos.z <= 0f) continue;

            float x = screenPos.x;
            float y = Screen.height - screenPos.y;
            const float w = 160f;
            const float h = 24f;

            _labelStyle.normal.textColor = Color.black;
            GUI.Label(new Rect(x - w * 0.5f + 1f, y - h + 1f, w, h), nick, _labelStyle);
            _labelStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(x - w * 0.5f, y - h, w, h), nick, _labelStyle);
        }

        DrawVisibilityIndicator(nm, localId);
    }

    private void DrawVisibilityIndicator(NetworkManager nm, ulong localId)
    {
        var local = nm.LocalClient?.PlayerObject;
        if (local == null) return;
        if (!local.TryGetComponent<NetworkPlayerSetup>(out var setup)) return;

        bool hidden = setup.NicknameHidden.Value;
        _indicatorStyle.normal.textColor = hidden ? new Color(1f, 0.45f, 0.45f) : new Color(0.45f, 1f, 0.45f);
        GUI.Label(
            new Rect(Screen.width - 210f, 8f, 200f, 24f),
            hidden ? "Nickname: hidden  [Shift]" : "Nickname: visible  [Shift]",
            _indicatorStyle);
    }

    private void EnsureStyles()
    {
        if (_labelStyle != null) return;
        _labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
        };
        _indicatorStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            alignment = TextAnchor.MiddleRight,
        };
    }
}
