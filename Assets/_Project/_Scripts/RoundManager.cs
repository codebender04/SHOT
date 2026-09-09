using System;
using System.Collections.Generic;
using UnityEngine;

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
    [SerializeField] private float enemySpawnPadding = 0.5f;
    [SerializeField] private int maxEnemiesPerRound = 20;

    [Header("Collectibles")]
    [SerializeField, Range(0f, 1f)] private float collectibleChance = 0.5f;
    [SerializeField] private List<CollectibleType> collectibleTypes = new();
    [SerializeField] private float collectibleSpawnPadding = 0.5f;

    public event Action<int> OnRoundStart;

    private readonly HashSet<Enemy> activeEnemies = new();

    public int CurrentRound { get; private set; }
    public bool IsRoundActive { get; private set; }

    private void Start()
    {
        Enemy.OnKilled += Enemy_OnKilled;
        Enemy.OnFinishedDeath += Enemy_OnFinishedDeath;

        StartRun();
    }

    private void OnDestroy()
    {
        Enemy.OnKilled -= Enemy_OnKilled;
        Enemy.OnFinishedDeath -= Enemy_OnFinishedDeath;
    }

    public void StartRun()
    {
        CurrentRound = 0;
        activeEnemies.Clear();

        StartNextRound();
    }

    public void StartNextRound()
    {
        if (IsRoundActive)
            return;

        CurrentRound++;
        IsRoundActive = true;

        SpawnRound();

        OnRoundStart?.Invoke(CurrentRound);
    }

    private void SpawnRound()
    {
        int budget = GetEnemyBudget(CurrentRound);

        SpawnEnemies(budget);
        SpawnCollectible();
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

        CollectibleType collectible = GetRandomCollectible();

        if (collectible == null)
            return;

        Vector2 position = ArenaManager.Instance.GetRandomPosition();

        Instantiate(
            collectible.prefab,
            position,
            Quaternion.identity
        );
    }

    private CollectibleType GetRandomCollectible()
    {
        List<CollectibleType> available = new();

        foreach (CollectibleType collectible in collectibleTypes)
        {
            if (collectible.prefab == null)
                continue;

            if (CurrentRound < collectible.unlockRound)
                continue;

            available.Add(collectible);
        }

        if (available.Count == 0)
            return null;

        float totalWeight = 0f;

        foreach (CollectibleType collectible in available)
            totalWeight += collectible.spawnWeight;

        float roll = UnityEngine.Random.value * totalWeight;

        foreach (CollectibleType collectible in available)
        {
            roll -= collectible.spawnWeight;

            if (roll <= 0f)
                return collectible;
        }

        return available[^1];
    }

    public void RegisterEnemy(Enemy enemy)
    {
        if (enemy == null)
            return;

        activeEnemies.Add(enemy);
    }

    private void Enemy_OnKilled(Vector3 position)
    {
        CheckRoundComplete();
    }

    private void Enemy_OnFinishedDeath(Enemy enemy)
    {
        activeEnemies.Remove(enemy);
        CheckRoundComplete();
    }

    public void CheckRoundComplete()
    {
        if (!IsRoundActive)
            return;

        if (activeEnemies.Count > 0)
            return;

        EndRound();
    }

    private void EndRound()
    {
        if (!IsRoundActive)
            return;

        IsRoundActive = false;

        UIManager.Instance.Open<CanvasShop>();
    }
}