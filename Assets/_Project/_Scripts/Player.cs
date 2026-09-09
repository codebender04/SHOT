using System;
using System.Collections.Generic;
using DG.Tweening;
using MoreMountains.Feedbacks;
using UnityEngine;

public class Player : Singleton<Player>
{
    public event Action<int> OnAmmoChanged;
    public event Action<int> OnMaxBouncesChanged;

    [Header("References")]
    [SerializeField] private Bullet bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private Transform gun;
    [SerializeField] private MMFeedbacks shootFeedback;
    [SerializeField] private MMFeedbacks bounceFeedback;
    [SerializeField] private MMFeedbacks hitFeedback;

    [Header("Shooting")]
    [SerializeField] private int ammo = 5;
    [SerializeField] private int maxBounces = 0;

    [Header("Movement")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private float recoilForce = 3f;
    [SerializeField] private float maxSpeed = 5f;
    [SerializeField] private float movementDamping = 5f;
    [SerializeField] private float wallBounceMultiplier = 1f;
    [SerializeField] private float wallSkin = 0.02f;
    [SerializeField] private float minBounceSpeed = 2f;

    [Header("Gun")]
    [SerializeField] private float gunDistance = 0.5f;

    [Header("Aim Tilt")]
    [SerializeField] private float maxTilt = 8f;
    [SerializeField] private float tiltSmoothness = 10f;
    [SerializeField] private float tiltDistance = 3f;

    [Header("Aim Preview")]
    [SerializeField] private LineRenderer aimPreviewLine;
    [SerializeField] private float previewLength = 10f;
    [SerializeField] private float dotSpacing = 0.35f;
    [SerializeField] private LayerMask bulletCollisionMask;

    private readonly List<Vector3> previewPoints = new();

    private Vector3 currentTiltVelocity;
    private Quaternion baseRotation;
    private Camera mainCamera;
    private bool canShoot = true;
    private int totalBounces;
    private int ammoUsed;
    public int Ammo => ammo;
    public int AmmoUsed => ammoUsed;
    public int MaxBounces => maxBounces;
    public int TotalBounces => totalBounces;
    private void Awake()
    {
        mainCamera = Camera.main;
        baseRotation = transform.localRotation;

        GameInput.Instance.ShootPressed += Shoot;

        SetupAimPreviewLine();

        OnAmmoChanged?.Invoke(ammo);
        OnMaxBouncesChanged?.Invoke(maxBounces);
    }

    private void OnDestroy()
    {
        if (GameInput.Instance != null)
            GameInput.Instance.ShootPressed -= Shoot;
    }

    private void SetupAimPreviewLine()
    {
        if (aimPreviewLine == null)
            return;

        aimPreviewLine.useWorldSpace = true;
        aimPreviewLine.textureMode = LineTextureMode.Tile;
    }

    private void Update()
    {
        UpdateAim(GameInput.Instance.AimScreenPosition);
    }

    private void UpdateAim(Vector2 screenPosition)
    {
        Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(
            new Vector3(
                screenPosition.x,
                screenPosition.y,
                -mainCamera.transform.position.z
            )
        );

        Vector2 direction = mouseWorld - transform.position;

        if (direction.sqrMagnitude <= 0.001f)
            return;

        direction.Normalize();

        UpdatePlayerTilt(mouseWorld);

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        gun.rotation = Quaternion.Euler(0f, 0f, angle);
        gun.position = transform.position + (Vector3)(direction * gunDistance);

        Vector3 gunScale = gun.localScale;
        gunScale.y = direction.x < 0f
            ? -Mathf.Abs(gunScale.y)
            : Mathf.Abs(gunScale.y);

        gun.localScale = gunScale;

        firePoint.right = direction;

        UpdateAimPreview(firePoint.position, direction);
    }

    private void UpdateAimPreview(Vector2 origin, Vector2 direction)
    {
        if (aimPreviewLine == null)
            return;

        aimPreviewLine.enabled = ammo > 0;

        if (!aimPreviewLine.enabled)
            return;

        previewPoints.Clear();
        previewPoints.Add(origin);

        Vector2 currentPosition = origin;
        Vector2 currentDirection = direction;

        float remainingDistance = previewLength;
        int bounceCount = 0;

        while (remainingDistance > 0f)
        {
            RaycastHit2D hit = Physics2D.Raycast(
                currentPosition,
                currentDirection,
                remainingDistance,
                bulletCollisionMask
            );

            if (hit.collider == null)
            {
                previewPoints.Add(
                    currentPosition + currentDirection * remainingDistance
                );

                break;
            }

            previewPoints.Add(hit.point);
            remainingDistance -= hit.distance;

            if (!hit.collider.CompareTag(Constants.TAG_WALL))
            {
                currentPosition =
                    hit.point + currentDirection * 0.02f;

                continue;
            }

            bounceCount++;

            if (bounceCount > maxBounces)
                break;

            currentDirection =
                Vector2.Reflect(
                    currentDirection,
                    hit.normal
                ).normalized;

            currentPosition =
                hit.point + currentDirection * 0.02f;
        }

        aimPreviewLine.positionCount = previewPoints.Count;
        aimPreviewLine.SetPositions(previewPoints.ToArray());

        UpdatePreviewTiling();
    }

    private void UpdatePreviewTiling()
    {
        if (aimPreviewLine.material == null || dotSpacing <= 0f)
            return;

        float totalLength = 0f;

        for (int i = 1; i < previewPoints.Count; i++)
        {
            totalLength += Vector3.Distance(
                previewPoints[i - 1],
                previewPoints[i]
            );
        }

        aimPreviewLine.material.mainTextureScale =
            new Vector2(totalLength / dotSpacing, 1f);
    }

    private void UpdatePlayerTilt(Vector3 mouseWorld)
    {
        Vector3 offset = mouseWorld - transform.position;

        float normalizedX = Mathf.Clamp(
            offset.x / tiltDistance,
            -1f,
            1f
        );

        float normalizedY = Mathf.Clamp(
            offset.y / tiltDistance,
            -1f,
            1f
        );

        float targetTiltX = -normalizedY * maxTilt;
        float targetTiltY = normalizedX * maxTilt;

        Vector3 targetEuler = new(
            targetTiltX,
            targetTiltY,
            0f
        );

        Vector3 currentEuler = transform.localEulerAngles;

        currentEuler.x = NormalizeAngle(currentEuler.x);
        currentEuler.y = NormalizeAngle(currentEuler.y);

        Vector3 newEuler = Vector3.SmoothDamp(
            currentEuler,
            targetEuler,
            ref currentTiltVelocity,
            1f / tiltSmoothness
        );

        transform.localRotation =
            baseRotation *
            Quaternion.Euler(newEuler);
    }

    private float NormalizeAngle(float angle)
    {
        if (angle > 180f)
            angle -= 360f;

        return angle;
    }

    private void Shoot()
    {
        if (!GameManager.Instance.IsPlaying)
            return;

        if (!canShoot || ammo <= 0)
            return;

        canShoot = false;
        ammo--;
        ammoUsed++;
        OnAmmoChanged?.Invoke(ammo);

        Vector2 direction = firePoint.right;

        rb.linearVelocity += -direction * recoilForce;

        Bullet bullet = Instantiate(
            bulletPrefab,
            firePoint.position,
            Quaternion.identity
        );

        bullet.Initialize(
            direction,
            maxBounces,
            () =>
                {
                    canShoot = true;
                    RoundManager.Instance.CheckRunLost();
                },
            () =>
            {
                totalBounces++;
                bounceFeedback?.PlayFeedbacks();
            },
            () => hitFeedback?.PlayFeedbacks()
        );

        shootFeedback?.PlayFeedbacks();
    }

    public void ChangeAmmo(int value)
    {
        ammo = Mathf.Max(0, ammo + value);
        OnAmmoChanged?.Invoke(ammo);
    }

    public void IncreaseMaxBounces(int amount = 1)
    {
        maxBounces += amount;
        OnMaxBouncesChanged?.Invoke(maxBounces);
    }

    private void FixedUpdate()
    {
        Vector2 velocity = rb.linearVelocity;

        velocity = Vector2.Lerp(
            velocity,
            Vector2.zero,
            movementDamping * Time.fixedDeltaTime
        );

        if (velocity.sqrMagnitude <= 0.001f)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        velocity = Vector2.ClampMagnitude(
            velocity,
            maxSpeed
        );

        float distance =
            velocity.magnitude * Time.fixedDeltaTime;

        Vector2 direction = velocity.normalized;

        RaycastHit2D hit = Physics2D.Raycast(
            rb.position,
            direction,
            distance + wallSkin,
            bulletCollisionMask
        );

        if (hit.collider != null &&
            hit.collider.CompareTag(Constants.TAG_WALL))
        {
            Vector2 reflectedDirection =
                Vector2.Reflect(
                    direction,
                    hit.normal
                ).normalized;

            float bounceSpeed = Mathf.Max(
                velocity.magnitude * wallBounceMultiplier,
                minBounceSpeed
            );

            velocity = reflectedDirection * bounceSpeed;

            rb.MovePosition(
                hit.point + hit.normal * wallSkin
            );

            rb.linearVelocity = velocity;

            bounceFeedback?.PlayFeedbacks();

            return;
        }

        rb.MovePosition(
            rb.position +
            velocity * Time.fixedDeltaTime
        );

        rb.linearVelocity = velocity;
    }
    public void ResetPlayer()
    {
        totalBounces = 0;
        ammoUsed = 0;
        transform.position = Vector3.zero;
    }
}
