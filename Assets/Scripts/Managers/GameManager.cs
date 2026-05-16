using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    
    private int _roomsSinceLastBoss;
    private float[] _weightBuffer = System.Array.Empty<float>();

    public List<AIInput> EnemyPrefabs;
    public AIInput BossPrefab;


    private float _score;
    private int _bossesDefeated;

    public PlayerController Player { get; set; }


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
            Destroy(gameObject);
    }

    public void ResetRunState()
    {
        _score = 0;
        _bossesDefeated = 0;
        _roomsSinceLastBoss = 0;
    }

    public void Restart()
    {
        ResetRunState();

        var nm = Unity.Netcode.NetworkManager.Singleton;
        if (nm != null && nm.IsListening)
        {
            var local = nm.LocalClient?.PlayerObject;
            if (local != null && local.TryGetComponent<NetworkPlayerSetup>(out var setup))
            {
                setup.RequestRestartServerRpc();
                return;
            }
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GoToMenu()
    {
        _score = 0;
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;
        SceneManager.LoadScene(0);
    }

    public void AddScore(float value)
    {
        _score += value;
    }

    public void MultiplyScore(float value)
    {
        _score *= value;
    }

    public void BossDefeated()
    {
        _bossesDefeated++;
    }

    private void OnGUI()
    {
        if (BotSpawner.Instance == null) return;
        string scoreText = _score.ToString("F1");
        float textWidth = 200f;
        float textHeight = 30f;
        float x = (Screen.width - textWidth) * 0.5f;
        float y = 10f;

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 24,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };

        GUI.Label(new Rect(x, y, textWidth, textHeight), scoreText, style);

        if (BotSpawner.Instance != null && BotSpawner.Instance.CurrentBoss != null)
        {
            var bossController = BotSpawner.Instance.CurrentBoss;
            float bossHpRatio = bossController.CurrentHp / bossController.MaxHp;

            GUI.color = PlayerController.GetHpFlashColor(bossController.DamageFlash, bossController.HealFlash);
            GUI.DrawTexture(new Rect(0, 0, Screen.width * bossHpRatio, 20f), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        if (_bossesDefeated > 0)
        {
            GUIStyle bossCountStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = Color.white }
            };
            GUI.Label(new Rect(6f, 22f, 160f, 24f), $"Bosses: {_bossesDefeated}", bossCountStyle);
        }
    }

    public AIInput GetRandomEnemy(Vector3Int pos)
    {
        if (EnemyPrefabs == null || EnemyPrefabs.Count == 0)
        {
            Debug.LogError("GameManager has no enemies assigned.", this);
            return null;
        }

        int sum = pos.x + pos.y + pos.z;

        if (_weightBuffer.Length < EnemyPrefabs.Count)
            _weightBuffer = new float[EnemyPrefabs.Count];
        float[] weights = _weightBuffer;

        for (int i = 0; i < weights.Length; i++)
            weights[i] = 1f;

        if (sum >= 5)
            weights[0] = 0.05f;
        else
            weights[0] = 1f;

        if (sum == 0)
            weights[weights.Length - 1] = 0.04f;
        else
            weights[weights.Length - 1] = 1f + sum * 0.2f;

        float totalWeight = 0f;
        foreach (var w in weights)
            totalWeight += w;

        float random = Random.Range(0f, totalWeight);

        float cumulative = 0f;
        for (int i = 0; i < weights.Length; i++)
        {
            cumulative += weights[i];
            if (random <= cumulative)
                return EnemyPrefabs[i];
        }

        return EnemyPrefabs[0];
    }

    public AIInput GetBoss(Vector3Int pos)
    {
        if (_roomsSinceLastBoss < 10)
        {
            _roomsSinceLastBoss++;
            return null;
        }

        float chance = (_roomsSinceLastBoss - 9) * 0.1f;
        if (Random.value >= chance)
        {
            _roomsSinceLastBoss++;
            return null;
        }

        Debug.Log($"BOSS spawned at room {pos}");
        _roomsSinceLastBoss = 0;

        return BossPrefab;
    }
}