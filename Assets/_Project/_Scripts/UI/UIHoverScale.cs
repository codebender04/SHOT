using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class UIHoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private float hoverScale = 1.2f;
    [SerializeField] private float duration = 0.25f;

    private RectTransform rectTransform;
    private Vector3 normalScale;
    private Tween scaleTween;

    private void Awake()
    {
        rectTransform = transform as RectTransform;
        normalScale = rectTransform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        scaleTween?.Kill();

        scaleTween = rectTransform
            .DOScale(normalScale * hoverScale, duration)
            .SetEase(Ease.InOutElastic);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        scaleTween?.Kill();

        scaleTween = rectTransform
            .DOScale(normalScale, duration)
            .SetEase(Ease.InOutElastic);
    }

    private void OnDestroy()
    {
        scaleTween?.Kill();
    }
}