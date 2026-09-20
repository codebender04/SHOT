using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UIButtonSFX : MonoBehaviour
{
    [SerializeField] private AudioClip clickSFX;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(PlaySFX);
    }

    private void PlaySFX()
    {
        if (clickSFX != null)
            AudioSource.PlayClipAtPoint(clickSFX, Camera.main.transform.position);
    }

    private void OnDestroy()
    {
        button.onClick.RemoveListener(PlaySFX);
    }
}