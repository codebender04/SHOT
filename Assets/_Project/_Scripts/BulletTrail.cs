using UnityEngine;

public class BulletTrail : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private float width = 0.08f;
    [SerializeField] private float maxLength = 0.6f;

    private Vector2 previousPosition;

    public void Initialize(Vector2 position)
    {
        previousPosition = position;

        transform.position = position;
        transform.localScale = Vector3.zero;
    }

    public void UpdateTrail(Vector2 currentPosition)
    {
        Vector2 movement = currentPosition - previousPosition;

        float length = movement.magnitude;

        if (length <= 0.001f)
            return;

        length = Mathf.Min(length, maxLength);

        Vector2 direction = movement.normalized;

        // Trail goes behind the bullet.
        Vector2 center = currentPosition - direction * (length * 0.5f);

        transform.position = center;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        transform.localScale = new Vector3(
            length,
            width,
            1f
        );

        previousPosition = currentPosition;
    }

    public void ResetPosition(Vector2 position)
    {
        previousPosition = position;
        transform.position = position;
        transform.localScale = Vector3.zero;
    }
}