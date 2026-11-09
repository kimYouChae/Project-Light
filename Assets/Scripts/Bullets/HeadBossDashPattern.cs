using UnityEngine;

/// <summary>고정 방향 돌진과 플레이어 접촉 및 패링을 처리합니다.</summary>
public sealed class HeadBossDashPattern : MonoBehaviour
{
    // ##TEMP 돌진 프로토타입 시작값.
    [SerializeField, Min(0.01f)] private float dashSpeed = 12f;
    [SerializeField, Min(0.01f)] private float contactRadius = 0.6f;

    private SpriteRenderer playfield;
    private PlayerHealth target;
    private PlayerLight playerLight;
    private PlayerPerfectDodge perfectDodge;
    private Vector2 previousPlayerPosition;
    private Vector3 dashEnd;
    private bool isDashing;
    private bool wasParried;

    /// <summary>이번 돌진이 플레이어 접촉으로 끝났는지 나타냅니다.</summary>
    public bool DidBite { get; private set; }

    /// <summary>돌진 종료 위치를 계산할 필드 표시를 연결합니다.</summary>
    public void Initialize(SpriteRenderer field)
    {
        playfield = field;
    }

    /// <summary>저장된 조준 방향으로 돌진하고 대상의 패링 입력에 등록합니다.</summary>
    public void BeginDash(PlayerHealth player, Vector2 direction)
    {
        if (!isActiveAndEnabled || isDashing || playfield == null
            || player == null || player.IsDead || direction.sqrMagnitude <= 0f)
        {
            return;
        }

        target = player;
        playerLight = player.GetComponent<PlayerLight>();
        perfectDodge = player.GetComponent<PlayerPerfectDodge>();
        previousPlayerPosition = player.transform.position;
        dashEnd = FindDashEnd(direction.normalized);
        DidBite = false;
        wasParried = false;
        isDashing = true;

        if (playerLight != null && perfectDodge != null)
        {
            perfectDodge.RegisterDashPerfect(TryParry);
        }
    }

    private Vector3 FindDashEnd(Vector2 direction)
    {
        // 몸체가 필드 경계를 완전히 지나갈 때까지 돌진한다.
        Bounds bounds = playfield.bounds;
        bounds.Expand(contactRadius * 2f);
        Vector3 start = transform.position;
        float exitX = float.PositiveInfinity;
        float exitY = float.PositiveInfinity;

        if (direction.x != 0f)
        {
            float edgeX = direction.x > 0f ? bounds.max.x : bounds.min.x;
            exitX = (edgeX - start.x) / direction.x;
        }

        if (direction.y != 0f)
        {
            float edgeY = direction.y > 0f ? bounds.max.y : bounds.min.y;
            exitY = (edgeY - start.y) / direction.y;
        }

        float distance = Mathf.Max(0f, Mathf.Min(exitX, exitY));
        return start + (Vector3)(direction * distance);
    }

    private void Update()
    {
        if (!isDashing || Time.deltaTime <= 0f)
        {
            return;
        }

        if (target == null || target.IsDead)
        {
            StopDash();
            return;
        }

        Vector3 start = transform.position;
        Vector3 end = Vector3.MoveTowards(start, dashEnd, dashSpeed * Time.deltaTime);
        Vector2 playerPosition = target.transform.position;
        float combinedRadius = contactRadius + target.BodyRadius;

        if (!wasParried && TryGetContactProgress(
            (Vector2)start - previousPlayerPosition,
            (Vector2)end - playerPosition,
            combinedRadius,
            out float contactProgress))
        {
            transform.position = Vector3.Lerp(start, end, contactProgress);
            StopDash();
            DidBite = true;
            return;
        }

        transform.position = end;
        previousPlayerPosition = playerPosition;

        if (end.Equals(dashEnd))
        {
            StopDash();
        }
    }

    private bool TryGetContactProgress(
        Vector2 start, Vector2 end, float radius, out float progress)
    {
        progress = 0f;
        float distanceSquared = start.sqrMagnitude - radius * radius;

        if (distanceSquared <= 0f)
        {
            return true;
        }

        // 상대 이동 선분이 접촉 원에 처음 닿는 지점을 찾는다.
        Vector2 movement = end - start;
        float movementSquared = movement.sqrMagnitude;
        float approach = Vector2.Dot(start, movement);
        float discriminant = approach * approach - movementSquared * distanceSquared;

        if (movementSquared <= 0f || approach >= 0f || discriminant < 0f)
        {
            return false;
        }

        progress = (-approach - Mathf.Sqrt(discriminant)) / movementSquared;
        return progress >= 0f && progress <= 1f;
    }

    private bool TryParry()
    {
        if (!isDashing || wasParried || playerLight == null || perfectDodge == null
            || !playerLight.IsOnCoreBoundary(
                transform.position, contactRadius, perfectDodge.PerfectTolerance))
        {
            return false;
        }

        wasParried = true;
        return true;
    }

    /// <summary>돌진을 중단하고 등록한 패링 입력을 해제합니다.</summary>
    public void StopDash()
    {
        if (perfectDodge != null)
        {
            perfectDodge.UnregisterDashPerfect(TryParry);
        }

        isDashing = false;
        wasParried = false;
        DidBite = false;
        target = null;
        playerLight = null;
        perfectDodge = null;
    }

    private void OnDisable()
    {
        StopDash();
    }
}
