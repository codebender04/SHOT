using UnityEngine;
using UnityEngine.UI;

public class UIStack : MonoBehaviour
{
    [SerializeField] private Image itemPrefab;
    [SerializeField] private float offset = 3f;

    private void Start()
    {
        Clear();
    }

    public void Change(int value)
    {
        if (value > 0)
        {
            for (int i = 0; i < value; i++)
                Instantiate(itemPrefab, transform);
        }
        else if (value < 0)
        {
            int removeCount = Mathf.Min(-value, transform.childCount);

            for (int i = 0; i < removeCount; i++)
                Destroy(transform.GetChild(transform.childCount - 1 - i).gameObject);
        }

        Refresh();
    }

    public void Refresh()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            RectTransform child = transform.GetChild(i) as RectTransform;

            if (child == null)
                continue;

            child.anchoredPosition = Vector2.up * (i * offset);
        }
    }

    private void Clear()
    {
        for (int i = 0; i < transform.childCount; i++)
            Destroy(transform.GetChild(i).gameObject);
    }
}
