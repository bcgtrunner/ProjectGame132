using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class BotSpawner : MonoBehaviour
{
    [SerializeField] private WorldGenerator _worldGenerator;
    [SerializeField] private AIInput _botPrefab;
    [SerializeField] private PlayerController _botTarget;
    [SerializeField] private int _botsPerBox = 10;
    [SerializeField] private float _deathPaintScale = 1f;
    [SerializeField] private int _deathBurstFrames = 100;

    private readonly List<AIInput> _bots = new();
    private readonly HashSet<Vector3Int> _filledBoxes = new();
    
    private bool _allBotsCleared;
    private PlayerController _currentBoss;
    public PlayerController CurrentBoss => _currentBoss;

    public static BotSpawner Instance;


    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(Instance);

        _worldGenerator.BoxOpened += FillBox;
        _worldGenerator.WallAboutToBeDestroyed += KillCharactersAttachedToWall;
    }

    private void OnDestroy()
    {
        _worldGenerator.BoxOpened -= FillBox;
        _worldGenerator.WallAboutToBeDestroyed -= KillCharactersAttachedToWall;
    }

    protected virtual int GetBotCount(Vector3Int pos)
    {
        return (Mathf.Abs(pos.x) + Mathf.Abs(pos.y) + Mathf.Abs(pos.z)) * _botsPerBox + 1;
    }

    protected virtual float GetBotHealth(Vector3Int pos)
    {
        int manhattan = Mathf.Abs(pos.x) + Mathf.Abs(pos.y) + Mathf.Abs(pos.z);
        return manhattan * 0.5f + 0.1f;
    }

    private static float GetBossDifficulty()
    {
        int defeated = GameManager.Instance != null ? GameManager.Instance.BossesDefeated : 0;
        return 0.35f + 0.45f * defeated;
    }

    public void ResetState()
    {
        StopAllCoroutines();
        for (int i = _bots.Count - 1; i >= 0; i--)
        {
            if (_bots[i] == null) continue;
            _bots[i].Destroyed -= HandleBotDestroyed;
            Destroy(_bots[i].gameObject);
        }
        _bots.Clear();
        _filledBoxes.Clear();
        _currentBoss = null;
        _allBotsCleared = false;
    }

    private void FillBox(Vector3Int pos, Vector3 openingDirection)
    {
        if (_filledBoxes.Contains(pos)) return;

        if (_botPrefab == null)
        {
            Debug.LogWarning($"BotSpawner cannot fill box {pos} because Bot Prefab is not assigned.", this);
            return;
        }

        CleanupMissingBots();

        var bossPrefab = _currentBoss == null ? GameManager.Instance.GetBoss(pos) : null;
        if (bossPrefab != null)
        {
            AIInput boss = Instantiate(bossPrefab, GetBotSpawnPosition(pos, 0), Quaternion.identity);
            boss.Target = _botTarget;
            var contr = boss.GetComponent<PlayerController>();
            float difficulty = GetBossDifficulty();
            contr.SetMaxHp(GetBotHealth(pos) * contr.MaxHp * difficulty);
            if (boss.TryGetComponent<SweepAttack>(out var sweep))
                sweep.ApplyDifficulty(difficulty);
            boss.Destroyed += HandleBotDestroyed;
            _currentBoss = contr;
            _bots.Add(boss);
            boss.SetVirtualAttachment(WorldGenerator.GetSpawnSurfaceNormal(openingDirection));
            boss.LaunchImmediately();
            return;
        }

        int botCount = GetBotCount(pos);
        _allBotsCleared = false;
        for (int i = 0; i < botCount; i++)
        {
            var prefab = GameManager.Instance.GetRandomEnemy(pos);
            AIInput bot = Instantiate(prefab, GetBotSpawnPosition(pos, i), Quaternion.identity);
            bot.Target = _botTarget;
            bot.GetComponent<PlayerController>().SetMaxHp(GetBotHealth(pos));
            bot.Destroyed += HandleBotDestroyed;
            _bots.Add(bot);
            bot.SetVirtualAttachment(WorldGenerator.GetSpawnSurfaceNormal(openingDirection));
            bot.LaunchImmediately();
        }

        _filledBoxes.Add(pos);
    }

    private void KillCharactersAttachedToWall(Wall wall)
    {
        Collider wallCollider = wall.GetComponent<Collider>();
        if (wallCollider == null) return;

        for (int i = _bots.Count - 1; i >= 0; i--)
        {
            if (_bots[i] == null) continue;

            PlayerController controller = _bots[i].GetComponent<PlayerController>();
            if (controller != null && controller.AttachedWallCollider == wallCollider && !controller.LaunchOnWallDestroyed)
                Destroy(_bots[i].gameObject);
        }

        if (_botTarget != null && _botTarget.AttachedWallCollider == wallCollider
            && !_botTarget.LaunchOnWallDestroyed && !_botTarget.IsImmune)
            Destroy(_botTarget.gameObject);
    }

    private void HandleBotDestroyed(AIInput bot)
    {
        if (bot == null) return;

        bot.Destroyed -= HandleBotDestroyed;

        PlayerController botController = bot.GetComponent<PlayerController>();
        if (botController == _currentBoss)
        {
            _currentBoss = null;
            if (GameManager.Instance != null) GameManager.Instance.BossDefeated();
        }

        if (botController != null)
            GameManager.Instance.AddScore(botController.MaxHp);

        _bots.Remove(bot);
        if (_bots.Count == 0 && !_allBotsCleared)
        {
            _allBotsCleared = true;
            GameManager.Instance.MultiplyScore(1.5f);

            if (_botTarget != null)
            {
                PaintShooter playerShooter = _botTarget.GetComponent<PaintShooter>();
                if (playerShooter != null)
                    StartCoroutine(DeathBurstCoroutine(playerShooter, _botTarget, 10, _deathPaintScale));
            }
        }
    }

    private IEnumerator DeathBurstCoroutine(PaintShooter shooter, PlayerController player, int shotsPerFrame, float scale)
    {
        for (int frame = 0; frame < _deathBurstFrames; frame++)
        {
            Vector3? surfaceNormal = player.HasSurfaceNormal ? player.CurrentSurfaceNormal : (Vector3?)null;
            shooter.ShootDeathBurst(shotsPerFrame, scale, surfaceNormal);
            yield return null;
        }
    }

    private Vector3 GetBotSpawnPosition(Vector3Int boxPos, int botIndex)
    {
        float inset = GetBotHalfHeight() + 1f;
        float halfRoom = WorldGenerator.Scale * 0.5f - inset;
        Vector3 center = new Vector3(
            boxPos.x * WorldGenerator.Scale,
            boxPos.y * WorldGenerator.Scale,
            boxPos.z * WorldGenerator.Scale);
        if (halfRoom <= 0f) return center;
        return center + new Vector3(
            Random.Range(-halfRoom, halfRoom),
            Random.Range(-halfRoom, halfRoom),
            Random.Range(-halfRoom, halfRoom));
    }

    private float GetBotHalfHeight()
    {
        if (_botPrefab.TryGetComponent<BoxCollider>(out BoxCollider boxCollider))
            return Mathf.Max(boxCollider.size.y * Mathf.Abs(_botPrefab.transform.localScale.y) * 0.5f, 0.5f);

        if (_botPrefab.TryGetComponent<SphereCollider>(out SphereCollider sphereCollider))
            return Mathf.Max(sphereCollider.radius * Mathf.Abs(_botPrefab.transform.localScale.y), 0.5f);

        if (_botPrefab.TryGetComponent<CapsuleCollider>(out CapsuleCollider capsuleCollider))
            return Mathf.Max(capsuleCollider.height * Mathf.Abs(_botPrefab.transform.localScale.y) * 0.5f, 0.5f);

        return 0.5f;
    }

    private void CleanupMissingBots()
    {
        for (int i = _bots.Count - 1; i >= 0; i--)
        {
            if (_bots[i] == null)
                _bots.RemoveAt(i);
        }
    }
}
