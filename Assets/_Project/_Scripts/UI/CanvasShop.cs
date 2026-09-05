using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class CanvasShop : UICanvas
{
    [Header("References")]
    [SerializeField] private RectTransform panel;
    [SerializeField] private Button btnNextRound;

    [Header("Animation")]
    [SerializeField] private float slideDistance = 600f;
    [SerializeField] private float slideDuration = 0.3f;

    private Vector2 targetPosition;
    private Sequence sequence;

    private void Awake()
    {
        targetPosition = panel.anchoredPosition;

        btnNextRound.onClick.AddListener(OnNextRound);
        CloseImmediate();
    }

    private void OnEnable()
    {
        PlayOpenAnimation();
        GameInput.Instance.BlockShootUntilReleased();
    }

    private void OnDisable()
    {
        sequence?.Kill();
    }

    private void PlayOpenAnimation()
    {
        sequence?.Kill();

        panel.anchoredPosition =
            targetPosition + Vector2.left * slideDistance;

        sequence = DOTween.Sequence();

        sequence.Append(
            panel.DOAnchorPos(
                targetPosition,
                slideDuration
            ).SetEase(Ease.OutCubic)
        );

        sequence.SetUpdate(true);
    }
    private void OnNextRound()
    {
        sequence?.Kill();

        sequence = DOTween.Sequence();

        sequence.Append(
            panel.DOAnchorPos(
                targetPosition + Vector2.left * slideDistance,
                slideDuration
            ).SetEase(Ease.InCubic)
        );

        sequence.SetUpdate(true);

        sequence.OnComplete(() =>
        {
            gameObject.SetActive(false);
            RoundManager.Instance.StartNextRound();
        });
    }
}