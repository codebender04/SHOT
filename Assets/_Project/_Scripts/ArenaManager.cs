using UnityEngine;

public class ArenaManager : Singleton<ArenaManager>
{
    [Header("References")]
    [SerializeField] private Transform arenaVisual;
    [SerializeField] private Camera gameplayCamera;

    [Header("Arena")]
    [SerializeField] private Vector2 baseSize = new Vector2(3f, 3f);
    [SerializeField] private Vector2 sizeIncrease = new Vector2(1f, 1f);
    [SerializeField] private float spawnPadding = 0.5f;

    [Header("Camera")]
    [SerializeField] private float cameraPadding = 0.5f;

    public Vector2 Size { get; private set; }

    private void Awake()
    {
        if (gameplayCamera == null)
            gameplayCamera = Camera.main;

        SetArenaSize(0);
    }

    public void SetArenaSize(int tier)
    {
        Size = baseSize + sizeIncrease * tier;

        UpdateVisual();
        UpdateCamera();
    }

    private void UpdateVisual()
    {
        if (arenaVisual == null)
            return;

        Vector3 scale = new Vector3(
            Size.x / baseSize.x,
            Size.y / baseSize.y,
            1f
        );

        arenaVisual.localScale = scale;
    }

    private void UpdateCamera()
    {
        if (gameplayCamera == null)
            return;

        float verticalSize = Size.y * 0.5f;
        float horizontalSize = Size.x * 0.5f / gameplayCamera.aspect;

        gameplayCamera.orthographicSize =
            Mathf.Max(verticalSize, horizontalSize) + cameraPadding;
    }

    public Vector2 GetRandomPosition()
    {
        Vector2 halfSize = Size * 0.5f;

        return (Vector2)transform.position + new Vector2(
            Random.Range(-halfSize.x + spawnPadding, halfSize.x - spawnPadding),
            Random.Range(-halfSize.y + spawnPadding, halfSize.y - spawnPadding)
        );
    }
    public Vector2 GetRandomPosition(Vector2 origin, float maxDistance, float padding)
    {
        Vector2 halfSize = Size * 0.5f;

        for (int i = 0; i < 20; i++)
        {
            Vector2 offset = Random.insideUnitCircle * maxDistance;
            Vector2 position = origin + offset;

            if (Mathf.Abs(position.x - transform.position.x) <= halfSize.x - padding &&
                Mathf.Abs(position.y - transform.position.y) <= halfSize.y - padding)
            {
                return position;
            }
        }

        return GetRandomPosition();
    }
    public bool IsInsideArena(Vector2 position, float padding = 0f)
    {
        Vector2 localPosition =
            position - (Vector2)transform.position;

        Vector2 halfSize = Size * 0.5f;

        return Mathf.Abs(localPosition.x) <= halfSize.x - padding &&
               Mathf.Abs(localPosition.y) <= halfSize.y - padding;
    }

    public Bounds GetBounds()
    {
        return new Bounds(
            transform.position,
            Size
        );
    }
}