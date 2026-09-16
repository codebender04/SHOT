using System.Collections.Generic;
using UnityEngine;

public class PremiumUpgradeManager : Singleton<PremiumUpgradeManager>
{
    [SerializeField] private List<PremiumUpgrade> upgrades = new();

    private readonly HashSet<PremiumUpgrade> purchasedUpgrades = new();

    public IReadOnlyList<PremiumUpgrade> Upgrades => upgrades;

    public bool HasUpgrade(PremiumUpgrade.UpgradeType type)
    {
        foreach (PremiumUpgrade upgrade in upgrades)
        {
            if (upgrade.type == type && purchasedUpgrades.Contains(upgrade))
            {
                return true;
            }
        }

        return false;
    }

    public bool Purchase(PremiumUpgrade upgrade)
    {
        if (upgrade == null)
            return false;

        if (purchasedUpgrades.Contains(upgrade))
            return false;

        if (!DiamondManager.Instance.SpendDiamonds(upgrade.cost))
            return false;

        purchasedUpgrades.Add(upgrade);

        return true;
    }

    public bool CanAfford(int cost)
    {
        return DiamondManager.Instance.Diamonds >= cost;
    }

    public List<PremiumUpgrade> GetAvailableUpgrades(int round)
    {
        List<PremiumUpgrade> available = new();

        foreach (PremiumUpgrade upgrade in upgrades)
        {
            if (upgrade == null)
                continue;

            if (purchasedUpgrades.Contains(upgrade))
                continue;

            if (round < upgrade.unlockRound)
                continue;

            available.Add(upgrade);
        }

        return available;
    }
}