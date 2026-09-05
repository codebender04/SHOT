using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T ins;

    public static T Instance
    {
        get
        {
            if (ins == null)
            {
                ins = FindAnyObjectByType<T>();
            }
            return ins;
        }
    }

}