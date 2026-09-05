using DG.Tweening;
using TMPro;
using UnityEngine;

public class ScorePopup : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI text;

    [Header("Animation")]
    [SerializeField] private float duration = 0.35f;
    [SerializeField] private float moveDistance = 0.5f;
    [SerializeField] private float punchScale = 1.2f;

    private Sequence sequence;

    public void Show(Vector3 position, string message)
    {
        sequence?.Kill();

        transform.position = position;
        text.text = message;

        // Reset state in case this object is reused.
        transform.localScale = Vector3.zero;

        Color color = text.color;
        color.a = 1f;
        text.color = color;

        sequence = DOTween.Sequence();

        // Snap in.
        sequence.Append(
            transform.DOScale(punchScale, 0.08f)
                .SetEase(Ease.OutBack)
        );

        // Settle.
        sequence.Append(
            transform.DOScale(1f, 0.06f)
                .SetEase(Ease.OutQuad)
        );

        // Float upward while fading.
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
