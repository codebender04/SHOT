using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CanvasGameplay : UICanvas
{
    [SerializeField] private Image bulletIcon;
    [SerializeField] private TextMeshProUGUI txtRound;
    [SerializeField] private Transform bulletHolder;
    [SerializeField] private TextMeshProUGUI txtBulletCounter;
    [SerializeField] private UIStack moneyStack;
    [SerializeField] private TextMeshProUGUI txtMoneyCounter;

    [Header("Diamonds")]
    [SerializeField] private Image diamondIcon;
    [SerializeField] private RectTransform diamondTray;
    [SerializeField] private TextMeshProUGUI textDiamondCounter;
    [SerializeField] private float insufficientDiamondFlashDuration = 0.15f;
    [SerializeField] private Color insufficientDiamondColor = Color.red;

    [Header("Bullet Animation")]
    [SerializeField] private float bulletSpawnDelay = 0.06f;
    [SerializeField] private float bulletPopDuration = 0.2f;
    [SerializeField] private float bulletPopScale = 1.2f;
    [SerializeField] private float bulletRemoveDuration = 0.12f;

    private int money;
    private int displayedAmmo = -1;
    private int pendingKillMoney;
    private int currentKillChain;

    private void Start()
    {
        Player.Instance.OnAmmoChanged += Player_OnAmmoChanged;
        Enemy.OnKilled += Enemy_OnKilled;
        Bullet.OnFinished += Bullet_OnFinished;
        RoundManager.Instance.OnRoundStart += RoundManager_OnRoundStart;
        DiamondManager.Instance.OnDiamondsChanged += Diamond_OnDiamondsChanged;

        Diamond_OnDiamondsChanged(DiamondManager.Instance.Diamonds);
        Player_OnAmmoChanged(Player.Instance.Ammo);

        txtRound.text = "ROUND 01";

        RefreshMoney();
    }

    private void Enemy_OnKilled(Vector3 position, int killStreak)
    {
        currentKillChain = killStreak;
        pendingKillMoney += killStreak;

        string preview = $"${money}";

        for (int i = 1; i <= currentKillChain; i++)
            preview += $"\n<size=70%>+${i}</size>";

        txtMoneyCounter.text = preview;
    }
    private void Bullet_OnFinished()
    {
        if (pendingKillMoney <= 0)
            return;

        int earnedMoney = pendingKillMoney;

        DOVirtual.DelayedCall(0.25f, () =>
        {
            money += earnedMoney;

            moneyStack.Change(earnedMoney);
            txtMoneyCounter.text = $"${money}";

            pendingKillMoney = 0;
            currentKillChain = 0;
        });
    }
    private void RoundManager_OnRoundStart(int round)
    {
        txtRound.text = $"ROUND {round:D2}";
    }

    private void OnDestroy()
    {
        if (Player.Instance != null)
            Player.Instance.OnAmmoChanged -= Player_OnAmmoChanged;

        Enemy.OnKilled -= Enemy_OnKilled;
        Bullet.OnFinished -= Bullet_OnFinished;

        if (RoundManager.Instance != null)
            RoundManager.Instance.OnRoundStart -= RoundManager_OnRoundStart;

        if (DiamondManager.Instance != null)
            DiamondManager.Instance.OnDiamondsChanged -= Diamond_OnDiamondsChanged;
    }

    private void Diamond_OnDiamondsChanged(int amount)
    {
        int currentDisplayed = diamondTray.childCount;
        int difference = amount - currentDisplayed;
        if (difference > 0)
        {
            for (int i = 0; i < difference; i++)
                CreateDiamond();
        }
        else if (difference < 0)
        {
            for (int i = 0; i < -difference; i++)
            {
                Transform diamond = diamondTray.GetChild(diamondTray.childCount - 1 - i);

                Destroy(diamond.gameObject);
            }
        }

        textDiamondCounter.text = $"{amount}<sprite=0>";
    }

    private void Player_OnAmmoChanged(int ammo)
    {
        txtBulletCounter.text = ammo.ToString();

        if (displayedAmmo < 0)
        {
            displayedAmmo = 0;
            CreateBullets(ammo, true);
            return;
        }

        int difference = ammo - displayedAmmo;

        if (difference == 1)
        {
            CreateBullet(true);
        }
        else if (difference == -1)
        {
            RemoveBullet();
        }
        else if (difference != 0)
        {
            RebuildBullets(ammo);
        }

        displayedAmmo = ammo;
    }

    private void CreateBullets(int amount, bool animate)
    {
        for (int i = 0; i < amount; i++)
            CreateBullet(animate, i * bulletSpawnDelay);
    }

    private void CreateBullet(bool animate, float delay = 0f)
    {
        Image bullet = Instantiate(bulletIcon, bulletHolder);

        if (!animate)
        {
            bullet.transform.localScale = Vector3.one;
            return;
        }

        Transform bulletTransform = bullet.transform;

        bulletTransform.localScale = Vector3.zero;

        bulletTransform
            .DOScale(bulletPopScale, bulletPopDuration * 0.6f)
            .SetDelay(delay)
            .SetEase(Ease.OutBack)
            .OnComplete(() =>
            {
                if (bulletTransform != null)
                {
                    bulletTransform
                        .DOScale(1f, bulletPopDuration * 0.4f)
                        .SetEase(Ease.OutQuad);
                }
            });
    }

    private void RemoveBullet()
    {
        if (bulletHolder.childCount == 0)
            return;

        Transform bullet = bulletHolder.GetChild(
            bulletHolder.childCount - 1
        );

        bullet.DOKill();

        bullet
            .DOScale(0f, bulletRemoveDuration)
            .SetEase(Ease.InBack)
            .OnComplete(() =>
            {
                if (bullet != null)
                    Destroy(bullet.gameObject);
            });
    }

    private void RebuildBullets(int amount)
    {
        foreach (Transform child in bulletHolder)
        {
            child.DOKill();
            Destroy(child.gameObject);
        }

        CreateBullets(amount, true);
    }

    public bool CanAfford(int cost)
    {
        if (money < cost) moneyStack.FlashInsufficient();
        return money >= cost;
    }

    public void ChangeMoney(int value)
    {
        money = Mathf.Max(0, money + value);
        moneyStack.Change(value);
        txtMoneyCounter.text = $"${money}";
    }

    public void SetMoney(int value)
    {
        money = Mathf.Max(0, value);
        moneyStack.Set(value);
        txtMoneyCounter.text = $"${money}";
    }

    public int GetMoney()
    {
        return money;
    }

    private void RefreshMoney()
    {
        moneyStack.Refresh();
    }

    public void SetDiamonds(int amount)
    {
        ClearDiamonds();

        for (int i = 0; i < amount; i++)
            CreateDiamond();
    }

    private void CreateDiamond()
    {
        if (diamondIcon == null || diamondTray == null)
            return;

        Image diamond = Instantiate(diamondIcon, diamondTray);

        RectTransform diamondTransform = diamond.rectTransform;

        Rect rect = diamondTray.rect;

        float halfWidth = diamondTransform.rect.width * 0.5f;
        float halfHeight = diamondTransform.rect.height * 0.5f;

        diamondTransform.anchoredPosition = new Vector2(
            Random.Range(
                rect.xMin + halfWidth,
                rect.xMax - halfWidth
            ),
            Random.Range(
                rect.yMin + halfHeight,
                rect.yMax - halfHeight
            )
        );

        diamondTransform.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                Random.Range(0f, 360f)
            );
    }

    private void RemoveDiamond()
    {
        if (diamondTray == null ||
            diamondTray.childCount == 0)
            return;

        Transform diamond =
            diamondTray.GetChild(
                diamondTray.childCount - 1
            );

        Destroy(diamond.gameObject);
    }

    private void ClearDiamonds()
    {
        if (diamondTray == null)
            return;

        foreach (Transform child in diamondTray)
            Destroy(child.gameObject);
    }
    public void FlashInsufficientDiamonds()
    {
        foreach (Transform child in diamondTray)
        {
            Image image = child.GetComponent<Image>();

            if (image == null)
                continue;

            Color originalColor = image.color;

            image.DOKill();

            image.color = originalColor;

            image.DOColor(
                insufficientDiamondColor,
                insufficientDiamondFlashDuration
            )
            .SetLoops(2, LoopType.Yoyo)
            .SetEase(Ease.OutQuad);
        }
    }
}