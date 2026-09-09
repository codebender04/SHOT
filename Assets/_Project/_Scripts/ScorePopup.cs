using DG.Tweening;
using TMPro;
using UnityEngine;

public class ScorePopup : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private TextMeshProUGUI chainText;

    [Header("Animation")]
    [SerializeField] private float duration = 0.35f;
    [SerializeField] private float moveDistance = 0.5f;

    [Header("Scale")]
    [SerializeField] private float baseScale = 1f;
    [SerializeField] private float maxScale = 1.5f;
    [SerializeField] private int streakForMaxScale = 10;

    [Header("Punch")]
    [SerializeField] private float basePunch = 1.2f;
    [SerializeField] private float maxPunch = 1.45f;

    [Header("Chain Color")]
    [SerializeField] private Color chainStartColor = Color.white;
    [SerializeField] private Color chainEndColor = new Color(1f, 0.2f, 0.05f);
    [SerializeField] private int streakForMaxColor = 10;

    private Sequence sequence;

    public void Show(Vector3 position, string message, int killStreak)
    {
        sequence?.Kill();

        transform.position = position;
        transform.localScale = Vector3.zero;

        text.text = message;

        Color textColor = text.color;
        textColor.a = 1f;
        text.color = textColor;

        bool showChain = killStreak >= 2;

        chainText.gameObject.SetActive(showChain);

        if (showChain)
        {
            chainText.text = $"CHAIN x{killStreak}";

            float colorT = Mathf.InverseLerp(
                2,
                streakForMaxColor,
                killStreak
            );

            chainText.color = Color.Lerp(
                chainStartColor,
                chainEndColor,
                colorT
            );
        }

        float scaleT = Mathf.InverseLerp(
            1,
            streakForMaxScale,
            killStreak
        );

        float targetScale = Mathf.Lerp(
            baseScale,
            maxScale,
            scaleT
        );

        float punchScale = Mathf.Lerp(
            basePunch,
            maxPunch,
            scaleT
        );

        sequence = DOTween.Sequence();

        sequence.Append(
            transform.DOScale(
                targetScale * punchScale,
                0.08f
            ).SetEase(Ease.OutBack)
        );

        sequence.Append(
            transform.DOScale(
                targetScale,
                0.06f
            ).SetEase(Ease.OutQuad)
        );

        sequence.Join(
            transform.DOMoveY(
                position.y + moveDistance,
                duration
            ).SetEase(Ease.OutQuad)
        );

        sequence.Join(
            text.DOFade(0f, 0.15f)
                .SetDelay(0.2f)
                .SetEase(Ease.InQuad)
        );

        if (showChain)
        {
            sequence.Join(
                chainText.DOFade(0f, 0.15f)
                    .SetDelay(0.25f)
                    .SetEase(Ease.InQuad)
            );
        }

        sequence.OnComplete(() =>
        {
            gameObject.SetActive(false);
        });
    }

    private void OnDestroy()
    {
        sequence?.Kill();
    }
}