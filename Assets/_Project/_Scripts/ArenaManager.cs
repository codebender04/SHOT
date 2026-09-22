using DG.Tweening;
using System.Collections;
using UnityEngine;

public class ArenaManager : Singleton<ArenaManager>
{
    [Header("References")]
    [SerializeField] private SpriteRenderer arenaVisual;
    [SerializeField] private Camera gameplayCamera;

    [Header("Walls")]
    [SerializeField] private Collider2D topWall;
    [SerializeField] private Collider2D bottomWall;
    [SerializeField] private Collider2D leftWall;
    [SerializeField] private Collider2D rightWall;

    [Header("Arena")]
    [SerializeField] private Vector2 baseSize = new Vector2(3f, 3f);
    [SerializeField] private Vector2 sizeIncrease = new Vector2(1f, 1f);
    [SerializeField] private float spawnPadding = 0.5f;
    [SerializeField] private int roundsPerSizeIncrease = 5;

    [Header("Camera")]
    [SerializeField] private float cameraPadding = 0.5f;

    [Header("Run Start Animation")]
    [SerializeField] private float popScale = 0.85f;
    [SerializeField] private float popDuration = 0.35f;
    [SerializeField] private Ease popEase = Ease.OutBack;

    public Vector2 Size { get; private set; }

    private Tween arenaTween;

    private float topWallOffset;
    private float bottomWallOffset;
    private float leftWallOffset;
    private float rightWallOffset;

    private Vector3 topWallBaseScale;
    private Vector3 bottomWallBaseScale;
    private Vector3 leftWallBaseScale;
    private Vector3 rightWallBaseScale;

    private Vector2 originalVisualSize;

    private void Awake()
    {
        if (gameplayCamera == null)
            gameplayCamera = Camera.main;

        CacheWallSetup();

        SetArenaSize(0);

        if (RoundManager.Instance != null)
        {
            RoundManager.Instance.OnRunStart += RoundManager_OnRunStart;
            RoundManager.Instance.OnRoundStart += RoundManager_OnRoundStart;
        }
    }

    private void CacheWallSetup()
    {
        if (arenaVisual != null)
            originalVisualSize = arenaVisual.size;

        Vector2 visualPosition = arenaVisual != null
            ? arenaVisual.transform.position
            : transform.position;

        if (topWall != null)
        {
            topWallOffset =
                topWall.transform.position.y -
                (visualPosition.y + originalVisualSize.y * 0.5f);

            topWallBaseScale = topWall.transform.localScale;
        }

        if (bottomWall != null)
        {
            bottomWallOffset =
                (visualPosition.y - originalVisualSize.y * 0.5f) -
                bottomWall.transform.position.y;

            bottomWallBaseScale = bottomWall.transform.localScale;
        }

        if (leftWall != null)
        {
            leftWallOffset =
                (visualPosition.x - originalVisualSize.x * 0.5f) -
                leftWall.transform.position.x;

            leftWallBaseScale = leftWall.transform.localScale;
        }

        if (rightWall != null)
        {
            rightWallOffset =
                rightWall.transform.position.x -
                (visualPosition.x + originalVisualSize.x * 0.5f);

            rightWallBaseScale = rightWall.transform.localScale;
        }
    }

    private void RoundManager_OnRunStart(int round)
    {
        SetArenaSizeForRound(round, true);
    }

    private void RoundManager_OnRoundStart(int round)
    {
        SetArenaSizeForRound(round);
    }

    private void OnDestroy()
    {
        arenaTween?.Kill();

        if (RoundManager.Instance != null)
        {
            RoundManager.Instance.OnRunStart -= RoundManager_OnRunStart;
            RoundManager.Instance.OnRoundStart -= RoundManager_OnRoundStart;
        }
    }

    public void SetArenaSizeForRound(int round, bool animate = false)
    {
        int tier = Mathf.Max(
            0,
            (round - 1) / roundsPerSizeIncrease
        );

        SetArenaSize(tier, animate);
    }

    public void SetArenaSize(int tier, bool animate = false)
    {
        Size = baseSize + sizeIncrease * tier;

        UpdateVisual(animate);
        UpdateWalls();
        UpdateCamera();
    }

    private void UpdateVisual(bool animate)
    {
        if (arenaVisual == null)
            return;

        arenaTween?.Kill();

        if (!animate)
        {
            arenaVisual.size = Size;
            return;
        }

        Vector2 startSize = Size * popScale;

        arenaVisual.size = startSize;

        arenaTween = DOTween.To(
            () => arenaVisual.size,
            value => arenaVisual.size = value,
            Size,
            popDuration
        )
        .SetEase(popEase)
        .SetUpdate(true);
    }

    private void UpdateWalls()
    {
        Vector2 center = transform.position;

        float scaleX = Size.x / originalVisualSize.x;
        float scaleY = Size.y / originalVisualSize.y;

        if (topWall != null)
        {
            Vector3 position = topWall.transform.position;

            position.x = center.x;
            position.y =
                center.y +
                Size.y * 0.5f +
                topWallOffset;

            topWall.transform.position = position;

            Vector3 scale = topWallBaseScale;
            scale.x *= scaleX;
            topWall.transform.localScale = scale;
        }

        if (bottomWall != null)
        {
            Vector3 position = bottomWall.transform.position;

            position.x = center.x;
            position.y =
                center.y -
                Size.y * 0.5f -
                bottomWallOffset;

            bottomWall.transform.position = position;

            Vector3 scale = bottomWallBaseScale;
            scale.x *= scaleX;
            bottomWall.transform.localScale = scale;
        }

        if (leftWall != null)
        {
            Vector3 position = leftWall.transform.position;

            position.x =
                center.x -
                Size.x * 0.5f -
                leftWallOffset;

            position.y = center.y;

            leftWall.transform.position = position;

            Vector3 scale = leftWallBaseScale;
            scale.y *= scaleY;
            leftWall.transform.localScale = scale;
        }

        if (rightWall != null)
        {
            Vector3 position = rightWall.transform.position;

            position.x =
                center.x +
                Size.x * 0.5f +
                rightWallOffset;

            position.y = center.y;

            rightWall.transform.position = position;

            Vector3 scale = rightWallBaseScale;
            scale.y *= scaleY;
            rightWall.transform.localScale = scale;
        }
    }

    private void UpdateCamera()
    {
        if (gameplayCamera == null)
            return;

        float verticalSize = Size.y * 0.5f;

        float horizontalSize =
            Size.x * 0.5f /
            gameplayCamera.aspect;

        gameplayCamera.orthographicSize =
            Mathf.Max(
                verticalSize,
                horizontalSize
            ) + cameraPadding;
    }

    public Vector2 GetRandomPosition()
    {
        Vector2 halfSize = Size * 0.5f;

        return (Vector2)transform.position + new Vector2(
            Random.Range(
                -halfSize.x + spawnPadding,
                halfSize.x - spawnPadding
            ),
            Random.Range(
                -halfSize.y + spawnPadding,
                halfSize.y - spawnPadding
            )
        );
    }

    public Vector2 GetRandomPosition(
        Vector2 origin,
        float maxDistance,
        float padding)
    {
        Vector2 halfSize = Size * 0.5f;

        for (int i = 0; i < 20; i++)
        {
            Vector2 offset =
                Random.insideUnitCircle * maxDistance;

            Vector2 position = origin + offset;

            if (Mathf.Abs(
                    position.x - transform.position.x) <=
                halfSize.x - padding &&
                Mathf.Abs(
                    position.y - transform.position.y) <=
                halfSize.y - padding)
            {
                return position;
            }
        }

        return GetRandomPosition();
    }

    public bool IsInsideArena(
        Vector2 position,
        float padding = 0f)
    {
        Vector2 localPosition =
            position - (Vector2)transform.position;

        Vector2 halfSize = Size * 0.5f;

        return Mathf.Abs(localPosition.x) <=
                   halfSize.x - padding &&
               Mathf.Abs(localPosition.y) <=
                   halfSize.y - padding;
    }

    public Bounds GetBounds()
    {
        return new Bounds(
            transform.position,
            Size
        );
    }
}