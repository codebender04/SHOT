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

    [Header("Bullet Animation")]
    [SerializeField] private float bulletSpawnDelay = 0.06f;
    [SerializeField] private float bulletPopDuration = 0.2f;
    [SerializeField] private float bulletPopScale = 1.2f;
    [SerializeField] private float bulletRemoveDuration = 0.12f;

    private int money;
    private int displayedAmmo = -1;

    private void Start()
    {
        Player.Instance.OnAmmoChanged += Player_OnAmmoChanged;
        Enemy.OnKilled += Enemy_OnKilled;
        RoundManager.Instance.OnRoundStart += RoundManager_OnRoundStart;

        Player_OnAmmoChanged(Player.Instance.Ammo);
        txtRound.text = "ROUND 01";
        RefreshMoney();
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

        if (RoundManager.Instance != null)
            RoundManager.Instance.OnRoundStart -= RoundManager_OnRoundStart;
    }

    private void Enemy_OnKilled(Vector3 position)
    {
        money++;
        txtMoneyCounter.text = $"${money}";
        moneyStack.Change(1);
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
}