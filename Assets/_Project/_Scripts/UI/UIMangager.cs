using System.Collections.Generic;
using UnityEngine;

public class UIManager : Singleton<UIManager>
{
    Dictionary<System.Type, UICanvas> canvases = new Dictionary<System.Type, UICanvas>();

    private void Awake()
    {
        // Find all UICanvas components in the scene and cache them
        UICanvas[] uiCanvases = FindObjectsByType<UICanvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < uiCanvases.Length; i++)
        {
            canvases.Add(uiCanvases[i].GetType(), uiCanvases[i]);
        }
    }

    public T Open<T>() where T : UICanvas
    {
        T canvas = GetCanvas<T>();

        if (canvas != null)
        {
            canvas.Setup();
            canvas.Open();
        }
        return canvas;
    }

    public void Close<T>(float time) where T : UICanvas
    {
        if (IsOpened<T>())
        {
            canvases[typeof(T)].Close(time);
        }
    }

    public void CloseImmediate<T>() where T : UICanvas
    {
        if (IsOpened<T>())
        {
            canvases[typeof(T)].CloseImmediate();
        }
    }

    public bool IsLoaded<T>() where T : UICanvas
    {
        return canvases.ContainsKey(typeof(T)) && canvases[typeof(T)] != null;
    }

    public bool IsOpened<T>() where T : UICanvas
    {
        return IsLoaded<T>() && canvases[typeof(T)].gameObject.activeSelf;
    }

    public T GetCanvas<T>() where T : UICanvas
    {
        if (IsLoaded<T>())
        {
            return canvases[typeof(T)] as T;
        }

        Debug.LogWarning($"UI Canvas of type {typeof(T)} not found in the scene.");
        return null;
    }

    public void CloseAll()
    {
        foreach (var canvas in canvases)
        {
            if (canvas.Value != null && canvas.Value.gameObject.activeSelf)
            {
                canvas.Value.CloseImmediate();
            }
        }
    }
}
