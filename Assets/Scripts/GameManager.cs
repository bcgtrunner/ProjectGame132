using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public int RoomsSinceLastBoss = 0;

    public List<AIInput> Enemies;
    public AIInput Boss;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public AIInput GetRandomEnemy(Vector3Int pos)
    {
        int sum = pos.x + pos.y + pos.z;

        float[] weights = new float[Enemies.Count];

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
        if (RoomsSinceLastBoss < 10)
        {
            RoomsSinceLastBoss++;
            return null;
        }

        float chance = (RoomsSinceLastBoss - 9) * 0.1f;
        if (Random.value >= chance)
        {
            RoomsSinceLastBoss++;
            return null;
        }

        Debug.Log($"BOSS spawned at room {pos}");
        RoomsSinceLastBoss = 0;

        return Boss;
    }
}