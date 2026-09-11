using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

public class CustomCursor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform cursor;
    [SerializeField] private Canvas canvas;

    [Header("Movement")]
    [SerializeField] private float rotationAmount = 0.08f;
    [SerializeField] private float maxRotation = 12f;
    [SerializeField] private float rotationSmoothness = 15f;

    [Header("Click Animation")]
    [SerializeField] private float clickScale = 1.25f;
    [SerializeField] private float clickScaleDuration = 0.12f;
    [SerializeField] private float clickShakeStrength = 4f;
    [SerializeField] private int clickShakeVibrato = 8;
    [SerializeField] private float clickShakeDuration = 0.15f;

    private Camera canvasCamera;
    private Vector2 previousMousePosition;
    private Vector2 mouseVelocity;
    private float currentRotation;

    private Tween scaleTween;
    private Tween rotationTween;
    private Tween shakeTween;

    private void Awake()
    {
        Cursor.visible = false;

        canvasCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : canvas.worldCamera;

        previousMousePosition = Mouse.current != null
            ? Mouse.current.position.ReadValue()
            : Vector2.zero;
    }

    private void Update()
    {
        if (Mouse.current == null)
            return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();

        UpdatePosition(mousePosition);
        UpdateRotation(mousePosition);

        if (Mouse.current.leftButton.wasPressedThisFrame)
            PlayClickAnimation();

        previousMousePosition = mousePosition;
    }

    private void UpdatePosition(Vector2 mousePosition)
    {
        RectTransform canvasRect = canvas.transform as RectTransform;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                mousePosition,
                canvasCamera,
                out Vector2 localPosition))
        {
            cursor.localPosition = localPosition;
        }
    }

    private void UpdateRotation(Vector2 mousePosition)
    {
        mouseVelocity = (mousePosition - previousMousePosition) / Time.unscaledDeltaTime;

        float targetRotation = Mathf.Clamp(
            -mouseVelocity.x * rotationAmount,
            -maxRotation,
            maxRotation
        );

        currentRotation = Mathf.Lerp(
            currentRotation,
            targetRotation,
            rotationSmoothness * Time.unscaledDeltaTime
        );

        cursor.localRotation = Quaternion.Euler(
            0f,
            0f,
            currentRotation
        );
    }

    private void PlayClickAnimation()
    {
        scaleTween?.Kill();
        rotationTween?.Kill();
        shakeTween?.Kill();

        scaleTween = cursor
            .DOScale(clickScale, clickScaleDuration)
            .SetEase(Ease.OutBack)
            .OnComplete(() =>
            {
                scaleTween = cursor
                    .DOScale(1f, clickScaleDuration)
                    .SetEase(Ease.OutQuad);
            });

        shakeTween = cursor
            .DOShakeAnchorPos(
                clickShakeDuration,
                clickShakeStrength,
                clickShakeVibrato,
                90f,
                false,
                true
            )
            .SetUpdate(true);
    }

    private void OnDestroy()
    {
        scaleTween?.Kill();
        rotationTween?.Kill();
        shakeTween?.Kill();

        Cursor.visible = true;
    }
}