using UnityEngine;

public class SpawnerEnemy : Enemy
{
    [Header("Spawn")]
    [SerializeField] private Enemy enemyPrefab;

    protected override void OnDeathStarted()
    {
        if (enemyPrefab == null)
            return;


        Invoke(nameof(SpawnEnemy), 0.2f);
    }
    private void SpawnEnemy()
    {
        Enemy spawnedEnemy = Instantiate(enemyPrefab, transform.position, Quaternion.identity);
        RoundManager.Instance.RegisterEnemy(spawnedEnemy);
    }
}
