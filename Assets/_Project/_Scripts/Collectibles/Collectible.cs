using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.UIElements;

public abstract class Collectible : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private float bobHeight = 0.08f;
    [SerializeField] private float bobDuration = 0.6f;
    [SerializeField] private SpriteRenderer visual;

    [Header("Collect Animation")]
    [Tooltip("Total duration multiplier for the collection sequence.")]
    [SerializeField] private float collectScale = 1.3f;

    private bool collected;
    private Sequence bobSequence;
    private Sequence collectSequence;

    private Vector3 basePosition;
    private Vector3 baseScale;

    protected virtual void Awake()
    {
        basePosition = transform.localPosition;
        baseScale = transform.localScale;
    }

    protected virtual void OnEnable()
    {
        collected = false;
        StartBob();
    }

    protected virtual void OnDisable()
    {
        bobSequence?.Kill();
        collectSequence?.Kill();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected)
            return;

        if (!other.TryGetComponent<Player>(out _))
            return;

        Collect();
    }

    private void StartBob()
    {
        bobSequence?.Kill();

        transform.localPosition = basePosition;

        bobSequence = DOTween.Sequence();

        bobSequence.Append(
            transform.DOLocalMoveY(
                basePosition.y + bobHeight,
                bobDuration
            ).SetEase(Ease.InOutSine)
        );

        bobSequence.Append(
            transform.DOLocalMoveY(
                basePosition.y,
                bobDuration
            ).SetEase(Ease.InOutSine)
        );

        bobSequence.SetLoops(-1);
    }

    private void Collect()
    {
        if (collected)
            return;

        collected = true;

        bobSequence?.Kill();

        OnCollected();

        PlayCollectAnimation();
    }
    private void PlayCollectAnimation()
    {
        collectSequence?.Kill();

        transform.localPosition = basePosition;
        transform.localScale = baseScale;

        collectSequence = DOTween.Sequence();

        collectSequence.Append(
            transform.DOScale(
                baseScale * collectScale,
                0.1f
            ).SetEase(Ease.OutBack)
        );

        collectSequence.Join(
            transform.DOLocalMoveY(
                basePosition.y + 0.2f,
                0.12f
            ).SetEase(Ease.OutQuad)
        );

        collectSequence.AppendInterval(0.2f);

        collectSequence.Append(
            transform.DOLocalMoveY(
                basePosition.y,
                0.15f
            ).SetEase(Ease.InQuad)
        );

        collectSequence.Join(
            transform.DOScale(
                Vector3.zero,
                0.15f
            ).SetEase(Ease.InBack)
        );

        collectSequence.OnComplete(() =>
        {
            Destroy(gameObject);
        });
    }
    protected abstract void OnCollected();
}