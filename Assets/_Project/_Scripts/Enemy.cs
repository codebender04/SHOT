using System;
using DG.Tweening;
using UnityEngine;
using Random = UnityEngine.Random;

public class Enemy : MonoBehaviour
{
    public static event Action<Vector3, int> OnKilled;
    public static event Action<Enemy> OnFinishedDeath;

    private static int killStreak;

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer visual;
    [SerializeField] private Collider2D hitCollider;

    [Header("Health")]
    [SerializeField] private int health = 1;

    [Header("Death")]
    [SerializeField] private float fadeDuration = 0.2f;
    [SerializeField] private float deathScale = 0.8f;

    private bool isDead;

    public bool IsDead => isDead;
    private void Awake()
    {
        if (visual == null)
            return;

        visual.DOKill();

        Vector3 targetPosition = visual.transform.localPosition;

        visual.transform.localPosition = targetPosition + Vector3.up * 0.2f;

        Color color = visual.color;
        color.a = 0f;
        visual.color = color;

        float delay = Random.Range(0f, 0.5f);

        Sequence sequence = DOTween.Sequence();

        sequence.AppendInterval(delay);

        sequence.Join(
            visual.transform.DOLocalMove(targetPosition, 0.4f)
                .SetEase(Ease.OutCubic)
        );

        sequence.Join(
            visual.DOFade(1f, 0.4f)
                .SetEase(Ease.OutCubic)
        );
    }

    public virtual void TakeHit()
    {
        if (isDead)
            return;

        health--;

        if (health <= 0)
            Die();
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;

        DisableHitbox();

        killStreak++;

        OnKilled?.Invoke(
            transform.position,
            killStreak
        );

        OnDeathStarted();

        animator.SetTrigger(DieHash);

        FinishDeath();
    }

    protected virtual void OnDeathStarted()
    {
    }

    private void DisableHitbox()
    {
        if (hitCollider != null)
            hitCollider.enabled = false;
    }

    public static void StartShot()
    {
        killStreak = 0;
    }

    public virtual void FinishDeath()
    {
        Sequence sequence = DOTween.Sequence();

        sequence.Join(
            visual.DOFade(
                0f,
                fadeDuration
            )
        );

        sequence.Join(
            transform
                .DOScale(
                    deathScale,
                    fadeDuration
                )
                .SetEase(Ease.InQuad)
        );

        sequence.OnComplete(() =>
        {
            OnFinishedDeath?.Invoke(this);
            Destroy(gameObject);
        });
    }

    private static readonly int DieHash =
        Animator.StringToHash("Die");
}