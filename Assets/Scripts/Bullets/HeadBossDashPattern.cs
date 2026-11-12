using UnityEngine;

/// <summary>고정 방향 돌진과 플레이어 접촉 및 패링을 처리합니다.</summary>
public sealed class HeadBossDashPattern : MonoBehaviour
{
    [SerializeField] private HeadBossBitePattern bitePattern;
    [SerializeField] private HeadBossEntrance entrance;
    [SerializeField] private SpriteRenderer body;
    [SerializeField] private LineRenderer dashParryRing;

    // ##TEMP 돌진 프로토타입 시작값.
    [SerializeField, Min(0.01f)] private float dashSpeed = 12f;
    [SerializeField, Min(0.01f)] private float contactRadius = 0.6f;

    // ##TEMP 돌진 패링 예고 색상.
    [SerializeField] private Color approachingRingColor = new Color(1f, 0.3f, 0.2f);
    [SerializeField] private Color readyRingColor = new Color(1f, 0.9f, 0.35f);

    private SpriteRenderer playfield;
    private Camera fieldCamera;
    private PlayerHealth target;
    private PlayerLight playerLight;
    private PlayerPerfectDodge perfectDodge;
    private Vector2 previousPlayerPosition;
    private Vector3 dashEnd;
    private Vector2 dashDirection;
    private bool isDashing;
    private bool isExiting;
    private bool wasParried;
    private bool hasPassedParryTarget;

    private float ParryEntryDistance =>
        playerLight.CoreRadius + contactRadius + perfectDodge.PerfectTolerance;

    /// <summary>이번 돌진이 플레이어 접촉으로 끝났는지 나타냅니다.</summary>
    public bool DidBite { get; private set; }

    /// <summary>돌진 또는 화면 밖 이탈 이동 중인지 반환합니다.</summary>
    public bool IsMovingAway => isDashing || isExiting;

    /// <summary>돌진 종료 위치를 계산할 필드 표시를 연결합니다.</summary>
    public void Initialize(SpriteRenderer field)
    {
        playfield = field;
        fieldCamera = Camera.main;
    }

    /// <summary>저장된 조준 방향으로 돌진하고 대상의 패링 입력에 등록합니다.</summary>
    public void BeginDash(PlayerHealth player, Vector2 direction)
    {
        if (!isActiveAndEnabled || isDashing || isExiting || playfield == null || entrance == null
            || player == null || player.IsDead || direction.sqrMagnitude <= 0f)
        {
            return;
        }

        target = player;
        playerLight = player.GetComponent<PlayerLight>();
        perfectDodge = player.GetComponent<PlayerPerfectDodge>();
        previousPlayerPosition = player.transform.position;
        dashDirection = direction.normalized;
        dashEnd = FindDashEnd(dashDirection);
        entrance.CancelReentry();
        DidBite = false;
        wasParried = false;
        hasPassedParryTarget = false;
        isDashing = true;
        PlayLog.Write("HeadBoss", $"Dash Start (speed={dashSpeed:F3})");

        if (playerLight != null && perfectDodge != null)
        {
            perfectDodge.RegisterDashPerfect(TryParry);
        }
    }

    private Vector3 FindDashEnd(Vector2 direction)
    {
        // 필드와 카메라 화면을 모두 지난 위치를 계산한다.
        Bounds bounds = playfield.bounds;
        Vector3 start = transform.position;
        if (fieldCamera != null)
        {
            float depth = fieldCamera.WorldToViewportPoint(start).z;
            bounds.Encapsulate(fieldCamera.ViewportToWorldPoint(new Vector3(0f, 0f, depth)));
            bounds.Encapsulate(fieldCamera.ViewportToWorldPoint(new Vector3(0f, 1f, depth)));
            bounds.Encapsulate(fieldCamera.ViewportToWorldPoint(new Vector3(1f, 0f, depth)));
            bounds.Encapsulate(fieldCamera.ViewportToWorldPoint(new Vector3(1f, 1f, depth)));
        }

        Vector3 margin = Vector3.one * contactRadius;
        if (body != null)
        {
            Bounds bodyBounds = body.bounds;
            Vector3 offset = bodyBounds.center - start;
            margin.x = Mathf.Max(margin.x, bodyBounds.extents.x + Mathf.Abs(offset.x));
            margin.y = Mathf.Max(margin.y, bodyBounds.extents.y + Mathf.Abs(offset.y));
        }

        bounds.Expand(margin * 2f);
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
        if (Time.deltaTime <= 0f)
        {
            return;
        }

        if (isExiting)
        {
            UpdateExit();
            return;
        }

        if (!isDashing)
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
            PlayLog.Write("HeadBoss", "Dash Contact");
            PlayerHealth bittenPlayer = target;
            ClearDashInput();

            DidBite = bitePattern != null
                && bitePattern.BeginHold(bittenPlayer, dashDirection, BeginExit);

            if (!DidBite)
            {
                BeginExit();
            }

            return;
        }

        transform.position = end;
        previousPlayerPosition = playerPosition;

        if (end.Equals(dashEnd))
        {
            BeginReturn();
        }
    }

    private void BeginExit()
    {
        // 들어 올리기로 머리 위치가 바뀌었으므로 현재 위치에서 다시 계산한다.
        dashEnd = FindDashEnd(dashDirection);
        isExiting = true;
    }

    private void UpdateExit()
    {
        transform.position = Vector3.MoveTowards(
            transform.position, dashEnd, dashSpeed * Time.deltaTime);

        if (transform.position.Equals(dashEnd))
        {
            BeginReturn();
        }
    }

    private void BeginReturn()
    {
        ClearDashInput();
        isExiting = false;
        PlayLog.Write("HeadBoss", "Dash Return Start");
        entrance.BeginReentry();
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

    private void LateUpdate()
    {
        TryUpdateParryRing();
    }

    private bool TryGetParryApproach(out Vector2 toPlayer)
    {
        toPlayer = Vector2.zero;

        if (!isDashing || wasParried || hasPassedParryTarget
            || target == null || playerLight == null || perfectDodge == null)
        {
            return false;
        }

        toPlayer = (Vector2)(target.transform.position - transform.position);

        if (Vector2.Dot(toPlayer, dashDirection) <= 0f)
        {
            // 머리가 플레이어를 지나면 이번 돌진의 패링 표시를 닫는다.
            hasPassedParryTarget = true;
            return false;
        }

        return true;
    }

    private bool CanParry()
    {
        return TryGetParryApproach(out _)
            && playerLight.IsOnCoreBoundary(
                transform.position, contactRadius, perfectDodge.PerfectTolerance);
    }

    private void TryUpdateParryRing()
    {
        if (!TryGetParryApproach(out Vector2 toPlayer) || dashParryRing == null)
        {
            HideParryRing();
            return;
        }

        Vector3 center = target.transform.position;
        float coreRadius = playerLight.CoreRadius;
        float radius = coreRadius + Mathf.Max(0f, toPlayer.magnitude - ParryEntryDistance);

        Color color = CanParry() ? readyRingColor : approachingRingColor;
        dashParryRing.startColor = dashParryRing.endColor = color;

        for (int index = 0; index < dashParryRing.positionCount; index++)
        {
            float angle = 2f * Mathf.PI * index / dashParryRing.positionCount;
            Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
            dashParryRing.SetPosition(index, center + offset * radius);
        }

        dashParryRing.enabled = true;
    }

    private void HideParryRing()
    {
        if (dashParryRing != null)
        {
            dashParryRing.enabled = false;
        }
    }

    private bool TryParry()
    {
        if (!CanParry())
        {
            return false;
        }

        wasParried = true;
        PlayLog.Write("HeadBoss", "Dash Parry Success");
        HideParryRing();
        return true;
    }

    /// <summary>돌진·물림·이탈·복귀를 중단하고 패링 입력을 해제합니다.</summary>
    public void StopDash()
    {
        if (bitePattern != null)
        {
            bitePattern.ReleaseHold();
        }

        if (entrance != null)
        {
            entrance.CancelReentry();
        }

        isExiting = false;
        DidBite = false;
        ClearDashInput();
    }

    private void ClearDashInput()
    {
        HideParryRing();

        if (perfectDodge != null)
        {
            perfectDodge.UnregisterDashPerfect(TryParry);
        }

        isDashing = false;
        wasParried = false;
        target = null;
        playerLight = null;
        perfectDodge = null;
    }

    private void OnDisable()
    {
        StopDash();
    }
}
