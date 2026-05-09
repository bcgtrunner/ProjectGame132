using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    private int _roomsSinceLastBoss;
    private float[] _weightBuffer = System.Array.Empty<float>();

    public List<AIInput> Enemies;
    public AIInput Boss;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void Restart()
    {
        SceneManager.LoadScene(1);
    }

    public void GoToMenu()
    {
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;
        SceneManager.LoadScene(0);
    }
        
    public AIInput GetRandomEnemy(Vector3Int pos)
    {
        if (Enemies == null || Enemies.Count == 0)
        {
            Debug.LogError("GameManager has no enemies assigned.", this);
            return null;
        }

        int sum = pos.x + pos.y + pos.z;

        if (_weightBuffer.Length < Enemies.Count)
            _weightBuffer = new float[Enemies.Count];
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
                return Enemies[i];
        }

        return Enemies[0];
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

        return Boss;
    }
}