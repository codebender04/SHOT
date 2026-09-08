using DG.Tweening;
using UnityEngine;

public class MovingEnemy : Enemy
{
    [Header("Movement")]
    [SerializeField] private Transform visualTransform;
    [SerializeField] private float stepDistance = 0.5f;
    [SerializeField] private float stepDuration = 0.25f;
    [SerializeField] private float waypointDistance = 2f;
    [SerializeField] private float waypointPadding = 0.5f;

    [Header("Step Animation")]
    [SerializeField] private float stepHeight = 0.08f;
    [SerializeField] private float stepRotation = 3f;

    private Vector2[] waypoints;
    private int waypointIndex;
    private Vector3 visualBasePosition;
    private Quaternion visualBaseRotation;
    private Sequence stepSequence;
    private bool stepFlip;

    private float MoveSpeed => stepDistance / stepDuration;

    private void Start()
    {
        visualBasePosition = visualTransform.localPosition;
        visualBaseRotation = visualTransform.localRotation;

        GenerateWaypoints();
        StartNextLeg();
    }

    private void GenerateWaypoints()
    {
        if (ArenaManager.Instance == null)
            return;

        waypoints = new Vector2[2];

        waypoints[0] = ArenaManager.Instance.GetRandomPosition(
            transform.position,
            waypointDistance,
            waypointPadding
        );

        waypoints[1] = ArenaManager.Instance.GetRandomPosition(
            waypoints[0],
            waypointDistance,
            waypointPadding
        );
    }

    private void StartNextLeg()
    {
        if (IsDead || waypoints == null)
            return;

        Vector2 target = waypoints[waypointIndex];
        waypointIndex = (waypointIndex + 1) % waypoints.Length;

        Vector2 currentPosition = transform.position;
        float distance = Vector2.Distance(currentPosition, target);

        if (distance <= 0.01f)
        {
            StartNextLeg();
            return;
        }

        float legDuration = distance / MoveSpeed;
        int numSteps = Mathf.Max(1, Mathf.CeilToInt(distance / stepDistance));
        float stepDurationActual = legDuration / numSteps;

        stepSequence?.Kill();
        stepSequence = DOTween.Sequence();

        stepSequence.Insert(
            0f,
            transform.DOMove(target, legDuration)
                .SetEase(Ease.Linear)
        );

        for (int i = 0; i < numSteps; i++)
        {
            float stepStart = i * stepDurationActual;
            float halfStep = stepDurationActual * 0.5f;

            float signedRotation = stepFlip
                ? stepRotation
                : -stepRotation;

            stepFlip = !stepFlip;

            stepSequence.Insert(
                stepStart,
                visualTransform.DOLocalMoveY(
                    visualBasePosition.y + stepHeight,
                    halfStep
                ).SetEase(Ease.OutSine)
            );

            stepSequence.Insert(
                stepStart + halfStep,
                visualTransform.DOLocalMoveY(
                    visualBasePosition.y,
                    halfStep
                ).SetEase(Ease.InSine)
            );

            stepSequence.Insert(
                stepStart,
                visualTransform.DOLocalRotate(
                    new Vector3(0f, 0f, signedRotation),
                    halfStep
                ).SetEase(Ease.OutSine)
            );

            stepSequence.Insert(
                stepStart + halfStep,
                visualTransform.DOLocalRotate(
                    visualBaseRotation.eulerAngles,
                    halfStep
                ).SetEase(Ease.InSine)
            );
        }

        stepSequence.OnComplete(StartNextLeg);
    }

    public override void FinishDeath()
    {
        stepSequence?.Kill();

        visualTransform.localPosition = visualBasePosition;
        visualTransform.localRotation = visualBaseRotation;

        base.FinishDeath();
    }

    private void OnDestroy()
    {
        stepSequence?.Kill();
    }
}