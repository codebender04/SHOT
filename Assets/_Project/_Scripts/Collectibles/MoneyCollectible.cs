using UnityEngine;

public class MoneyCollectible : Collectible
{
    [SerializeField] private int value = 1;

    protected override void OnCollected()
    {
        UIManager.Instance.GetCanvas<CanvasGameplay>().ChangeMoney(value);
    }
}