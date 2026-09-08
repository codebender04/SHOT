using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class RoundManager : Singleton<RoundManager>
{
    public event Action<int> OnRoundStart;
    [Header("References")]
    [SerializeField] private Enemy enemyPrefab;
    [SerializeField] private BoxCollider2D arenaBounds;

    [Header("Round")]
    [SerializeField] private int startingEnemies = 3;
    [SerializeField] private int enemiesPerRound = 1;

    [Header("Spawn")]
    [SerializeField] private float spawnPadding = 0.5f;
    [SerializeField] private float minDistanceFromPlayer = 2f;
    [SerializeField] private int maxSpawnAttempts = 50;

    private readonly List<Enemy> activeEnemies = new();

    private int currentRound;
    private bool roundActive;
    private bool waitingForBullet;

    public int CurrentRound => currentRound;

    private void OnEnable()
    {
        Enemy.OnKilled += Enemy_OnKilled;
        Bullet.OnFinished += Bullet_OnFinished;
    }

    private void OnDisable()
    {
        Enemy.OnKilled -= Enemy_OnKilled;
        Bullet.OnFinished -= Bullet_OnFinished;
    }

    private void Start()
    {
        GameManager.Instance.SetState(GameState.Playing);
        StartRound(1);
    }

    public void StartNextRound()
    {
        StartRound(currentRound + 1);
    }

    private void StartRound(int round)
    {
        currentRound = round;
        roundActive = true;
        waitingForBullet = false;

        GameManager.Instance.SetState(GameState.Playing);

        Time.timeScale = 1f;

        SpawnEnemies(GetEnemyCount(currentRound));
        OnRoundStart?.Invoke(currentRound);
    }

    private int GetEnemyCount(int round)
    {
        return startingEnemies + (round - 1) * enemiesPerRound;
    }

    private void SpawnEnemies(int count)
    {
        activeEnemies.Clear();

        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPosition = GetSpawnPosition();

            Enemy enemy = Instantiate(
                enemyPrefab,
                spawnPosition,
                Quaternion.identity
            );

            activeEnemies.Add(enemy);
        }
    }

    private Vector3 GetSpawnPosition()
    {
        Bounds bounds = arenaBounds.bounds;

        for (int i = 0; i < maxSpawnAttempts; i++)
        {
            float x = Random.Range(
                bounds.min.x + spawnPadding,
                bounds.max.x - spawnPadding
            );

            float y = Random.Range(
                bounds.min.y + spawnPadding,
                bounds.max.y - spawnPadding
            );

            Vector3 position = new Vector3(x, y, 0f);

            if (Player.Instance == null)
                return position;

            if (Vector2.Distance(
                    position,
                    Player.Instance.transform.position) >= minDistanceFromPlayer)
            {
                return position;
            }
        }

        return bounds.center;
    }

    private void Enemy_OnKilled(Vector3 position)
    {
        if (!roundActive)
            return;

        activeEnemies.RemoveAll(enemy =>
            enemy == null || enemy.IsDead
        );

        if (activeEnemies.Count > 0)
            return;

        if (Bullet.ActiveCount > 0)
        {
            waitingForBullet = true;
            return;
        }

        CompleteRound();
    }

    private void Bullet_OnFinished()
    {
        if (!roundActive || !waitingForBullet)
            return;

        waitingForBullet = false;

        CompleteRound();
    }
    public void RegisterEnemy(Enemy enemy)
    {
        if (enemy == null)
            return;

        if (!activeEnemies.Contains(enemy))
            activeEnemies.Add(enemy);
    }

    private void CompleteRound()
    {
        if (!roundActive)
            return;

        roundActive = false;
        GameManager.Instance.SetState(GameState.Shop);

        Invoke(nameof(OpenShop), 0.5f);
    }
    private void OpenShop()
    {
        Time.timeScale = 0f;
        UIManager.Instance.GetCanvas<CanvasShop>().Open();
    }
}