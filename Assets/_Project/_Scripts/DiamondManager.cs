using System;
using UnityEngine;

public class DiamondManager : Singleton<DiamondManager>
{
    public event Action<int> OnDiamondsChanged;

    public int Diamonds { get; private set; }

    private void Awake()
    {
        Load();
    }

    public void AddDiamonds(int amount)
    {
        if (amount <= 0)
            return;

        Diamonds += amount;

        Save();

        OnDiamondsChanged?.Invoke(Diamonds);
    }

    public bool SpendDiamonds(int amount)
    {
        if (amount <= 0 || Diamonds < amount)
            return false;

        Diamonds -= amount;

        Save();

        OnDiamondsChanged?.Invoke(Diamonds);

        return true;
    }

    private void Save()
    {
        PlayerPrefs.SetInt("Diamonds", Diamonds);
        PlayerPrefs.Save();
    }

    private void Load()
    {
        Diamonds = PlayerPrefs.GetInt("Diamonds", 0);
        OnDiamondsChanged?.Invoke(Diamonds);
    }
}