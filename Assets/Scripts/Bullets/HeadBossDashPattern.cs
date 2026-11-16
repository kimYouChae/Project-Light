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
    // ##TEMP 패링 결과의 표시 시간과 빗나감 확장 거리.
    [SerializeField, Min(0.01f)] private float resultDuration = 0.15f;
    [SerializeField, Min(0f)] private float missExpansion = 0.2f;
    [SerializeField] private Color missRingColor = Color.gray;
    [SerializeField] private Color biteRingColor = Color.red;
    // ##TEMP 물림 시작과 해제 시 색 전환 시간.
    [SerializeField, Min(0.01f)] private float biteColorTransitionDuration = 0.3f;

    private SpriteRenderer playfield;
    private Camera fieldCamera;
    private PlayerHealth target;
    private PlayerLight playerLight;
    private PlayerPerfectDodge perfectDodge;
    private Vector2 previousPlayerPosition;
    private PlayerMovement targetMovement;
    private double? lastParryContactAt;
    private Vector3 dashEnd;
    private Vector2 dashDirection;
    private bool isDashing;
    private bool isExiting;
    private bool wasParried;
    private bool hasPassedParryTarget;
    private bool wasInParryPath;
    private bool hidRingOutsidePath;
    private BossHealth bossHealth;
    private PlayerFrenzy pausedFrenzy;
    private bool awaitingReentry;
    private Vector3 ringCenter;
    private float ringRadius;
    private Color resultColor;
    private float resultExpansion;
    private float resultRemaining;
    private PlayerMovement biteRingTarget;
    private Color biteRingColorAtRelease;
    private float biteRingElapsed;
    private bool biteRingReleasing;

    private float ParryEntryDistance =>
        playerLight.CoreRadius + contactRadius;

    /// <summary>이번 돌진이 플레이어 접촉으로 끝났는지 나타냅니다.</summary>
    public bool DidBite { get; private set; }

    /// <summary>돌진 또는 화면 밖 이탈 이동 중인지 반환합니다.</summary>
    public bool IsMovingAway => isDashing || isExiting;

    /// <summary>돌진 종료 위치를 계산할 필드 표시를 연결합니다.</summary>
    public void Initialize(SpriteRenderer field)
    {
        playfield = field;
        fieldCamera = Camera.main;
        bossHealth = GetComponent<BossHealth>();
    }

    /// <summary>저장된 조준 방향으로 돌진하고 대상의 패링 입력에 등록합니다.</summary>
    public void BeginDash(PlayerHealth player, Vector2 direction)
    {
        if (!isActiveAndEnabled || isDashing || isExiting || awaitingReentry
            || pausedFrenzy != null || (bossHealth != null && bossHealth.IsPlayerAttackBlocked)
            || playfield == null || entrance == null
            || player == null || player.IsDead || direction.sqrMagnitude <= 0f)
        {
            return;
        }

        target = player;
        playerLight = player.GetComponent<PlayerLight>();
        perfectDodge = player.GetComponent<PlayerPerfectDodge>();
        previousPlayerPosition = player.transform.position;
        targetMovement = player.GetComponent<PlayerMovement>();
        lastParryContactAt = null;
        dashDirection = direction.normalized;
        dashEnd = FindDashEnd(dashDirection);
        entrance.CancelReentry();
        HideParryRing();
        DidBite = false;
        wasParried = false;
        hasPassedParryTarget = false;
        wasInParryPath = false;
        hidRingOutsidePath = false;
        isDashing = true;
        BeginDashRestrictions(player);
        PlayLog.Write("HeadBoss", $"Dash Start (speed={dashSpeed:F3})");

        if (playerLight != null && perfectDodge != null)
        {
            perfectDodge.RegisterDashPerfect(GetDashSecondsToContact, CompleteDashPerfect);
        }
    }

    private void BeginDashRestrictions(PlayerHealth player)
    {
        pausedFrenzy = player.GetComponent<PlayerFrenzy>();
        if (pausedFrenzy != null)
            pausedFrenzy.PauseCountdownForDash(this);
        if (bossHealth != null)
            bossHealth.BeginPlayerAttackBlock(transform.position);
    }

    private void EndDashRestrictions()
    {
        awaitingReentry = false;
        if (bossHealth != null)
            bossHealth.EndPlayerAttackBlock();
        if (pausedFrenzy != null)
            pausedFrenzy.ResumeCountdownForDash(this);
        pausedFrenzy = null;
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

        if (awaitingReentry)
        {
            if (entrance.IsReentryComplete)
                EndDashRestrictions();
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
        Vector2 relativeStart = (Vector2)start - previousPlayerPosition;
        Vector2 relativeEnd = (Vector2)end - playerPosition;

        if (!wasParried && TryGetContactProgress(
            relativeStart, relativeEnd,
            combinedRadius,
            out float contactProgress))
        {
            transform.position = Vector3.Lerp(start, end, contactProgress);
            PlayLog.Write("HeadBoss", "Dash Contact");
            PlayerHealth bittenPlayer = target;
            ClearDashInput();

            DidBite = bitePattern != null
                && bitePattern.BeginHold(bittenPlayer, dashDirection, BeginExit);

            if (DidBite)
            {
                BeginBiteRing(bittenPlayer);
            }
            else
            {
                ShowParryResult(missRingColor, missExpansion);
                BeginExit();
            }

            return;
        }

        if (!wasParried && playerLight != null
            && TryGetParryCrossing(relativeStart, relativeEnd, out float crossing))
        {
            lastParryContactAt = Time.timeAsDouble - Time.deltaTime * (1d - crossing);
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
        if (!DidBite && resultRemaining <= 0f)
            ShowParryResult(missRingColor, missExpansion);
        ClearDashInput();
        isExiting = false;
        awaitingReentry = true;
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
        if (biteRingTarget != null)
        {
            AdvanceBiteRing();
            return;
        }

        if (resultRemaining > 0f)
        {
            AdvanceParryResult();
            return;
        }

        TryUpdateParryRing();
    }

    private void BeginBiteRing(PlayerHealth player)
    {
        PlayerLight light = player.GetComponent<PlayerLight>();
        PlayerMovement movement = player.GetComponent<PlayerMovement>();
        if (dashParryRing == null || light == null || movement == null)
        {
            return;
        }

        biteRingTarget = movement;
        biteRingElapsed = 0f;
        biteRingReleasing = false;
        resultRemaining = 0f;
        ringCenter = player.transform.position;
        ringRadius = light.CoreRadius;
        DrawParryRing(ringRadius, readyRingColor);
    }

    private void AdvanceBiteRing()
    {
        ringCenter = biteRingTarget.transform.position;
        float progress = Mathf.Clamp01(biteRingElapsed / biteColorTransitionDuration);
        if (!biteRingReleasing && !biteRingTarget.IsHeldByBite)
        {
            biteRingColorAtRelease = Color.Lerp(readyRingColor, biteRingColor, progress);
            biteRingReleasing = true;
            biteRingElapsed = 0f;
            progress = 0f;
        }

        Color color = biteRingReleasing
            ? Color.Lerp(biteRingColorAtRelease, readyRingColor, progress)
            : Color.Lerp(readyRingColor, biteRingColor, progress);
        DrawParryRing(ringRadius, color);

        if (biteRingReleasing && progress >= 1f)
        {
            HideParryRing();
            return;
        }

        biteRingElapsed = Mathf.Min(
            biteRingElapsed + Time.deltaTime, biteColorTransitionDuration);
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

        if (Vector2.Dot(toPlayer, dashDirection) <= 0f
            && (!lastParryContactAt.HasValue
                || !perfectDodge.IsWithinPerfectWindow(lastParryContactAt.Value - Time.timeAsDouble)))
        {
            // 머리가 플레이어를 지나면 이번 돌진의 패링 표시를 닫는다.
            hasPassedParryTarget = true;
            return false;
        }

        return true;
    }

    private bool TryGetParryCrossing(Vector2 start, Vector2 end, out float progress)
    {
        progress = 0f;
        Vector2 movement = end - start;
        float radius = ParryEntryDistance;
        float distanceSquared = start.sqrMagnitude - radius * radius;
        float approach = Vector2.Dot(start, movement);
        float discriminant = approach * approach - movement.sqrMagnitude * distanceSquared;
        return distanceSquared >= 0f && approach < 0f && discriminant > 0f
            && TryGetContactProgress(start, end, radius, out progress);
    }

    private double GetDashSecondsToContact()
    {
        if (!isDashing || wasParried || target == null || target.IsDead)
            return double.NaN;

        double now = Time.timeAsDouble;
        float grace = perfectDodge.PerfectGraceSeconds;
        if (lastParryContactAt.HasValue && now - lastParryContactAt.Value <= grace)
            return lastParryContactAt.Value - now;

        double predicted = PredictParryContactSeconds(grace);
        return double.IsNaN(predicted) && lastParryContactAt.HasValue
            ? lastParryContactAt.Value - now : predicted;
    }

    private double PredictParryContactSeconds(float grace)
    {
        Vector2 head = transform.position;
        Vector2 playerPosition = target.transform.position;
        Vector2 playerVelocity = targetMovement != null
            ? targetMovement.GetRequestedVelocity() : Vector2.zero;
        float horizon = Mathf.Min(grace, Vector2.Distance(head, (Vector2)dashEnd) / dashSpeed);
        Vector2 relativeStart = head - playerPosition;
        if (horizon <= 0f)
        {
            float radius = ParryEntryDistance;
            return relativeStart.sqrMagnitude == radius * radius
                && Vector2.Dot(relativeStart, dashDirection * dashSpeed - playerVelocity) < 0f
                ? 0d : double.NaN;
        }

        // 각 축이 경계에 닿는 시각에서 나눠 구간별 상대 이동을 계산한다.
        Vector2 unclampedEnd = playerPosition + playerVelocity * horizon;
        Vector2 clampedEnd = targetMovement != null
            ? (Vector2)targetMovement.ClampToPlayfield(unclampedEnd) : unclampedEnd;
        float xStop = playerVelocity.x == 0f ? horizon
            : Mathf.Clamp((clampedEnd.x - playerPosition.x) / playerVelocity.x, 0f, horizon);
        float yStop = playerVelocity.y == 0f ? horizon
            : Mathf.Clamp((clampedEnd.y - playerPosition.y) / playerVelocity.y, 0f, horizon);

        float segmentStart = 0f;
        for (int segment = 0; segment < 3; segment++)
        {
            float segmentEnd = segment == 0 ? Mathf.Min(xStop, yStop)
                : segment == 1 ? Mathf.Max(xStop, yStop) : horizon;
            if (segmentEnd <= segmentStart)
                continue;

            Vector2 predictedPlayer = playerPosition + new Vector2(
                playerVelocity.x * Mathf.Min(segmentEnd, xStop),
                playerVelocity.y * Mathf.Min(segmentEnd, yStop));
            Vector2 relativeEnd = head + dashDirection * (dashSpeed * segmentEnd) - predictedPlayer;
            if (TryGetParryCrossing(relativeStart, relativeEnd, out float crossing))
                return Mathf.Lerp(segmentStart, segmentEnd, crossing);

            relativeStart = relativeEnd;
            segmentStart = segmentEnd;
        }

        return double.NaN;
    }

    private void TryUpdateParryRing()
    {
        if (!TryGetParryApproach(out Vector2 toPlayer) || dashParryRing == null)
        {
            if (isDashing && !wasParried && hasPassedParryTarget)
                ShowParryResult(missRingColor, missExpansion);
            else
                HideParryRing();
            return;
        }

        float lateralDistance = Mathf.Abs(toPlayer.x * dashDirection.y - toPlayer.y * dashDirection.x);
        bool isInParryPath = lateralDistance <= ParryEntryDistance;
        if (wasInParryPath && !isInParryPath)
        {
            wasInParryPath = false;
            hidRingOutsidePath = true;
            ShowParryResult(missRingColor, missExpansion);
            return;
        }

        if (hidRingOutsidePath && !isInParryPath)
            return;

        hidRingOutsidePath = false;
        wasInParryPath = isInParryPath;
        ringCenter = target.transform.position;
        float coreRadius = playerLight.CoreRadius;
        ringRadius = coreRadius + Mathf.Max(0f, toPlayer.magnitude - ParryEntryDistance);

        Color color = perfectDodge.IsWithinPerfectWindow(GetDashSecondsToContact())
            ? readyRingColor : approachingRingColor;
        DrawParryRing(ringRadius, color);
    }

    private void DrawParryRing(float radius, Color color)
    {
        dashParryRing.startColor = dashParryRing.endColor = color;

        for (int index = 0; index < dashParryRing.positionCount; index++)
        {
            float angle = 2f * Mathf.PI * index / dashParryRing.positionCount;
            Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
            dashParryRing.SetPosition(index, ringCenter + offset * radius);
        }

        dashParryRing.enabled = true;
    }

    private void ShowParryResult(Color color, float expansion)
    {
        if (dashParryRing == null || !dashParryRing.enabled)
            return;

        resultColor = color;
        resultExpansion = expansion;
        resultRemaining = resultDuration;
        DrawParryRing(ringRadius, resultColor);
    }

    private void AdvanceParryResult()
    {
        resultRemaining = Mathf.Max(0f, resultRemaining - Time.deltaTime);
        if (resultRemaining <= 0f)
        {
            HideParryRing();
            return;
        }

        float progress = 1f - resultRemaining / resultDuration;
        Color color = resultColor;
        color.a *= 1f - progress;
        DrawParryRing(ringRadius + resultExpansion * progress, color);
    }

    private void HideParryRing()
    {
        biteRingTarget = null;
        resultRemaining = 0f;
        if (dashParryRing != null)
        {
            dashParryRing.enabled = false;
        }
    }

    private void CompleteDashPerfect()
    {
        wasParried = true;
        PlayLog.Write("HeadBoss", "Dash Parry Success");
        ShowParryResult(readyRingColor, 0f);
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
        HideParryRing();
        EndDashRestrictions();
    }

    private void ClearDashInput()
    {
        if (perfectDodge != null)
        {
            perfectDodge.UnregisterDashPerfect(GetDashSecondsToContact);
        }

        isDashing = false;
        wasParried = false;
        target = null;
        targetMovement = null;
        lastParryContactAt = null;
        playerLight = null;
        perfectDodge = null;
    }

    private void OnDisable()
    {
        StopDash();
    }
}
