using System;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public static event Action<Vector3, int> OnEnemyKilled;
    public static event Action OnFinished;

    public static int ActiveCount { get; private set; }

    [Header("Movement")]
    [SerializeField] private float speed = 18f;
    [SerializeField] private float lifetime = 5f;

    [Header("Collision Layers")]
    [SerializeField] private LayerMask hitLayers;

    public float MaxTravelDistance => speed * lifetime;

    [Header("Effects")]
    [SerializeField] private ParticleSystem bounceEffect;
    [SerializeField] private TrailRenderer bulletTrail;
    [SerializeField] private int trailCornerVertices;
    [SerializeField] private int trailCapVertices;
    [SerializeField] private float trailWidth = 0.05f;

    private BulletFireEffect fireEffect;

    private Vector2 direction;
    private Action onFinished;
    private Action onBounced;
    private Action onHit;

    private int maxBounces;
    private int bounceCount;
    private int killStreak;
    private float remainingLifetime;
    private bool active;

    private readonly List<Enemy> killedEnemyList = new();

    private void Awake()
    {
        fireEffect = GetComponent<BulletFireEffect>();
    }

    public void Initialize(
        Vector2 shootDirection,
        int bounceLimit,
        Action finishedCallback,
        Action bouncedCallback,
        Action hitCallback)
    {
        direction = shootDirection.normalized;
        maxBounces = bounceLimit;

        onFinished = finishedCallback;
        onBounced = bouncedCallback;
        onHit = hitCallback;

        killStreak = 0;
        bounceCount = 0;
        remainingLifetime = lifetime;
        active = true;

        ActiveCount++;

        SetupTrail();
        fireEffect?.ResetFire();
    }

    private void OnDestroy()
    {
        if (active)
            ActiveCount--;
    }

    private void SetupTrail()
    {
        if (bulletTrail == null)
            return;

        bulletTrail.numCornerVertices = trailCornerVertices;
        bulletTrail.numCapVertices = trailCapVertices;
        bulletTrail.widthCurve = AnimationCurve.Constant(0f, 1f, trailWidth);
        bulletTrail.widthMultiplier = 1f;
        bulletTrail.textureMode = LineTextureMode.Stretch;
        bulletTrail.alignment = LineAlignment.TransformZ;

        bulletTrail.Clear();
        bulletTrail.emitting = true;
        bulletTrail.AddPosition(transform.position);
    }

    private void Update()
    {
        if (!active)
            return;

        float distance = speed * Time.deltaTime;

        MoveWithCollision(distance);

        remainingLifetime -= Time.deltaTime;

        if (remainingLifetime <= 0f)
            Finish();
    }

    private void MoveWithCollision(float distance)
    {
        Vector2 currentPosition = transform.position;

        RaycastHit2D hit = Physics2D.Raycast(
            currentPosition,
            direction,
            distance,
            hitLayers
        );

        if (hit.collider == null)
        {
            transform.position = currentPosition + direction * distance;
            return;
        }

        HandleCollision(hit, distance);
    }

    private void HandleCollision(RaycastHit2D hit, float distance)
    {
        transform.position = hit.point;

        int enemyLayer = LayerMask.NameToLayer("Enemy");

        if (hit.collider.gameObject.layer == enemyLayer)
        {
            if (hit.collider.TryGetComponent(out Enemy enemy))
            {
                enemy.TakeHit();
                onHit?.Invoke();

                if (enemy.IsDead)
                {
                    killedEnemyList.Add(enemy);

                    killStreak++;
                    OnEnemyKilled?.Invoke(hit.point, killStreak);
                    fireEffect?.SetKillStreak(killStreak);
                }
            }

            float remainingDistance = distance - hit.distance;

            if (remainingDistance > 0f)
                transform.position += (Vector3)direction * remainingDistance;

            return;
        }

        int wallLayer = LayerMask.NameToLayer("Wall");

        if (hit.collider.gameObject.layer == wallLayer ||
            hit.collider.CompareTag(Constants.TAG_WALL))
        {
            bulletTrail?.AddPosition(hit.point);

            Bounce(hit.normal);

            if (bounceEffect != null)
            {
                ParticleSystem effect = Instantiate(
                    bounceEffect,
                    hit.point,
                    Quaternion.Euler(hit.normal)
                );

                Destroy(effect.gameObject, 1f);
            }

            onBounced?.Invoke();

            if (!active)
                return;

            float remainingDistance = distance - hit.distance;

            if (remainingDistance > 0f)
                transform.position += (Vector3)direction * remainingDistance;

            return;
        }

        Finish();
    }

    private void Bounce(Vector2 normal)
    {
        bounceCount++;

        if (bounceCount > maxBounces)
        {
            Finish();
            return;
        }

        direction = Vector2.Reflect(direction, normal).normalized;
    }

    private void Finish()
    {
        if (!active)
            return;

        active = false;

        foreach (Enemy enemy in killedEnemyList)
        {
            if (enemy != null)
                enemy.FinishDeath();
        }

        onFinished?.Invoke();
        OnFinished?.Invoke();

        Destroy(gameObject);
    }
}