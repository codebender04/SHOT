using TMPro;
using Unity.VisualScripting;
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

    private int money;

    private void Start()
    {
        Player.Instance.OnAmmoChanged += Player_OnAmmoChanged;
        Enemy.OnKilled += Enemy_OnKilled;
        RoundManager.Instance.OnRoundStart += RoundManager_OnRoundStart;

        Player_OnAmmoChanged(Player.Instance.Ammo);
        txtRound.text = $"ROUND 01";
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

        foreach (Transform t in bulletHolder)
            Destroy(t.gameObject);

        for (int i = 0; i < ammo; i++)
            Instantiate(bulletIcon, bulletHolder);
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
    public int GetMoney()
    {
        return money;
    }
    private void RefreshMoney()
    {
        moneyStack.Refresh();
    }
}