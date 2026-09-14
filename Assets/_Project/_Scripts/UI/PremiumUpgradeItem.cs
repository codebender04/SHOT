using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PremiumUpgradeItem : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("References")]
    [SerializeField] private Button button;
    [SerializeField] private GameObject purchasedOverlay;
    [SerializeField] private GameObject upcomingOverlay;
    [SerializeField] private GameObject lockedOverlay;

    [Header("Tooltip")]
    [SerializeField] private PremiumTooltip tooltip;

    [Header("Animation")]
    [SerializeField] private float hoverScale = 1.08f;
    [SerializeField] private float animationDuration = 0.12f;

    private PremiumUpgrade upgrade;
    private Tween scaleTween;
    private bool canHover;
    private bool isUnlocked;

    public void Setup(PremiumUpgrade premiumUpgrade, bool unlocked, bool upcoming)
    {
        upgrade = premiumUpgrade;
        isUnlocked = unlocked;
        canHover = unlocked || upcoming;

        button.onClick.RemoveAllListeners();

        if (unlocked)
            button.onClick.AddListener(Purchase);

        if (purchasedOverlay != null)
            purchasedOverlay.SetActive(false);

        if (upcomingOverlay != null)
            upcomingOverlay.SetActive(upcoming);

        if (lockedOverlay != null)
            lockedOverlay.SetActive(!unlocked && !upcoming);

        Refresh();
    }

    public void Refresh()
    {
        if (upgrade == null)
            return;

        bool purchased =
            PremiumUpgradeManager.Instance.HasUpgrade(
                upgrade.type
            );

        if (purchasedOverlay != null)
            purchasedOverlay.SetActive(purchased);

        button.interactable = isUnlocked && !purchased;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!canHover || upgrade == null)
            return;

        scaleTween?.Kill();

        scaleTween = transform
            .DOScale(
                hoverScale,
                animationDuration
            )
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);

        tooltip?.Show(upgrade);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!canHover)
            return;

        scaleTween?.Kill();

        scaleTween = transform
            .DOScale(
                Vector3.one,
                animationDuration
            )
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);

        tooltip?.Hide();
    }

    private void Purchase()
    {
        if (upgrade == null || !isUnlocked)
            return;

        if (!PremiumUpgradeManager.Instance.Purchase(upgrade))
            return;

        tooltip?.Hide();

        Refresh();
    }

    private void OnDestroy()
    {
        scaleTween?.Kill();
    }
}