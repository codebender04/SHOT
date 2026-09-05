using UnityEngine;

public class UICanvas : MonoBehaviour
{
    [SerializeField] private bool destroyOnClose = false;

    public virtual void Setup()
    {

    }
    public virtual void Open()
    {
        gameObject.SetActive(true);
    }
    public virtual void Close(float time)
    {
        Invoke(nameof(CloseImmediate), time);
    }
    public virtual void CloseImmediate()
    {
        if (destroyOnClose)
        {
            Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

}
