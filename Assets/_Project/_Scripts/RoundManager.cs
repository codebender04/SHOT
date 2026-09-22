using System;
using System.Collections.Generic;
using UnityEngine;
using static PremiumUpgrade;

public class RoundManager : Singleton<RoundManager>
{
    [Serializable]
    private class EnemyType
    {
        public Enemy prefab;
        public int cost = 1;
        public int unlockRound = 1;
        [Range(0f, 1f)] public float spawnWeight = 1f;
    }

    [Serializable]
    private class CollectibleType
    {
        public Collectible prefab;
        public int unlockRound = 1;
        [Range(0f, 1f)] public float spawnWeight = 1f;
    }

    [Header("Rounds")]
    [SerializeField] private int startingEnemyBudget = 3;
    [SerializeField] private int enemyBudgetIncrease = 2;
    [SerializeField] private float budgetGrowth = 0.1f;

    [Header("Enemies")]
    [SerializeField] private List<EnemyType> enemyTypes = new();
    [SerializeField] private int maxEnemiesPerRound = 20;

    [Header("Collectibles")]
    [SerializeField, Range(0f, 1f)] private float collectibleChance = 0.5f;
    [SerializeField] private List<CollectibleType> collectibleTypes = new();
    [SerializeField] private float collectibleSpawnPadding = 0.5f;

    [Header("Obstacles")]
    [SerializeField] private GameObject obstaclePrefab;
    [SerializeField, Range(0f, 1f)] private float obstacleBaseChance = 0.2f;
    [SerializeField, Range(0f, 1f)] private float obstacleChanceIncreasePerRound = 0.05f;
    [SerializeField, Range(0f, 1f)] private float obstacleMaxChance = 1f;
    [SerializeField] private int obstacleBaseMaxCount = 1;
    [SerializeField] private int obstacleCountIncreaseEveryRounds = 5;
    [SerializeField] private int obstacleMaxCount = 6;
    [SerializeField] private float obstacleSpawnPadding = 1f;
    [SerializeField] private int obstaclePositionAttempts = 20;
    [SerializeField] private LayerMask obstacleBlockedLayers;

    public event Action<int> OnRoundStart;
    public event Action<int> OnRunStart;

    private readonly HashSet<Enemy> activeEnemies = new();
    private readonly List<Collectible> activeCollectibles = new();
    private readonly List<GameObject> activeObstacles = new();

    public int HighestRound { get; private set; }
    public int CurrentRound { get; private set; }
    public bool IsRoundActive { get; private set; }
    public int HighestKillStreak => highestKillStreak;

    private int highestKillStreak;

    private void Start()
    {
        Enemy.OnKilled += Enemy_OnKilled;
        Enemy.OnFinishedDeath += Enemy_OnFinishedDeath;
    }

    private void OnDestroy()
    {
        Enemy.OnKilled -= Enemy_OnKilled;
        Enemy.OnFinishedDeath -= Enemy_OnFinishedDeath;
    }
    private void Enemy_OnKilled(Vector3 position, int killStreak)
    {
        highestKillStreak = Mathf.Max(highestKillStreak, killStreak);
        CheckRoundComplete();
    }
    private void Enemy_OnFinishedDeath(Enemy enemy)
    {
        activeEnemies.Remove(enemy);
        CheckRoundComplete();
    }

    public void StartRun()
    {
        IsRoundActive = false;
        CurrentRound = 0;
        highestKillStreak = 0;

        foreach (Enemy enemy in activeEnemies)
        {
            if (enemy != null)
                Destroy(enemy.gameObject);
        }

        activeEnemies.Clear();

        foreach (Collectible collectible in activeCollectibles)
        {
            if (collectible != null)
                Destroy(collectible.gameObject);
        }

        activeCollectibles.Clear();

        foreach (GameObject obstacle in activeObstacles)
        {
            if (obstacle != null)
                Destroy(obstacle);
        }

        activeObstacles.Clear();

        Player.Instance.ResetPlayer();
        UIManager.Instance.GetCanvas<CanvasShop>().ResetPrice();
        GameManager.Instance.SetState(GameState.Playing);
        OnRunStart?.Invoke(CurrentRound);

        StartNextRound();
    }
    public void StartNextRound()
    {
        if (IsRoundActive)
            return;

        if (Player.Instance.Ammo <= 0)
        {
            GameOver();
            return;
        }

        if (PremiumUpgradeManager.Instance.HasUpgrade(UpgradeType.Interest))
        {
            CanvasGameplay gameplay = UIManager.Instance.GetCanvas<CanvasGameplay>();
            int interest = gameplay.GetMoney() / 5;
            if (interest > 0) gameplay.ChangeMoney(interest);
        }
        if (PremiumUpgradeManager.Instance.HasUpgrade(UpgradeType.EmergencyAmmo) && Player.Instance.Ammo == 1)
        {
            Player.Instance.ChangeAmmo(1);
        }

        CurrentRound++;
        HighestRound = Mathf.Max(HighestRound, CurrentRound);

        IsRoundActive = true;

        SpawnRound();

        OnRoundStart?.Invoke(CurrentRound);
    }
    private void SpawnRound()
    {
        int budget = GetEnemyBudget(CurrentRound);

        SpawnEnemies(budget);
        SpawnCollectible();
        SpawnObstacles();
    }

    private int GetEnemyBudget(int round)
    {
        float growth = Mathf.Pow(1f + budgetGrowth, round - 1);

        return Mathf.Max(
            startingEnemyBudget,
            Mathf.RoundToInt(
                startingEnemyBudget * growth +
                (round - 1) * enemyBudgetIncrease
            )
        );
    }

    private void SpawnEnemies(int budget)
    {
        int spawned = 0;

        while (budget > 0 && spawned < maxEnemiesPerRound)
        {
            EnemyType enemyType = GetRandomEnemy(budget);

            if (enemyType == null)
                break;

            Vector2 position = ArenaManager.Instance.GetRandomPosition();

            Enemy enemy = Instantiate(
                enemyType.prefab,
                position,
                Quaternion.identity
            );

            RegisterEnemy(enemy);

            budget -= enemyType.cost;
            spawned++;
        }
    }

    private EnemyType GetRandomEnemy(int budget)
    {
        List<EnemyType> available = new();

        foreach (EnemyType enemyType in enemyTypes)
        {
            if (enemyType.prefab == null)
                continue;

            if (CurrentRound < enemyType.unlockRound)
                continue;

            if (enemyType.cost > budget)
                continue;

            if (enemyType.spawnWeight <= 0f)
                continue;

            available.Add(enemyType);
        }

        if (available.Count == 0)
            return null;

        float totalWeight = 0f;

        foreach (EnemyType enemyType in available)
            totalWeight += enemyType.spawnWeight;

        float roll = UnityEngine.Random.value * totalWeight;

        foreach (EnemyType enemyType in available)
        {
            roll -= enemyType.spawnWeight;

            if (roll <= 0f)
                return enemyType;
        }

        return available[^1];
    }

    private void SpawnCollectible()
    {
        if (UnityEngine.Random.value > collectibleChance)
            return;

        CollectibleType collectibleType = GetRandomCollectible();

        if (collectibleType == null)
            return;

        Vector2 position = ArenaManager.Instance.GetRandomPosition();

        Collectible collectible = Instantiate(
            collectibleType.prefab,
            position,
            Quaternion.identity
        );

        activeCollectibles.Add(collectible);
    }

    private CollectibleType GetRandomCollectible()
    {
        List<CollectibleType> available = new();

        foreach (CollectibleType collectibleType in collectibleTypes)
        {
            if (collectibleType.prefab == null)
                continue;

            if (CurrentRound < collectibleType.unlockRound)
                continue;

            if (collectibleType.spawnWeight <= 0f)
                continue;

            available.Add(collectibleType);
        }

        if (available.Count == 0)
            return null;

        float totalWeight = 0f;

        foreach (CollectibleType collectibleType in available)
            totalWeight += collectibleType.spawnWeight;

        float roll = UnityEngine.Random.value * totalWeight;

        foreach (CollectibleType collectibleType in available)
        {
            roll -= collectibleType.spawnWeight;

            if (roll <= 0f)
                return collectibleType;
        }

        return available[^1];
    }

    private void SpawnObstacles()
    {
        if (obstaclePrefab == null)
            return;

        float chance = Mathf.Min(
            obstacleBaseChance +
            (CurrentRound - 1) * obstacleChanceIncreasePerRound,
            obstacleMaxChance
        );

        if (UnityEngine.Random.value > chance)
            return;

        int maxCount = Mathf.Min(
            obstacleBaseMaxCount +
            (CurrentRound - 1) / Mathf.Max(1, obstacleCountIncreaseEveryRounds),
            obstacleMaxCount
        );

        int count = UnityEngine.Random.Range(1, maxCount + 1);

        for (int i = 0; i < count; i++)
        {
            if (TryGetObstaclePosition(out Vector2 position))
            {
                float rotation = UnityEngine.Random.Range(0f, 360f);

                GameObject obstacle = Instantiate(
                    obstaclePrefab,
                    position,
                    Quaternion.Euler(0f, 0f, rotation)
                );

                activeObstacles.Add(obstacle);
            }
        }
    }

    private bool TryGetObstaclePosition(out Vector2 position)
    {
        for (int i = 0; i < obstaclePositionAttempts; i++)
        {
            Vector2 candidate = ArenaManager.Instance.GetRandomPosition();

            Collider2D blockedCollider = Physics2D.OverlapCircle(
                candidate,
                obstacleSpawnPadding,
                obstacleBlockedLayers
            );

            if (blockedCollider != null)
                continue;

            position = candidate;
            return true;
        }

        position = Vector2.zero;
        return false;
    }

    public void RegisterEnemy(Enemy enemy)
    {
        if (enemy == null)
            return;

        activeEnemies.Add(enemy);
    }

    public void CheckRoundComplete()
    {
        if (!IsRoundActive)
            return;

        foreach (Enemy enemy in activeEnemies)
        {
            if (enemy != null && !enemy.IsDead)
                return;
        }

        EndRound();
    }

    private void EndRound()
    {
        if (!IsRoundActive)
            return;

        IsRoundActive = false;

        UIManager.Instance.Open<CanvasShop>();
    }
    public void CheckRunLost()
    {
        if (!IsRoundActive)
            return;

        if (Player.Instance.Ammo > 0)
        {
            return;
        }

        foreach (Enemy enemy in activeEnemies)
        {
            if (enemy != null && !enemy.IsDead)
            {
                GameOver();
                return;
            }
        }
    }
    private int GetDiamondReward(int round)
    {
        int completedRounds = Mathf.Max(0, round - 1);
        int total = 0;

        for (int i = 1; i <= completedRounds; i++)
            total += Mathf.CeilToInt(i / 5f);

        return total;
    }
    private void GameOver()
    {
        IsRoundActive = false;

        int money = UIManager.Instance.GetCanvas<CanvasGameplay>().GetMoney();

        int diamondReward = GetDiamondReward(CurrentRound);

        DiamondManager.Instance.AddDiamonds(diamondReward);

        UIManager.Instance.Open<CanvasGameOver>().SetStats(
            Player.Instance.AmmoUsed,
            Player.Instance.TotalBounces,
            money,
            HighestKillStreak,
            CurrentRound,
            diamondReward
        );
    }
}
