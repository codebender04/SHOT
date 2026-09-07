using System;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public static event Action OnFinished;

    public static int ActiveCount { get; private set; }

    [Header("Movement")]
    [SerializeField] private float speed = 18f;
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private int maxBounces = 10;

    [Header("Collision Layers")]
    [SerializeField] private LayerMask hitLayers;

    public float MaxTravelDistance => speed * lifetime;
    public int MaxBounces => maxBounces;

    [Header("Effects")]
    [SerializeField] private ParticleSystem bounceEffect;
    [SerializeField] private TrailRenderer bulletTrail;
    [SerializeField] private int trailCornerVertices = 0;
    [SerializeField] private int trailCapVertices = 0;
    [SerializeField] private float trailWidth = 0.05f;

    private Vector2 direction;
    private Action onFinished;
    private Action onBounced;
    private Action onHit;

    private int bounceCount;
    private float remainingLifetime;
    private bool active;
    private readonly List<Enemy> killedEnemyList = new();

    public void Initialize(
        Vector2 shootDirection,
        Action finishedCallback,
        Action bouncedCallback,
        Action hitCallback)
    {
        direction = shootDirection.normalized;
        onFinished = finishedCallback;
        onBounced = bouncedCallback;
        onHit = hitCallback;

        bounceCount = 0;
        remainingLifetime = lifetime;
        active = true;

        ActiveCount++;

        SetupTrail();
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
            if (hit.collider.TryGetComponent<Enemy>(out var enemy))
            {
                enemy.TakeHit();
                onHit?.Invoke();

                if (enemy.IsDead)
                    killedEnemyList.Add(enemy);
            }

            float remainingDistance = distance - hit.distance;

            if (remainingDistance > 0f)
                transform.position += (Vector3)direction * remainingDistance;

            return;
        }

        int wallLayer = LayerMask.NameToLayer("Wall");

        if (hit.collider.gameObject.layer == wallLayer || hit.collider.CompareTag(Constants.TAG_WALL))
        {
            bulletTrail?.AddPosition(hit.point);

            Bounce(hit.normal);

            Destroy(Instantiate(bounceEffect, (Vector3)hit.point, Quaternion.Euler(hit.normal)).gameObject, 1f);
            onBounced?.Invoke();

            float remainingDistance = distance - hit.distance;

            if (remainingDistance > 0f)
                transform.position += (Vector3)direction * remainingDistance;

            return;
        }

        Finish();
    }

    private void Bounce(Vector2 normal)
    {
        direction = Vector2.Reflect(direction, normal).normalized;
        bounceCount++;

        if (bounceCount >= maxBounces)
            Finish();
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