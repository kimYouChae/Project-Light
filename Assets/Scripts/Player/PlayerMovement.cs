using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class PlayerMovement : MonoBehaviour
{
    [Header("이동 설정")]
    // ##TEMP 프로토타입 이동 속도와 몸체 반경. 인스펙터에서 조정한다.
    [SerializeField, Min(0f)] private float moveSpeed = 5f;
    [SerializeField, Min(0f)] private float bodyRadius = 0.25f;
    [SerializeField] private SpriteRenderer playfield;

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || playfield == null)
            return;

        Vector2 direction = new Vector2(
            (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f),
            (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f));
        Move(direction, Time.deltaTime);
    }

    private void Move(Vector2 direction, float deltaTime)
    {
        if (playfield == null)
            return;

        Vector3 position = transform.position;
        Vector2 offset = Vector2.ClampMagnitude(direction, 1f) * moveSpeed * Mathf.Max(0f, deltaTime);
        position.x += offset.x;
        position.y += offset.y;

        // 표시용 바닥의 월드 경계에서 몸체가 벗어나지 않게 제한한다.
        Bounds bounds = playfield.bounds;
        float insetX = Mathf.Min(bodyRadius, bounds.extents.x);
        float insetY = Mathf.Min(bodyRadius, bounds.extents.y);
        position.x = Mathf.Clamp(position.x, bounds.min.x + insetX, bounds.max.x - insetX);
        position.y = Mathf.Clamp(position.y, bounds.min.y + insetY, bounds.max.y - insetY);
        transform.position = position;
    }

    private void OnValidate()
    {
        moveSpeed = Mathf.Max(0f, moveSpeed);
        bodyRadius = Mathf.Max(0f, bodyRadius);
    }
}
