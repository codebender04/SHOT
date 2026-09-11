using UnityEngine;

public class SpawnerEnemy : Enemy
{
    [Header("Spawn")]
    [SerializeField] private Enemy enemyPrefab;

    private Enemy spawnedEnemy;

    private void Start()
    {
        PrepareSpawn();
    }

    private void PrepareSpawn()
    {
        if (enemyPrefab == null)
            return;

        spawnedEnemy = Instantiate(enemyPrefab, transform.position, Quaternion.identity);

        spawnedEnemy.gameObject.SetActive(false);

        RoundManager.Instance.RegisterEnemy(spawnedEnemy);
    }

    protected override void OnDeathStarted()
    {
        if (spawnedEnemy == null)
            return;

        Invoke(nameof(EnableSpawnedEnemy), 0.2f);
    }
    private void EnableSpawnedEnemy()
    {
        spawnedEnemy.transform.position = transform.position;
        spawnedEnemy.gameObject.SetActive(true);
    }

    public override void FinishDeath()
    {
        base.FinishDeath();
    }

    private void OnDestroy()
    {
        if (spawnedEnemy != null && !spawnedEnemy.gameObject.activeSelf)
        {
            Destroy(spawnedEnemy.gameObject);
        }
    }
}