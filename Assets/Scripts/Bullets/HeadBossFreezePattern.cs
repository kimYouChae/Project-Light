using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>서치라이트 안의 이동을 감지하고 굳은 플레이어에게 빛 원의 초점을 맞춥니다.</summary>
public sealed class HeadBossFreezePattern : MonoBehaviour
{
    // ##TEMP 승인한 감지 시작값.
    [SerializeField, Min(0.01f)] private float duration = 2f;
    [SerializeField, Min(0f)] private float movementThreshold = 0.05f;
    // ##TEMP 이동 거리 비교에 사용하는 시간 구간.
    [SerializeField, Min(0.01f)] private float movementSampleWindow = 0.1f;
    // ##TEMP 그림자에서 벗어난 뒤 이동 판정을 유예하는 시간.
    [SerializeField, Min(0f)] private float shadowExitGraceDuration = 0.1f;

    // ##TEMP 굳기 중 빛 원 반경이 줄어드는 속도. 월드 유닛/초.
    [SerializeField, Min(0.01f)] private float focusShrinkSpeed = 1.5f;
    // ##TEMP 머리 표시의 움찔 거리와 복원 시간.
    [SerializeField] private Transform headVisual;
    [SerializeField, Min(0f)] private float recoilDistance = 0.12f;
    [SerializeField, Min(0.01f)] private float recoilDuration = 0.15f;

    private PlayerHealth playerHealth;
    private PlayerMovement playerMovement;
    private PlayerLight playerLight;
    private PlayerPerfectDodge perfectDodge;
    private HeadBossSearchlightVisual focusVisual;
    private Vector2 focusCenter;
    private float focusStartRadius;
    private double focusStartedAt;
    private double focusContactAt;
    private double focusEndsAt;
    private Vector3 headRestPosition;
    private float recoilRemaining;

    private Vector2 detectionCenter;
    private float detectionRadius;
    private double detectionStartedAt;
    private Func<Vector2, bool> isSightBlocked;
    private bool wasSightBlocked;
    private double shadowExitGraceEndsAt;

    private readonly Queue<(double Time, Vector2 Position)> movementSamples =
        new Queue<(double Time, Vector2 Position)>();

    private (double Time, Vector2 Position) previousMovementSample;

    private bool isDetecting;
    // 감지 한 번에 굳기를 한 번만 발동한다.
    private bool hasTriggeredFreeze;
    private bool isComplete;

    /// <summary>감지 구간이 정상 종료됐는지 반환합니다.</summary>
    public bool IsComplete => isComplete;

    /// <summary>이동 감지 구간이 진행 중인지 나타냅니다.</summary>
    public bool IsDetecting => isDetecting;

    /// <summary>이번 감지에서 플레이어 이동을 잠갔는지 나타냅니다.</summary>
    public bool HasTriggeredFreeze => hasTriggeredFreeze;

    /// <summary>이동 감지와 초점 판정에 필요한 플레이어 참조를 연결합니다.</summary>
    public void Initialize(PlayerHealth player)
    {
        playerHealth = player;
        playerMovement = player != null ? player.GetComponent<PlayerMovement>() : null;
        playerLight = player != null ? player.GetComponent<PlayerLight>() : null;
        perfectDodge = player != null ? player.GetComponent<PlayerPerfectDodge>() : null;
    }

    /// <summary>포착된 서치라이트 원을 기준으로 이동 감지를 시작합니다.</summary>
    public void BeginDetection(
        Vector2 center, float radius, HeadBossSearchlightVisual visual,
        Func<Vector2, bool> isSightBlocked = null)
    {
        if (!isActiveAndEnabled || isDetecting)
        {
            return;
        }

        if (playerHealth == null || playerMovement == null || playerLight == null
            || perfectDodge == null || visual == null)
        {
            PlayLog.Write("HeadBoss",
                "서치라이트 초점 판정의 플레이어와 표시 참조를 확인해주세요.",
                LogType.Error, this);
            return;
        }

        if (playerHealth.IsDead)
        {
            return;
        }

        detectionCenter = center;
        detectionRadius = radius;
        focusVisual = visual;
        detectionStartedAt = Time.timeAsDouble;
        this.isSightBlocked = isSightBlocked;
        wasSightBlocked = isSightBlocked?.Invoke(playerHealth.transform.position) == true;
        shadowExitGraceEndsAt = 0d;

        movementSamples.Clear();
        previousMovementSample = (0d, (Vector2)playerHealth.transform.position);

        hasTriggeredFreeze = false;
        isComplete = false;
        isDetecting = true;
        PlayLog.Write("HeadBoss", $"Detection Start ({duration:F3}s)");
    }

    private void Update()
    {
        if (!isDetecting || Time.timeScale <= 0f)
        {
            return;
        }

        if (playerHealth == null || playerHealth.IsDead)
        {
            StopDetection();
            return;
        }

        if (hasTriggeredFreeze)
        {
            AdvanceFocus();
            return;
        }

        double elapsed = Time.timeAsDouble - detectionStartedAt;
        if (elapsed >= duration)
        {
            StopDetection();
            isComplete = true;
            return;
        }

        Vector2 playerPosition = playerHealth.transform.position;
        bool sightBlocked = isSightBlocked?.Invoke(playerPosition) == true;
        if (sightBlocked || wasSightBlocked)
        {
            movementSamples.Clear();
            previousMovementSample = (elapsed, playerPosition);
            if (!sightBlocked)
            {
                shadowExitGraceEndsAt = elapsed + shadowExitGraceDuration;
            }

            wasSightBlocked = sightBlocked;
            return;
        }

        bool hasMoved = HasMovedWithinWindow(elapsed, playerPosition);
        if (elapsed < shadowExitGraceEndsAt)
        {
            return;
        }

        bool isInsideCircle =
            (playerPosition - detectionCenter).sqrMagnitude <= detectionRadius * detectionRadius;

        if (!isInsideCircle || !hasMoved)
        {
            return;
        }

        hasTriggeredFreeze = true;
        movementSamples.Clear();
        PlayLog.Write("HeadBoss", "Freeze Start");
        BeginFocus(playerPosition);
    }

    private void BeginFocus(Vector2 playerPosition)
    {
        focusCenter = playerPosition;
        float coreRadius = playerLight.CoreRadius;
        float grace = perfectDodge.PerfectGraceSeconds;
        focusStartRadius = Mathf.Max(detectionRadius, coreRadius + focusShrinkSpeed * grace);
        focusStartedAt = Time.timeAsDouble;
        focusContactAt = focusStartedAt + (focusStartRadius - coreRadius) / focusShrinkSpeed;
        focusEndsAt = Math.Min(focusContactAt + grace,
            focusStartedAt + focusStartRadius / focusShrinkSpeed);

        playerMovement.SetFrozen(true);
        perfectDodge.RegisterSearchlightPerfect(GetFocusSecondsToContact, CompleteFocusPerfect);
        focusVisual.Show(focusCenter, focusStartRadius);
        focusVisual.SetFocusProgress(0f);
    }

    private void AdvanceFocus()
    {
        double now = Time.timeAsDouble;
        if (now >= focusEndsAt)
        {
            FinishFocus(success: false);
            return;
        }

        double elapsed = now - focusStartedAt;
        float radius = Mathf.Max(0f, focusStartRadius - focusShrinkSpeed * (float)elapsed);
        float progress = Mathf.Clamp01((float)(elapsed / (focusEndsAt - focusStartedAt)));
        focusVisual.Show(focusCenter, radius);
        focusVisual.SetFocusProgress(progress);
    }

    private double GetFocusSecondsToContact()
    {
        double now = Time.timeAsDouble;
        // 원이 완전히 닫히면 입력 Update가 먼저 실행돼도 판정을 닫는다.
        if (now >= focusStartedAt + focusStartRadius / focusShrinkSpeed)
        {
            return double.NaN;
        }

        return focusContactAt - now;
    }

    private void CompleteFocusPerfect()
    {
        FinishFocus(success: true);
    }

    private void FinishFocus(bool success)
    {
        PlayLog.Write("HeadBoss", success ? "Searchlight Focus Perfect" : "Searchlight Focus Failed");
        StopDetection();
        isComplete = true;

        if (success)
        {
            playerLight.FlashSearchlightPerfect();
            BeginRecoil();
        }
        else
        {
            playerHealth.TryTakeHit(1);
        }
    }

    private void BeginRecoil()
    {
        if (headVisual == null)
        {
            return;
        }

        RestoreHeadPose();
        headRestPosition = headVisual.localPosition;
        recoilRemaining = recoilDuration;
        headVisual.localPosition = headRestPosition + Vector3.up * recoilDistance;
    }

    private void LateUpdate()
    {
        if (recoilRemaining <= 0f || headVisual == null)
        {
            return;
        }

        recoilRemaining = Mathf.Max(0f, recoilRemaining - Time.deltaTime);
        headVisual.localPosition = headRestPosition
            + Vector3.up * (recoilDistance * recoilRemaining / recoilDuration);
    }

    private void RestoreHeadPose()
    {
        if (recoilRemaining > 0f && headVisual != null)
        {
            headVisual.localPosition = headRestPosition;
        }

        recoilRemaining = 0f;
    }

    private bool HasMovedWithinWindow(double elapsed, Vector2 position)
    {
        if (elapsed > 0d)
        {
            movementSamples.Enqueue((elapsed, position));
        }

        double comparisonTime = elapsed - movementSampleWindow;
        if (comparisonTime < previousMovementSample.Time)
        {
            return false;
        }

        // 비교 시각 앞뒤의 위치를 보간해 프레임 간격이 달라도 같은 시간 차이를 사용한다.
        while (movementSamples.TryPeek(out var sample) && sample.Time <= comparisonTime)
        {
            previousMovementSample = movementSamples.Dequeue();
        }

        var nextSample = movementSamples.Peek();
        float blend = (float)((comparisonTime - previousMovementSample.Time)
            / (nextSample.Time - previousMovementSample.Time));
        Vector2 comparisonPosition =
            Vector2.Lerp(previousMovementSample.Position, nextSample.Position, blend);

        return (position - comparisonPosition).sqrMagnitude
            > movementThreshold * movementThreshold;
    }

    /// <summary>
    /// 이동 감지를 중단하고 이 패턴이 건 이동 잠금을 풉니다.
    /// HeadBossSearchlightPattern.StopTracking과 비활성 처리에서 호출합니다.
    /// </summary>
    public void StopDetection()
    {
        if (isDetecting)
        {
            PlayLog.Write("HeadBoss", $"Detection End (frozen={hasTriggeredFreeze})");
        }

        if (hasTriggeredFreeze && playerMovement != null)
        {
            playerMovement.SetFrozen(false);
        }

        movementSamples.Clear();
        if (hasTriggeredFreeze)
        {
            if (perfectDodge != null)
            {
                perfectDodge.UnregisterSearchlightPerfect(GetFocusSecondsToContact);
            }

            if (focusVisual != null)
            {
                focusVisual.Hide();
            }
        }

        focusVisual = null;
        isSightBlocked = null;
        wasSightBlocked = false;
        shadowExitGraceEndsAt = 0d;
        isDetecting = false;
        hasTriggeredFreeze = false;
        isComplete = false;
    }

    private void OnDisable()
    {
        StopDetection();
        RestoreHeadPose();
    }

    private void OnValidate()
    {
        duration = Mathf.Max(0.01f, duration);
        movementThreshold = Mathf.Max(0f, movementThreshold);
        movementSampleWindow = Mathf.Max(0.01f, movementSampleWindow);
        shadowExitGraceDuration = Mathf.Max(0f, shadowExitGraceDuration);
        focusShrinkSpeed = Mathf.Max(0.01f, focusShrinkSpeed);
        recoilDistance = Mathf.Max(0f, recoilDistance);
        recoilDuration = Mathf.Max(0.01f, recoilDuration);
    }
}
