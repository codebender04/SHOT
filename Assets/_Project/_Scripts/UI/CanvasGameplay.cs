using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CanvasGameplay : UICanvas
{
    [SerializeField] private Image bulletIcon;
    [SerializeField] private Transform bulletHolder;
    [SerializeField] private TextMeshProUGUI txtBulletCounter;
    [SerializeField] private UIStack moneyStack;
    [SerializeField] private TextMeshProUGUI txtMoneyCounter;

    private int money;

    private void Start()
    {
        Player.Instance.OnAmmoChanged += Player_OnAmmoChanged;
        Enemy.OnKilled += Enemy_OnKilled;

        Player_OnAmmoChanged(Player.Instance.Ammo);
        RefreshMoney();
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
        moneyStack.Add();
    }

    private void Player_OnAmmoChanged(int ammo)
    {
        txtBulletCounter.text = ammo.ToString();

        foreach (Transform t in bulletHolder)
            Destroy(t.gameObject);

        for (int i = 0; i < ammo; i++)
            Instantiate(bulletIcon, bulletHolder);
    }
    public void ChangeMoney(int value)
    {
        money += value;
        moneyStack.Add(value);
        txtMoneyCounter.text = $"${money}";
    }
    private void RefreshMoney()
    {
        moneyStack.Refresh();
    }
}