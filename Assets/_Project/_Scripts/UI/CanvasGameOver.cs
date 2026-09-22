using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CanvasGameOver : UICanvas
{
    [Header("References")]
    [SerializeField] private Button btnPremiumShop;
    [SerializeField] private TextMeshProUGUI txtAmmoUsed;
    [SerializeField] private TextMeshProUGUI txtNoOfBounce;
    [SerializeField] private TextMeshProUGUI txtMoney;
    [SerializeField] private TextMeshProUGUI txtHighestKillStreak;
    [SerializeField] private TextMeshProUGUI txtRound;
    [SerializeField] private TextMeshProUGUI txtDiamond;
    [SerializeField] private Image background;
    [SerializeField] private RectTransform panel;

    [Header("Animation")]
    [SerializeField] private float backgroundFadeDuration = 0.4f;
    [SerializeField] private float panelSlideDistance = 800f;
    [SerializeField] private float panelSlideDuration = 0.5f;

    private Vector2 panelTargetPosition;
    private Color backgroundTargetColor;
    private Sequence sequence;

    private void Awake()
    {
        panelTargetPosition = panel.anchoredPosition;
        backgroundTargetColor = background.color;

        btnPremiumShop.onClick.AddListener(OnPremiumShop);

        CloseImmediate();
    }

    private void OnEnable()
    {
        PlayOpenAnimation();
    }

    private void OnDisable()
    {
        sequence?.Kill();
    }

    private void PlayOpenAnimation()
    {
        sequence?.Kill();

        Color backgroundColor = backgroundTargetColor;
        backgroundColor.a = 0f;
        background.color = backgroundColor;

        panel.anchoredPosition =
            panelTargetPosition +
            Vector2.down * panelSlideDistance;

        sequence = DOTween.Sequence();

        sequence.Append(
            background.DOFade(
                backgroundTargetColor.a,
                backgroundFadeDuration
            ).SetEase(Ease.OutQuad)
        );

        sequence.Join(
            panel.DOAnchorPos(
                panelTargetPosition,
                panelSlideDuration
            ).SetEase(Ease.OutBack)
        );

        sequence.SetUpdate(true);
    }

    private void OnPremiumShop()
    {
        SoundManager.Instance.PlaySuccessUIClick();
        CloseImmediate();
        UIManager.Instance.Open<CanvasPremiumShop>();
    }
    public void SetStats(
        int ammoUsed,
        int noOfBounce,
        int money,
        int highestKillStreak,
        int round,
        int diamondReward)
    {
        txtAmmoUsed.text = ammoUsed.ToString();
        txtNoOfBounce.text = noOfBounce.ToString();
        txtMoney.text = $"${money}";
        txtHighestKillStreak.text = $"{highestKillStreak}";
        txtRound.text = $"{round:00}";
        txtDiamond.text = $"+{diamondReward}<sprite=0>";
    }
}