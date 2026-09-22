using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class CanvasPremiumShop : UICanvas
{
    [Header("References")]
    [SerializeField] private Image background;
    [SerializeField] private RectTransform panel;
    [SerializeField] private Button btnStart;

    [Header("Upgrade Items")]
    [SerializeField] private PremiumUpgradeItem[] upgradeItems;

    [Header("Animation")]
    [SerializeField] private float backgroundFadeDuration = 0.4f;
    [SerializeField] private float slideDistance = 600f;
    [SerializeField] private float slideDuration = 0.4f;

    private Vector2 targetPosition;
    private Color backgroundTargetColor;
    private Sequence sequence;

    private void Awake()
    {
        targetPosition = panel.anchoredPosition;
        backgroundTargetColor = background.color;

        btnStart.onClick.AddListener(OnStart);

        CloseImmediate();
    }

    private void OnEnable()
    {
        SetupUpgradeItems();
        PlayOpenAnimation();
    }
    private void SetupUpgradeItems()
    {
        if (PremiumUpgradeManager.Instance == null)
            return;

        var upgrades = PremiumUpgradeManager.Instance.Upgrades;

        int highestRound = RoundManager.Instance.HighestRound;
        int upcomingCount = 0;

        for (int i = 0; i < upgradeItems.Length; i++)
        {
            PremiumUpgradeItem item = upgradeItems[i];

            if (item == null)
                continue;

            if (i >= upgrades.Count)
                continue;

            PremiumUpgrade upgrade = upgrades[i];

            bool unlocked = highestRound >= upgrade.unlockRound;
            bool upcoming = false;

            if (!unlocked && upcomingCount < 2)
            {
                upcoming = true;
                upcomingCount++;
            }

            item.gameObject.SetActive(true);

            item.Setup(upgrade, unlocked, upcoming);
        }
    }
    private void PlayOpenAnimation()
    {
        sequence?.Kill();

        panel.anchoredPosition =
            targetPosition + Vector2.up * slideDistance;

        sequence = DOTween.Sequence();

        sequence.Append(
            panel.DOAnchorPos(
                targetPosition,
                slideDuration
            ).SetEase(Ease.OutBack)
        );

        sequence.SetUpdate(true);
    }

    private void OnStart()
    {
        sequence?.Kill();

        sequence = DOTween.Sequence();

        sequence.Append(
            background.DOFade(
                backgroundTargetColor.a,
                backgroundFadeDuration
            ).SetEase(Ease.OutQuad)
        );

        sequence.Join(
            panel.DOAnchorPos(
                targetPosition + Vector2.up * slideDistance,
                slideDuration
            ).SetEase(Ease.InCubic)
        );

        sequence.SetUpdate(true);

        sequence.OnComplete(() =>
        {
            CloseImmediate();
            RoundManager.Instance.StartRun();
        });
    }

    private void OnDisable()
    {
        sequence?.Kill();
    }
}