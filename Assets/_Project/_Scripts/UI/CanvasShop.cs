using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

        targetPosition = panel.anchoredPosition;

        btnNextRound.onClick.AddListener(OnNextRound);

        btnBuyBounce.onClick.AddListener(BuyBounce);
        btnBuyBounce2.onClick.AddListener(BuyBounce);

        btnBuyAmmo.onClick.AddListener(BuyAmmo);
        btnBuyAmmo2.onClick.AddListener(BuyAmmo);

        txtBouncePrice.text = $"${bouncePrice}";
        txtAmmoPrice.text = $"${ammoPrice}";
        CloseImmediate();
    }

    private void OnEnable()
    {
        PlayOpenAnimation();

        GameManager.Instance.SetState(GameState.Shop);
        GameInput.Instance.BlockShootUntilReleased();
    }

    private void OnDisable()
    {
        GameManager.Instance.SetState(GameState.Playing);
        sequence?.Kill();
    }

    private void BuyBounce()
    {
        Buy(
            bouncePrice,
            () => Player.Instance.IncreaseMaxBounces(),
            () =>
            {
                bouncePrice += bouncePriceIncrease;
                txtBouncePrice.text = $"${bouncePrice}";
            }
        );
    }

    private void BuyAmmo()
    {
        Buy(
            ammoPrice,
            () => Player.Instance.ChangeAmmo(1),
            () =>
            {
                ammoPrice += ammoPriceIncrease;
                txtAmmoPrice.text = $"${ammoPrice}";
            }

        );
    }

    private void Buy(int price, System.Action purchase, System.Action increasePrice)
    {
        if (!UIManager.Instance.GetCanvas<CanvasGameplay>().CanAfford(price))
            return;

        UIManager.Instance.GetCanvas<CanvasGameplay>().ChangeMoney(-price);

        purchase();
        increasePrice();
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
    public void ResetPrice()
    {
        bouncePrice = baseBouncePrice;
        ammoPrice = baseAmmoPrice;

        txtBouncePrice.text = $"${bouncePrice}";
        txtAmmoPrice.text = $"${ammoPrice}";
    }
}