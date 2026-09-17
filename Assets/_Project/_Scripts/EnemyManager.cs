using UnityEngine;
using UnityEngine.UIElements;

public class EnemyManager : Singleton<EnemyManager>
{
    [SerializeField] private ScorePopup scorePopup;

    private void Start()
    {
        Enemy.OnKilled += Enemy_OnKilled;
    }
    private void OnDestroy()
    {
        Enemy.OnKilled += Enemy_OnKilled;
    }
    private void Enemy_OnKilled(Vector3 position, int killStreak)
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
