using DG.Tweening;
using TMPro;
using UnityEngine;

public class PremiumTooltip : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform panel;
    [SerializeField] private TextMeshProUGUI txtName;
    [SerializeField] private TextMeshProUGUI txtDescription;
    [SerializeField] private TextMeshProUGUI txtCost;
    [SerializeField] private TextMeshProUGUI txtUnlockedAt;

    [Header("Animation")]
    [SerializeField] private float startScale = 0.85f;
    [SerializeField] private float startRotation = -5f;
    [SerializeField] private float animationDuration = 0.15f;

    private Vector3 defaultScale;
    private Quaternion defaultRotation;
    private Sequence sequence;

    private void Awake()
    {
        defaultScale = panel.localScale;
        defaultRotation = panel.localRotation;

        gameObject.SetActive(false);
    }

    public void Show(PremiumUpgrade upgrade)
    {
        if (upgrade == null)
            return;

        sequence?.Kill();

        txtName.text = upgrade.upgradeName;
        txtDescription.text = upgrade.description;
        txtCost.text = $"Cost: {upgrade.cost}<sprite=0>";
        txtUnlockedAt.text = $"Unlocked At:\nRound {upgrade.unlockRound:D2}";

        gameObject.SetActive(true);

        panel.localScale = defaultScale * startScale;

        panel.localRotation =
            defaultRotation *
            Quaternion.Euler(0f, 0f, startRotation);

        sequence = DOTween.Sequence();

        sequence.Join(
            panel.DOScale(
                defaultScale,
                animationDuration
            ).SetEase(Ease.OutBack)
        );

        sequence.Join(
            panel.DOLocalRotateQuaternion(
                defaultRotation,
                animationDuration
            ).SetEase(Ease.OutQuad)
        );

        sequence.SetUpdate(true);
    }

    public void Hide()
    {
        sequence?.Kill();
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        sequence?.Kill();
    }
}