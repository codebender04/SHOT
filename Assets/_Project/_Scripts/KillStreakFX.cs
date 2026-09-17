using UnityEngine;

public class KillStreakFX : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ParticleSystem killStreakFXLeft;
    [SerializeField] private ParticleSystem killStreakFXRight;

    [Header("Emission")]
    [SerializeField] private int minStreak = 2;
    [SerializeField] private int maxStreak = 10;
    [SerializeField] private float minEmission = 3f;
    [SerializeField] private float maxEmission = 50f;

    private ParticleSystem.EmissionModule leftEmission;
    private ParticleSystem.EmissionModule rightEmission;

    private void Awake()
    {
        Enemy.OnKilled += Enemy_OnKilled;
        RoundManager.Instance.OnRoundStart += RoundManager_OnRoundStart;

        if (killStreakFXLeft != null)
            leftEmission = killStreakFXLeft.emission;

        if (killStreakFXRight != null)
            rightEmission = killStreakFXRight.emission;

        ResetFX();
    }

    private void Enemy_OnKilled(Vector3 position, int killStreak)
    {

        if (killStreak < minStreak)
            return;

        float t = Mathf.InverseLerp(
            minStreak,
            maxStreak,
            killStreak
        );

        float emissionRate = Mathf.Lerp(
            minEmission,
            maxEmission,
            t
        );

        leftEmission.rateOverTime = emissionRate;
        rightEmission.rateOverTime = emissionRate;

        killStreakFXLeft?.Play();
        killStreakFXRight?.Play();
    }

    private void OnDestroy()
    {
        Enemy.OnKilled -= Enemy_OnKilled;

        if (RoundManager.Instance != null)
            RoundManager.Instance.OnRoundStart -= RoundManager_OnRoundStart;
    }

    private void RoundManager_OnRoundStart(int round)
    {
        ResetFX();
    }
    private void ResetFX()
    {
        leftEmission.rateOverTime = 0f;
        rightEmission.rateOverTime = 0f;

        // Let existing particles naturally finish.
        killStreakFXLeft?.Stop(
            true,
            ParticleSystemStopBehavior.StopEmitting
        );

        killStreakFXRight?.Stop(
            true,
            ParticleSystemStopBehavior.StopEmitting
        );
    }
}
