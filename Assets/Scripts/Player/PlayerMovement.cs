using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerStats))]
public sealed class PlayerMovement : MonoBehaviour
{
    [Header("===이동 설정===")]
    // ##TEMP 프로토타입 이동 속도와 몸체 반경. 인스펙터에서 조정한다.
    [SerializeField] private PlayerStats stats;
    [SerializeField, Min(0f)] private float bodyRadius = 0.25f;
    [SerializeField] private SpriteRenderer playfield;

    private bool isFrozen;

    /// <summary>이동 경계와 몸체 피격 판정에 사용하는 월드 반경.</summary>
    public float BodyRadius => bodyRadius;

    /// <summary>굳기로 이동이 잠겨 있는지 나타냅니다.</summary>
    public bool IsFrozen => isFrozen;

    /// <summary>보스에게 물려 이동이 잠겼는지 나타냅니다.</summary>
    public bool IsHeldByBite { get; private set; }

    /// <summary>물림에 따른 이동 잠금을 설정하거나 해제합니다.</summary>
    public void SetBiteHeld(bool held)
    {
        IsHeldByBite = held;
    }

    /// <summary>굳기 상태에 따라 WASD 이동을 잠그거나 풉니다.</summary>
    public void SetFrozen(bool frozen)
    {
        isFrozen = frozen;
    }

    private void Awake() => stats = GetComponent<PlayerStats>();

    private void Update()
    {
        // 시간 정지, 굳기 또는 물림 중에는 이동 입력을 처리하지 않는다.
        if (Time.timeScale <= 0f || isFrozen || IsHeldByBite)
        {
            return;
        }

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
        if (playfield == null || stats == null)
            return;

        Vector3 position = transform.position;
        Vector2 offset = Vector2.ClampMagnitude(direction, 1f) * stats.MoveSpeed * Mathf.Max(0f, deltaTime);
        position.x += offset.x;
        position.y += offset.y;

        transform.position = ClampToPlayfield(position);
    }

    /// <summary>플레이어 몸체가 필드 안에 들어오도록 월드 위치를 제한합니다.</summary>
    public Vector3 ClampToPlayfield(Vector3 position)
    {
        if (playfield == null)
        {
            return position;
        }

        Bounds bounds = playfield.bounds;
        float insetX = Mathf.Min(bodyRadius, bounds.extents.x);
        float insetY = Mathf.Min(bodyRadius, bounds.extents.y);
        position.x = Mathf.Clamp(position.x, bounds.min.x + insetX, bounds.max.x - insetX);
        position.y = Mathf.Clamp(position.y, bounds.min.y + insetY, bounds.max.y - insetY);
        return position;
    }

    private void OnValidate()
    {
        bodyRadius = Mathf.Max(0f, bodyRadius);
    }
}
