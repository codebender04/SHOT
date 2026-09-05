using UnityEngine;

public class EnemyManager : Singleton<EnemyManager>
{
    [SerializeField] private ScorePopup scorePopup;
    private void Start()
    {
        Enemy.OnKilled += Enemy_OnKilled;
    }

    private void Enemy_OnKilled(Vector3 position)
    {
        ScorePopup popup = Instantiate(scorePopup, position, Quaternion.identity);

        popup.Show(position, "+1$");
    }
}
