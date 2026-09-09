using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    public GameState State { get; private set; }

    public bool IsPlaying => State == GameState.Playing;
    public bool IsShop => State == GameState.Shop;

    public void SetState(GameState state)
    {
        State = state;
    }
    public void PauseGame()
    {
        SetState(GameState.Shop);
    }
}
public enum GameState
{
    Playing,
    Shop
}