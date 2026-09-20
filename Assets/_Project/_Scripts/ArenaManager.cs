using DG.Tweening;
using System.Collections;
using UnityEngine;

public class ArenaManager : Singleton<ArenaManager>
{
    [Header("References")]
    [SerializeField] private Transform arenaVisual;
    [SerializeField] private Camera gameplayCamera;

    [Header("Walls")]
    [SerializeField] private Collider2D topWall;
    [SerializeField] private Collider2D bottomWall;
    [SerializeField] private Collider2D leftWall;
    [SerializeField] private Collider2D rightWall;
    [SerializeField] private float wallInset = 0.15f;

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

    private void Awake()
    {
        if (gameplayCamera == null)
            gameplayCamera = Camera.main;

        SetArenaSize(0);
        RoundManager.Instance.OnRunStart += RoundManager_OnRunStart;
        RoundManager.Instance.OnRoundStart += RoundManager_OnRoundStart;
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
    private void RoundManager_OnRunStart(int round)
    {
        SetArenaSizeForRound(round, true);
        StartCoroutine(PlayRunStartAnimation());
    }

    private IEnumerator PlayRunStartAnimation()
    {
        yield return null;

        if (arenaVisual == null)
            yield break;

        arenaVisual.DOKill();

        Vector3 targetScale = new Vector3(
            Size.x / baseSize.x,
            Size.y / baseSize.y,
            1f
        );

        arenaVisual.localScale = Vector3.one * 0.4f;

        arenaVisual
            .DOScale(targetScale, popDuration)
            .SetEase(Ease.OutBack)
            .SetUpdate(true);
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

        UpdateWalls();
        UpdateCamera();

        if (arenaVisual == null)
            return;

        Vector3 targetScale = new Vector3(
            Size.x / baseSize.x,
            Size.y / baseSize.y,
            1f
        );

        arenaVisual.DOKill();

        if (animate)
        {
            arenaVisual.localScale = Vector3.zero;

            arenaVisual
                .DOScale(targetScale, popDuration)
                .SetEase(popEase);
        }
        else
        {
            arenaVisual.localScale = targetScale;
        }
    }
    private void UpdateWalls()
    {
        Vector2 center = transform.position;
        Vector2 halfSize = Size * 0.5f;

        PositionHorizontalWall(
            topWall,
            center + Vector2.up * (halfSize.y - wallInset)
        );

        PositionHorizontalWall(
            bottomWall,
            center - Vector2.up * (halfSize.y - wallInset)
        );

        PositionVerticalWall(
            leftWall,
            center - Vector2.right * (halfSize.x - wallInset)
        );

        PositionVerticalWall(
            rightWall,
            center + Vector2.right * (halfSize.x - wallInset)
        );
    }

    private void PositionHorizontalWall(
        Collider2D wall,
        Vector2 targetCenter)
    {
        if (wall == null)
            return;

        float halfThickness = wall.bounds.extents.y;
        float y = targetCenter.y;

        if (wall == topWall)
            y += halfThickness;

        if (wall == bottomWall)
            y -= halfThickness;

        wall.transform.position = new Vector3(
            targetCenter.x,
            y,
            wall.transform.position.z
        );
    }

    private void PositionVerticalWall(
        Collider2D wall,
        Vector2 targetCenter)
    {
        if (wall == null)
            return;

        float halfThickness = wall.bounds.extents.x;
        float x = targetCenter.x;

        if (wall == leftWall)
            x -= halfThickness;

        if (wall == rightWall)
            x += halfThickness;

        wall.transform.position = new Vector3(
            x,
            targetCenter.y,
            wall.transform.position.z
        );
    }

    private void UpdateCamera()
    {
        if (gameplayCamera == null)
            return;

        float verticalSize = Size.y * 0.5f;
        float horizontalSize =
            Size.x * 0.5f / gameplayCamera.aspect;

        gameplayCamera.orthographicSize =
            Mathf.Max(verticalSize, horizontalSize) +
            cameraPadding;
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