using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(TrailRenderer))]
public class BulletFireEffect : MonoBehaviour
{
    [Header("Streak Thresholds")]
    [SerializeField] private int streakToIgnite = 3;
    [SerializeField] private int streakForFullBlaze = 8;
    [SerializeField] private int streakForWhiteHot = 15;

    [Header("Feel")]
    [SerializeField] private float rampDuration = 0.25f;
    [SerializeField] private Ease rampEase = Ease.OutQuad;

    private TrailRenderer fireTrail;
    private MaterialPropertyBlock propertyBlock;
    private Tween intensityTween;
    private Tween heatTween;

    private float currentIntensity;
    private float currentHeat;

    private static readonly int IntensityID = Shader.PropertyToID("_Intensity");
    private static readonly int HeatID = Shader.PropertyToID("_Heat");

    private void Awake()
    {
        fireTrail = GetComponent<TrailRenderer>();
        propertyBlock = new MaterialPropertyBlock();
    }

    /// <summary>Call when a bullet is (re)initialized so it starts unlit.</summary>
    public void ResetFire()
    {
        intensityTween?.Kill();
        heatTween?.Kill();
        currentIntensity = 0f;
        currentHeat = 0f;
        Apply();
    }

    /// <summary>Call whenever the bullet's kill streak changes.</summary>
    public void SetKillStreak(int streak)
    {
        float targetIntensity = Mathf.InverseLerp(streakToIgnite, streakForFullBlaze, streak);
        float targetHeat = Mathf.InverseLerp(streakForFullBlaze, streakForWhiteHot, streak);
        fireTrail.emitting = targetIntensity > 0f;

        intensityTween?.Kill();
        heatTween?.Kill();

        intensityTween = DOTween.To(
            () => currentIntensity,
            value => { currentIntensity = value; Apply(); },
            targetIntensity,
            rampDuration
        ).SetEase(rampEase);

        heatTween = DOTween.To(
            () => currentHeat,
            value => { currentHeat = value; Apply(); },
            targetHeat,
            rampDuration
        ).SetEase(rampEase);
    }

    private void Apply()
    {
        fireTrail.GetPropertyBlock(propertyBlock);
        propertyBlock.SetFloat(IntensityID, currentIntensity);
        propertyBlock.SetFloat(HeatID, currentHeat);
        fireTrail.SetPropertyBlock(propertyBlock);
    }

    private void OnDestroy()
    {
        intensityTween?.Kill();
        heatTween?.Kill();
    }
}