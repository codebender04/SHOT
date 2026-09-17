using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public static event Action<Vector3, int> OnKilled;
    public static event Action<Enemy> OnFinishedDeath;

    private static int killStreak;
    private static readonly List<Enemy> pendingDeaths = new();

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

        animator.SetTrigger(DieHash);
        DisableHitbox();

        killStreak++;

        pendingDeaths.Add(this);

        OnKilled?.Invoke(
            transform.position,
            killStreak
        );

        OnDeathStarted();
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
        pendingDeaths.Clear();
    }

    public static void FinishShot()
    {
        for (int i = 0; i < pendingDeaths.Count; i++)
        {
            Enemy enemy = pendingDeaths[i];

            if (enemy != null)
                enemy.FinishDeath();
        }

        pendingDeaths.Clear();
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