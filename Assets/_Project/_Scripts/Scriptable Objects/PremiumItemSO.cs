using UnityEngine;

[CreateAssetMenu(fileName = "PremiumUpgrade", menuName = "Game/Premium Upgrade SO")]
public class PremiumUpgrade : ScriptableObject
{
    public enum UpgradeType
    {
        TrajectoryPreview,
        BiggerBullet,
        Interest,
        EmergencyAmmo,
        WASDMovement,
        RecoilDamage,
        DualGun,
        LuckyShop,
    }
    [Header("Upgrade")]
    public UpgradeType type;
    public string upgradeName;
    [TextArea]
    public string description;
    public int cost;
    public int unlockRound;
}