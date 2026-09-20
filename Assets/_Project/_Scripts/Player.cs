using DG.Tweening;
using MoreMountains.Feedbacks;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static PremiumUpgrade;

public class Player : Singleton<Player>
{
    public event Action<int> OnAmmoChanged;
    public event Action<int> OnMaxBouncesChanged;

    [Header("References")]
    [SerializeField] private SpriteRenderer visual;
    [SerializeField] private Bullet bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private Transform gun;
    [SerializeField] private Transform secondaryFirePoint;
    [SerializeField] private Transform secondaryGun;
    [SerializeField] private MMFeedbacks shootFeedback;
    [SerializeField] private MMFeedbacks bounceFeedback;
    [SerializeField] private MMFeedbacks hitFeedback;

    [Header("Shooting")]
    [SerializeField] private int ammo = 5;
    [SerializeField] private int maxBounces = 0;

    [Header("Recoil Movement")]
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
    [SerializeField] private LineRenderer secondaryAimPreviewLine;
    [SerializeField] private float dotSpacing = 0.35f;
    [SerializeField] private LayerMask bulletCollisionMask;
    [Header("Bullet Spawn")]
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private float wallSpawnOffset = 0.02f;

    [Header("WASD Movement")]
    [SerializeField] private float movementDistancePerRound = 3f;
    [SerializeField] private float movementSpeed = 5f;
    [SerializeField] private LayerMask movementCollisionMask;
    [SerializeField] private Slider movementSlider;

    public int Ammo => ammo;
    public int AmmoUsed => ammoUsed;
    public int MaxBounces => maxBounces;
    public int TotalBounces => totalBounces;

    private readonly List<Vector3> previewPoints = new();

    private float remainingMovementDistance;
    private bool recoilActive;
    private Vector3 currentTiltVelocity;
    private Quaternion baseRotation;
    private Camera mainCamera;
    private bool canShoot = true;
    private int totalBounces;
    private int ammoUsed;

    private int bulletsRemainingThisShot;

    private void Awake()
    {
        mainCamera = Camera.main;
        baseRotation = transform.localRotation;

        GameInput.Instance.ShootPressed += Shoot;
        RoundManager.Instance.OnRoundStart += RoundManager_OnRoundStart;
        RoundManager.Instance.OnRunStart += RoundManager_OnRunStart;

        movementSlider.gameObject.SetActive(false);

        if (secondaryGun != null)
            secondaryGun.gameObject.SetActive(false);

        OnAmmoChanged?.Invoke(ammo);
        OnMaxBouncesChanged?.Invoke(maxBounces);
    }

    private void RoundManager_OnRunStart(int round)
    {
        visual.DOKill();

        Vector3 targetPosition = visual.transform.localPosition;

        visual.transform.localPosition = targetPosition + Vector3.up * 0.2f;


        if (visual != null)
        {
            Color color = visual.color;
            color.a = 0f;
            visual.color = color;

            Sequence sequence = DOTween.Sequence();

            sequence.Join(
                visual.transform.DOLocalMove(targetPosition, 0.4f)
                    .SetEase(Ease.OutCubic)
            );

            sequence.Join(
                visual.DOFade(1f, 0.4f)
                    .SetEase(Ease.OutCubic)
            );
        }
    }

    private void RoundManager_OnRoundStart(int round)
    {
        remainingMovementDistance = movementDistancePerRound;

        bool hasUpgrade =
            PremiumUpgradeManager.Instance.HasUpgrade(
                UpgradeType.WASDMovement);

        movementSlider.gameObject.SetActive(hasUpgrade);

        if (hasUpgrade)
            movementSlider.value = 1f;

        UpdateSecondaryGunVisibility();
    }

    private void OnDestroy()
    {
        if (GameInput.Instance != null)
            GameInput.Instance.ShootPressed -= Shoot;

        if (RoundManager.Instance != null)
            RoundManager.Instance.OnRoundStart -= RoundManager_OnRoundStart;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!recoilActive)
            return;

        if (!PremiumUpgradeManager.Instance.HasUpgrade(
            UpgradeType.RecoilDamage))
            return;

        if (collision.gameObject.layer != LayerMask.NameToLayer("Enemy"))
            return;

        if (!collision.TryGetComponent(out Enemy enemy))
            return;

        enemy.TakeHit();
        hitFeedback?.PlayFeedbacks();
    }

    private void FixedUpdate()
    {
        HandleWASDMovement();

        Vector2 velocity = rb.linearVelocity;

        velocity = Vector2.Lerp(
            velocity,
            Vector2.zero,
            movementDamping * Time.fixedDeltaTime
        );

        if (velocity.sqrMagnitude <= 0.001f)
        {
            rb.linearVelocity = Vector2.zero;
            recoilActive = false;
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

        float angle =
            Mathf.Atan2(direction.y, direction.x) *
            Mathf.Rad2Deg;

        gun.rotation =
            Quaternion.Euler(0f, 0f, angle);

        gun.position =
            transform.position +
            (Vector3)(direction * gunDistance);

        Vector3 gunScale = gun.localScale;

        gunScale.y = direction.x < 0f
            ? -Mathf.Abs(gunScale.y)
            : Mathf.Abs(gunScale.y);

        gun.localScale = gunScale;

        firePoint.right = direction;

        UpdateSecondaryGun(
            direction,
            angle
        );

        Vector2 primaryPreviewOrigin = GetSafeBulletSpawnPosition(firePoint.position, direction);

        UpdateAimPreview(
            primaryPreviewOrigin,
            direction
        );
    }

    private void UpdateSecondaryGun(
        Vector2 primaryDirection,
        float primaryAngle)
    {
        if (secondaryGun == null || secondaryFirePoint == null)
            return;

        bool hasDualGun =
            PremiumUpgradeManager.Instance.HasUpgrade(
                UpgradeType.DualGun);

        secondaryGun.gameObject.SetActive(hasDualGun);

        if (!hasDualGun)
            return;

        Vector2 oppositeDirection = -primaryDirection;

        float oppositeAngle =
            primaryAngle + 180f;

        secondaryGun.rotation =
            Quaternion.Euler(
                0f,
                0f,
                oppositeAngle
            );

        secondaryGun.position =
            transform.position +
            (Vector3)(oppositeDirection * gunDistance);

        Vector3 gunScale = secondaryGun.localScale;

        gunScale.y = oppositeDirection.x < 0f
            ? -Mathf.Abs(gunScale.y)
            : Mathf.Abs(gunScale.y);

        secondaryGun.localScale = gunScale;

        secondaryFirePoint.right =
            oppositeDirection;
    }

    private void UpdateSecondaryGunVisibility()
    {
        if (secondaryGun == null)
            return;

        bool hasDualGun =
            PremiumUpgradeManager.Instance.HasUpgrade(
                UpgradeType.DualGun);

        secondaryGun.gameObject.SetActive(hasDualGun);
    }
    private void UpdateAimPreview(Vector2 origin, Vector2 direction)
    {
        bool previewEnabled =
            ammo > 0 &&
            PremiumUpgradeManager.Instance.HasUpgrade(
                PremiumUpgrade.UpgradeType.TrajectoryPreview);

        if (aimPreviewLine != null)
            aimPreviewLine.enabled = previewEnabled;

        if (secondaryAimPreviewLine != null)
            secondaryAimPreviewLine.enabled =
                previewEnabled &&
                PremiumUpgradeManager.Instance.HasUpgrade(
                    PremiumUpgrade.UpgradeType.DualGun);

        if (!previewEnabled)
            return;

        UpdateSingleAimPreview(
            aimPreviewLine,
            origin,
            direction
        );

        if (secondaryAimPreviewLine != null && PremiumUpgradeManager.Instance.HasUpgrade(PremiumUpgrade.UpgradeType.DualGun))
        {
            Vector2 secondaryDirection = -direction;

            Vector2 secondaryPreviewOrigin =
                GetSafeBulletSpawnPosition(
                    secondaryFirePoint.position,
                    secondaryDirection
                );

            UpdateSingleAimPreview(
                secondaryAimPreviewLine,
                secondaryPreviewOrigin,
                secondaryDirection
            );
        }
    }

    private void UpdateSingleAimPreview(
        LineRenderer line,
        Vector2 origin,
        Vector2 direction)
    {
        if (line == null)
            return;

        previewPoints.Clear();
        previewPoints.Add(origin);

        Vector2 currentPosition = origin;
        Vector2 currentDirection = direction.normalized;

        float remainingDistance =
            bulletPrefab.MaxTravelDistance;

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
                    currentPosition +
                    currentDirection * remainingDistance
                );

                break;
            }

            previewPoints.Add(hit.point);

            remainingDistance -= hit.distance;

            if (!hit.collider.CompareTag(Constants.TAG_WALL))
            {
                currentPosition =
                    hit.point +
                    currentDirection * 0.02f;

                continue;
            }

            if (bounceCount >= maxBounces)
                break;

            bounceCount++;

            currentDirection =
                Vector2.Reflect(
                    currentDirection,
                    hit.normal
                ).normalized;

            currentPosition =
                hit.point +
                currentDirection * 0.02f;
        }

        line.positionCount =
            previewPoints.Count;

        line.SetPositions(
            previewPoints.ToArray()
        );

        UpdatePreviewTiling(line);
    }

    private void UpdatePreviewTiling(LineRenderer line)
    {
        if (line == null ||
            line.material == null ||
            dotSpacing <= 0f)
            return;

        float totalLength = 0f;

        for (int i = 1; i < previewPoints.Count; i++)
        {
            totalLength += Vector3.Distance(
                previewPoints[i - 1],
                previewPoints[i]
            );
        }

        line.material.mainTextureScale =
            new Vector2(
                totalLength / dotSpacing,
                1f
            );
    }
    private void UpdatePreviewTiling()
    {
        if (aimPreviewLine.material == null ||
            dotSpacing <= 0f)
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
            new Vector2(
                totalLength / dotSpacing,
                1f
            );
    }

    private void UpdatePlayerTilt(Vector3 mouseWorld)
    {
        Vector3 offset =
            mouseWorld - transform.position;

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

        float targetTiltX =
            -normalizedY * maxTilt;

        float targetTiltY =
            normalizedX * maxTilt;

        Vector3 targetEuler = new(
            targetTiltX,
            targetTiltY,
            0f
        );

        Vector3 currentEuler =
            transform.localEulerAngles;

        currentEuler.x =
            NormalizeAngle(currentEuler.x);

        currentEuler.y =
            NormalizeAngle(currentEuler.y);

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

        Vector2 primaryDirection =
            firePoint.right;

        bool hasDualGun =
            PremiumUpgradeManager.Instance.HasUpgrade(
                UpgradeType.DualGun);

        int bulletCount =
            hasDualGun ? 2 : 1;

        bulletsRemainingThisShot =
            bulletCount;

        Enemy.StartShot();

        rb.linearVelocity +=
            -primaryDirection * recoilForce;

        recoilActive = true;

        Vector2 primarySpawnPosition = GetSafeBulletSpawnPosition(firePoint.position, primaryDirection);

        SpawnBullet(primarySpawnPosition, primaryDirection);

        if (hasDualGun)
        {
            Vector2 secondaryDirection = -primaryDirection;

            Vector2 secondarySpawnPosition = GetSafeBulletSpawnPosition(secondaryFirePoint.position, secondaryDirection);

            SpawnBullet(secondarySpawnPosition, secondaryDirection);
        }

        shootFeedback?.PlayFeedbacks();
    }

    private void SpawnBullet(
        Vector3 position,
        Vector2 direction)
    {
        Bullet bullet = Instantiate(
            bulletPrefab,
            position,
            Quaternion.identity
        );

        bullet.Initialize(
            direction,
            maxBounces,
            PremiumUpgradeManager.Instance.HasUpgrade(
                UpgradeType.BiggerBullet),
            HandleBulletFinished,
            () =>
            {
                totalBounces++;
                bounceFeedback?.PlayFeedbacks();
            },
            () =>
            {
                hitFeedback?.PlayFeedbacks();
            }
        );
    }

    private void HandleBulletFinished()
    {
        bulletsRemainingThisShot--;

        if (bulletsRemainingThisShot > 0)
            return;

        canShoot = true;

        RoundManager.Instance.CheckRunLost();
    }

    public void ChangeAmmo(int value)
    {
        ammo = Mathf.Max(
            0,
            ammo + value
        );

        OnAmmoChanged?.Invoke(ammo);

        RoundManager.Instance.CheckRunLost();
    }

    private void SetAmmo(int value)
    {
        ammo = Mathf.Max(
            0,
            value
        );

        OnAmmoChanged?.Invoke(ammo);
    }

    public void IncreaseMaxBounces(int amount = 1)
    {
        maxBounces += amount;

        OnMaxBouncesChanged?.Invoke(
            maxBounces
        );
    }

    private void HandleWASDMovement()
    {
        if (!PremiumUpgradeManager.Instance.HasUpgrade(
            UpgradeType.WASDMovement))
            return;

        if (remainingMovementDistance <= 0f)
            return;

        Vector2 input =
            GameInput.Instance.MoveInput;

        if (input.sqrMagnitude <= 0.001f)
            return;

        input = Vector2.ClampMagnitude(
            input,
            1f
        );

        float distance =
            movementSpeed *
            Time.fixedDeltaTime;

        distance = Mathf.Min(
            distance,
            remainingMovementDistance
        );

        Vector2 movement =
            input * distance;

        RaycastHit2D hit = Physics2D.CircleCast(
            rb.position,
            0.5f,
            movement.normalized,
            movement.magnitude,
            movementCollisionMask
        );

        if (hit.collider != null)
        {
            float allowedDistance =
                Mathf.Max(
                    0f,
                    hit.distance - wallSkin
                );

            movement =
                movement.normalized *
                allowedDistance;
        }

        float movedDistance =
            movement.magnitude;

        if (movedDistance <= 0f)
            return;

        rb.MovePosition(
            rb.position + movement
        );

        remainingMovementDistance -=
            movedDistance;

        if (movementSlider != null)
        {
            movementSlider.value =
                remainingMovementDistance /
                movementDistancePerRound;
        }
    }
    private Vector2 GetSafeBulletSpawnPosition(Vector2 firePosition, Vector2 direction)
    {
        direction.Normalize();

        Collider2D firePointWall = Physics2D.OverlapPoint(
            firePosition,
            wallLayer
        );

        if (firePointWall == null)
            return firePosition;

        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            direction,
            Vector2.Distance(transform.position, firePosition) + 1f,
            wallLayer
        );

        if (hit.collider == null)
            return firePosition;

        return hit.point - direction * wallSpawnOffset;
    }
    public void ResetPlayer()
    {
        totalBounces = 0;
        maxBounces = 0;
        ammoUsed = 0;
        canShoot = true;
        bulletsRemainingThisShot = 0;

        SetAmmo(5);

        UIManager.Instance
            .GetCanvas<CanvasGameplay>()
            .SetMoney(0);

        remainingMovementDistance =
            movementDistancePerRound;

        recoilActive = false;

        rb.linearVelocity =
            Vector2.zero;

        rb.angularVelocity = 0f;

        rb.position =
            Vector2.zero;

        transform.position =
            Vector3.zero;

        transform.localRotation =
            baseRotation;

        UpdateSecondaryGunVisibility();
    }
}