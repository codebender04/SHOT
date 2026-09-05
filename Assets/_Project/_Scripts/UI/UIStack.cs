using UnityEngine;
using UnityEngine.UI;

public class UIStack : MonoBehaviour
{
    [SerializeField] private Image itemPrefab;
    [SerializeField] private float offset = 3f;
    private void Start()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
    }
    public void Add(int value = 1)
    {
        for (int i = 0; i < value; i++)
        {
            Instantiate(itemPrefab, transform);
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
}