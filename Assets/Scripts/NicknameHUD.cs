using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NicknameHUD : MonoBehaviour
{
    private GUIStyle _labelStyle;
    private GUIStyle _indicatorStyle;
    private GUIStyle _leaderboardHeaderStyle;
    private GUIStyle _leaderboardRowStyle;
    private GUIStyle _leaderboardRowLocalStyle;

    private readonly List<LeaderboardEntry> _leaderboardBuffer = new();

    private struct LeaderboardEntry
    {
        public string Nick;
        public int Kills;
        public int Deaths;
        public bool IsLocal;
    }

    private void OnGUI()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsListening) return;

        var cam = Camera.main;
        EnsureStyles();

        ulong localId = nm.LocalClientId;

        if (cam != null)
        {
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
        }

        DrawVisibilityIndicator(nm, localId);
        DrawLeaderboard(nm, localId);
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

    private void DrawLeaderboard(NetworkManager nm, ulong localId)
    {
        _leaderboardBuffer.Clear();
        foreach (var client in nm.ConnectedClientsList)
        {
            var playerObj = client.PlayerObject;
            if (playerObj == null) continue;
            if (!playerObj.TryGetComponent<NetworkPlayerSetup>(out var setup)) continue;

            string nick = setup.Nickname.Value.ToString();
            if (string.IsNullOrEmpty(nick)) nick = $"Player {client.ClientId}";

            _leaderboardBuffer.Add(new LeaderboardEntry
            {
                Nick = nick,
                Kills = setup.Kills.Value,
                Deaths = setup.Deaths.Value,
                IsLocal = client.ClientId == localId,
            });
        }
        if (_leaderboardBuffer.Count == 0) return;

        _leaderboardBuffer.Sort((a, b) =>
        {
            int byKills = b.Kills.CompareTo(a.Kills);
            if (byKills != 0) return byKills;
            return a.Deaths.CompareTo(b.Deaths);
        });

        const float panelWidth = 260f;
        const float rowHeight = 18f;
        const float headerHeight = 22f;
        const float padding = 6f;
        float panelHeight = headerHeight + rowHeight * _leaderboardBuffer.Count + padding * 2f;
        float x = Screen.width - panelWidth - 10f;
        float y = 40f;

        GUI.color = new Color(0f, 0f, 0f, 0.55f);
        GUI.DrawTexture(new Rect(x, y, panelWidth, panelHeight), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float nameCol = x + 8f;
        float kCol = x + panelWidth - 150f;
        float dCol = x + panelWidth - 110f;
        float kdCol = x + panelWidth - 70f;
        float nameWidth = kCol - nameCol - 4f;

        GUI.Label(new Rect(nameCol, y + padding, nameWidth, headerHeight), "Player", _leaderboardHeaderStyle);
        GUI.Label(new Rect(kCol, y + padding, 40f, headerHeight), "K", _leaderboardHeaderStyle);
        GUI.Label(new Rect(dCol, y + padding, 40f, headerHeight), "D", _leaderboardHeaderStyle);
        GUI.Label(new Rect(kdCol, y + padding, 60f, headerHeight), "K/D", _leaderboardHeaderStyle);

        float rowY = y + padding + headerHeight;
        foreach (var entry in _leaderboardBuffer)
        {
            var style = entry.IsLocal ? _leaderboardRowLocalStyle : _leaderboardRowStyle;
            GUI.Label(new Rect(nameCol, rowY, nameWidth, rowHeight), entry.Nick, style);
            GUI.Label(new Rect(kCol, rowY, 40f, rowHeight), entry.Kills.ToString(), style);
            GUI.Label(new Rect(dCol, rowY, 40f, rowHeight), entry.Deaths.ToString(), style);
            string kd = entry.Deaths > 0
                ? (entry.Kills / (float)entry.Deaths).ToString("F2")
                : entry.Kills.ToString("F2");
            GUI.Label(new Rect(kdCol, rowY, 60f, rowHeight), kd, style);
            rowY += rowHeight;
        }
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
        _leaderboardHeaderStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = new Color(0.85f, 0.85f, 0.95f) },
        };
        _leaderboardRowStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = Color.white },
        };
        _leaderboardRowLocalStyle = new GUIStyle(_leaderboardRowStyle)
        {
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(1f, 0.9f, 0.4f) },
        };
    }
}
