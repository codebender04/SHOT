using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    public GameState State { get; private set; }

    public bool IsPlaying => State == GameState.Playing;
    public bool IsShop => State == GameState.Pausing;

    public void SetState(GameState state)
    {
        State = state;
    }
}
public enum GameState
{
    Playing,
    Pausing
}