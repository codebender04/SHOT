using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static PremiumUpgrade;

public class CanvasShop : UICanvas
{
    [Header("References")]
    [SerializeField] private RectTransform panel;
    [SerializeField] private Button btnNextRound;
    [SerializeField] private Button btnBuyBounce;
    [SerializeField] private Button btnBuyBounce2;
    [SerializeField] private TextMeshProUGUI txtBouncePrice;

    [SerializeField] private Button btnBuyAmmo;
    [SerializeField] private Button btnBuyAmmo2;
    [SerializeField] private TextMeshProUGUI txtAmmoPrice;

    [Header("Shop Deal")]
    [SerializeField, Range(0f, 1f)] private float specialDealChance = 0.6f;
    [SerializeField, Range(0f, 1f)] private float freeDealChance = 0.5f;

    private enum DealType
    {
        None,
        Free,
        HalfPrice
    }

    private DealType ammoDealType = DealType.None;
    private DealType bounceDealType = DealType.None;

    private int normalAmmoPrice;
    private int normalBouncePrice;

    [Header("Bounce")]
    [SerializeField] private int baseBouncePrice = 1;
    [SerializeField] private int bouncePriceIncrease = 2;

    [Header("Ammo")]
    [SerializeField] private int baseAmmoPrice = 3;
    [SerializeField] private int ammoPriceIncrease = 3;

    [Header("Animation")]
    [SerializeField] private float slideDistance = 600f;
    [SerializeField] private float slideDuration = 0.3f;

    private int bouncePrice;
    private int ammoPrice;
    private Vector2 targetPosition;
    private Sequence sequence;

    private void Awake()
    {
        bouncePrice = baseBouncePrice;
        ammoPrice = baseAmmoPrice;

        normalBouncePrice = bouncePrice;
        normalAmmoPrice = ammoPrice;

        targetPosition = panel.anchoredPosition;

        btnNextRound.onClick.AddListener(OnNextRound);

        btnBuyBounce.onClick.AddListener(BuyBounce);
        btnBuyBounce2.onClick.AddListener(BuyBounce);

        btnBuyAmmo.onClick.AddListener(BuyAmmo);
        btnBuyAmmo2.onClick.AddListener(BuyAmmo);

        RefreshPriceTexts();

        CloseImmediate();
    }

    private void OnEnable()
    {
        PlayOpenAnimation();

        if (PremiumUpgradeManager.Instance.HasUpgrade(UpgradeType.LuckyShop))
            SetupShopDeal();
        else
            ResetDeals();

        GameManager.Instance.SetState(GameState.Pausing);
        GameInput.Instance.BlockShootUntilReleased();
    }

    private void ResetDeals()
    {
        ammoDealType = DealType.None;
        bounceDealType = DealType.None;

        normalAmmoPrice = ammoPrice;
        normalBouncePrice = bouncePrice;

        RefreshPriceTexts();
    }

    private void OnDisable()
    {
        sequence?.Kill();
    }

    private void SetupShopDeal()
    {
        normalAmmoPrice = ammoPrice;
        normalBouncePrice = bouncePrice;

        ammoDealType = DealType.None;
        bounceDealType = DealType.None;

        if (UnityEngine.Random.value <= specialDealChance)
        {
            ammoDealType =
                UnityEngine.Random.value <= freeDealChance
                    ? DealType.Free
                    : DealType.HalfPrice;
        }

        if (UnityEngine.Random.value <= specialDealChance)
        {
            bounceDealType =
                UnityEngine.Random.value <= freeDealChance
                    ? DealType.Free
                    : DealType.HalfPrice;
        }

        RefreshPriceTexts();
    }

    private void RefreshPriceTexts()
    {
        if (txtAmmoPrice != null)
        {
            txtAmmoPrice.text = GetPriceText(
                ammoDealType,
                normalAmmoPrice
            );
        }

        if (txtBouncePrice != null)
        {
            txtBouncePrice.text = GetPriceText(
                bounceDealType,
                normalBouncePrice
            );
        }
    }

    private string GetPriceText(
        DealType dealType,
        int normalPrice)
    {
        if (dealType == DealType.Free)
            return $"<color=#666666><s>${normalPrice}</s></color> FREE!";

        if (dealType == DealType.HalfPrice)
        {
            int discountedPrice =
                Mathf.CeilToInt(normalPrice * 0.5f);

            return $"<color=#666666><s>${normalPrice}</s></color> ${discountedPrice}";
        }

        return $"${normalPrice}";
    }

    private int GetPurchasePrice(
        DealType dealType,
        int normalPrice)
    {
        if (dealType == DealType.Free)
            return 0;

        if (dealType == DealType.HalfPrice)
            return Mathf.CeilToInt(normalPrice * 0.5f);

        return normalPrice;
    }

    private void BuyBounce()
    {
        int price = GetPurchasePrice(
            bounceDealType,
            bouncePrice
        );

        Buy(
            price,
            () => Player.Instance.IncreaseMaxBounces(),
            () =>
            {
                bouncePrice += bouncePriceIncrease;
                normalBouncePrice = bouncePrice;

                bounceDealType = DealType.None;

                RefreshPriceTexts();
            }
        );
    }

    private void BuyAmmo()
    {
        int price = GetPurchasePrice(
            ammoDealType,
            ammoPrice
        );

        Buy(
            price,
            () => Player.Instance.ChangeAmmo(1),
            () =>
            {
                ammoPrice += ammoPriceIncrease;
                normalAmmoPrice = ammoPrice;

                ammoDealType = DealType.None;

                RefreshPriceTexts();
            }
        );
    }

    private void Buy(
        int price,
        System.Action purchase,
        System.Action increasePrice)
    {
        if (!UIManager.Instance
            .GetCanvas<CanvasGameplay>()
            .CanAfford(price))
        {
            return;
        }

        UIManager.Instance
            .GetCanvas<CanvasGameplay>()
            .ChangeMoney(-price);

        purchase();
        increasePrice();
    }

    private void PlayOpenAnimation()
    {
        sequence?.Kill();

        panel.anchoredPosition =
            targetPosition +
            Vector2.left * slideDistance;

        sequence = DOTween.Sequence();

        sequence.Append(
            panel.DOAnchorPos(
                targetPosition,
                slideDuration
            ).SetEase(Ease.OutBack)
        );

        sequence.SetUpdate(true);
    }

    private void OnNextRound()
    {
        sequence?.Kill();

        sequence = DOTween.Sequence();

        sequence.Append(
            panel.DOAnchorPos(
                targetPosition +
                Vector2.left * slideDistance,
                slideDuration
            ).SetEase(Ease.InCubic)
        );

        sequence.SetUpdate(true);

        sequence.OnComplete(() =>
        {
            gameObject.SetActive(false);
            RoundManager.Instance.StartNextRound();
            GameManager.Instance.SetState(GameState.Playing);
        });
    }

    public void ResetPrice()
    {
        bouncePrice = baseBouncePrice;
        ammoPrice = baseAmmoPrice;

        normalBouncePrice = bouncePrice;
        normalAmmoPrice = ammoPrice;

        ammoDealType = DealType.None;
        bounceDealType = DealType.None;

        RefreshPriceTexts();
    }
}