using UnityEngine;

public class BulletCollectible : Collectible
{
    [SerializeField] private int value = 1;

    protected override void OnCollected()
    {
        Player.Instance.ChangeAmmo(value);
    }
}
