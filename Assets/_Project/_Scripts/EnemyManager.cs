using UnityEngine;

public class EnemyManager : Singleton<EnemyManager>
{
    [SerializeField] private ScorePopup scorePopup;

    private void Start()
    {
        Bullet.OnEnemyKilled += Bullet_OnEnemyKilled;
    }

    private void OnDestroy()
    {
        Bullet.OnEnemyKilled -= Bullet_OnEnemyKilled;
    }

    private void Bullet_OnEnemyKilled(Vector3 position, int killStreak)
    {
        int reward = killStreak;

        UIManager.Instance
            .GetCanvas<CanvasGameplay>()
            .ChangeMoney(reward);

        ScorePopup popup = Instantiate(
            scorePopup,
            position,
            Quaternion.identity
        );

        popup.Show(
            position,
            $"+{reward}$",
            killStreak
        );
    }
}
