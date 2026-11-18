using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>스페이스 입력 순간에 코어 경계의 탄을 검사하고 무적·게이지를 부여한다.</summary>
[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerHealth), typeof(PlayerLight))]
[RequireComponent(typeof(PlayerFrenzy))]
[RequireComponent(typeof(PlayerStats))]
public sealed class PlayerPerfectDodge : MonoBehaviour
{
    [Header("탄 관리자")]
    [SerializeField] private BulletRegistry bulletRegistry;

    [Header("퍼펙트 회피")]
    // ##TEMP 승인한 프로토타입 시작값. 인스펙터에서 조정한다.
    [SerializeField] private PlayerStats stats;
    [SerializeField, Min(0f)] private float failureLockDuration = 0.5f;
    [SerializeField, Min(1)] private int simultaneousBonusLimit = 3;
    // ##TEMP 판정선 접촉 시각 앞뒤로 적용하는 게임 시간.
    [SerializeField, Min(0f)] private float perfectGraceSeconds = 0.1f;

    public float PerfectGraceSeconds => perfectGraceSeconds;

    [Header("게이지")]
    // ##TEMP 승인한 프로토타입 시작값.
    [SerializeField, Min(1f)] private float maxGauge = 100f;
    [SerializeField] private float currentGauge;

    private PlayerHealth playerHealth;
    private PlayerMovement playerMovement;
    private PlayerFrenzy frenzy;
    private float nextInputTime;
    private bool hasPendingPerfectInput;
    private bool processInputAfterPending;
    private double perfectPressedAt;
    private double perfectGraceAtInput;
    private double pendingPerfectEndsAt;
    private int pendingSuccessCount;
    private Func<double> getDashSecondsToContact;
    private Action completeDashPerfect;
    private Func<double> getTailStabSecondsToContact;
    private Action completeTailStabPerfect;
    private Func<double> getSearchlightSecondsToContact;
    private Action completeSearchlightPerfect;
    private Func<double> getBiteBraceSecondsToContact;
    private Action completeBiteBrace;

    /// <summary>빛 원 조이기 판정을 스페이스 입력에 연결합니다.</summary>
    public void RegisterSearchlightPerfect(Func<double> getSecondsToContact, Action completePerfect)
    {
        getSearchlightSecondsToContact = getSecondsToContact;
        completeSearchlightPerfect = completePerfect;
    }

    /// <summary>같은 빛 원 조이기가 등록한 입력 판정을 해제합니다.</summary>
    public void UnregisterSearchlightPerfect(Func<double> getSecondsToContact)
    {
        if (getSearchlightSecondsToContact == getSecondsToContact)
        {
            getSearchlightSecondsToContact = null;
            completeSearchlightPerfect = null;
        }
    }

    /// <summary>현재 충전된 퍼펙트 게이지.</summary>
    public float CurrentGauge => frenzy != null && frenzy.IsActive
        ? maxGauge * frenzy.RemainingRatio : currentGauge;

    /// <summary>게이지 최대 충전량.</summary>
    public float MaxGauge => maxGauge;


    /// <summary>축적된 퍼펙트 게이지를 초기화합니다.</summary>
    public void ResetGauge()
    {
        currentGauge = 0f;
    }

    /// <summary>현재 돌진의 패링 판정을 스페이스 입력에 연결합니다.</summary>
    public void RegisterDashPerfect(Func<double> getSecondsToContact, Action completePerfect)
    {
        getDashSecondsToContact = getSecondsToContact;
        completeDashPerfect = completePerfect;
    }

    /// <summary>같은 돌진이 등록한 패링 판정을 해제합니다.</summary>
    public void UnregisterDashPerfect(Func<double> getSecondsToContact)
    {
        if (getDashSecondsToContact == getSecondsToContact)
        {
            getDashSecondsToContact = null;
            completeDashPerfect = null;
        }
    }

    /// <summary>꼬리 꽂기의 퍼펙트 판정을 스페이스 입력에 연결합니다.</summary>
    public void RegisterTailStabPerfect(Func<double> getSecondsToContact, Action completePerfect)
    {
        getTailStabSecondsToContact = getSecondsToContact;
        completeTailStabPerfect = completePerfect;
    }

    /// <summary>같은 꼬리 꽂기가 등록한 퍼펙트 판정을 해제합니다.</summary>
    public void UnregisterTailStabPerfect(Func<double> getSecondsToContact)
    {
        if (getTailStabSecondsToContact == getSecondsToContact)
        {
            getTailStabSecondsToContact = null;
            completeTailStabPerfect = null;
        }
    }

    /// <summary>물림 중 스페이스 입력을 흔들기 판정에 연결합니다.</summary>
    public void RegisterBiteBraceInput(Func<double> getSecondsToContact, Action completeBrace)
    {
        getBiteBraceSecondsToContact = getSecondsToContact;
        completeBiteBrace = completeBrace;
    }

    /// <summary>같은 물림이 등록한 흔들기 입력을 해제합니다.</summary>
    public void UnregisterBiteBraceInput(Func<double> getSecondsToContact)
    {
        if (getBiteBraceSecondsToContact == getSecondsToContact)
        {
            getBiteBraceSecondsToContact = null;
            completeBiteBrace = null;
        }
    }

    private void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        playerMovement = GetComponent<PlayerMovement>();
        frenzy = GetComponent<PlayerFrenzy>();
        stats = GetComponent<PlayerStats>();
        currentGauge = 0f;
        nextInputTime = float.NegativeInfinity;
    }

    private void Update()
    {
        // 필드 시간이 정지된 동안 이동·공격·판정·획득을 처리하지 않는다.
        if (Time.timeScale <= 0f)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.spaceKey.wasPressedThisFrame)
        {
            return;
        }

        if (playerMovement != null && playerMovement.IsHeldByBite)
        {
            TryBiteBrace();
            return;
        }

        TryPerfectDodge();
    }

    private void TryBiteBrace()
    {
        double secondsToContact = getBiteBraceSecondsToContact?.Invoke() ?? double.NaN;
        if (Time.time < nextInputTime)
        {
            LogTimedPerfectInput("BiteBrace", secondsToContact, false, "RetryLocked");
            return;
        }

        if (IsWithinPerfectWindow(secondsToContact))
        {
            completeBiteBrace();
            LogTimedPerfectInput("BiteBrace", secondsToContact, true);
            return;
        }

        nextInputTime = Time.time + failureLockDuration;
        LogTimedPerfectInput("BiteBrace", secondsToContact, false,
            double.IsNaN(secondsToContact) ? "NoActiveStrike" : "OutOfWindow");
    }

    private void TryPerfectDodge()
    {
        if (!isActiveAndEnabled || playerHealth.IsDead || bulletRegistry == null)
            return;

        if (getSearchlightSecondsToContact != null)
        {
            double secondsToContact = getSearchlightSecondsToContact();
            if (Time.time < nextInputTime)
            {
                LogTimedPerfectInput("Searchlight", secondsToContact, false, "RetryLocked");
                return;
            }

            bool success = IsWithinPerfectWindow(secondsToContact);
            string failureReason = double.IsNaN(secondsToContact) ? "ContactEnded" : "OutOfWindow";
            LogTimedPerfectInput("Searchlight", secondsToContact, success,
                success ? null : failureReason);
            if (success)
            {
                completeSearchlightPerfect();
                playerHealth.GrantInvulnerability(stats.PerfectInvulnerability, isPerfect: true);
            }
            else
            {
                nextInputTime = Time.time + failureLockDuration;
            }

            return;
        }

        if (hasPendingPerfectInput)
        {
            if (Time.timeAsDouble > pendingPerfectEndsAt)
                processInputAfterPending = true;
            return;
        }

        double pressedAt = Time.timeAsDouble;
        double dashSeconds = getDashSecondsToContact?.Invoke() ?? double.NaN;
        double tailSeconds = getTailStabSecondsToContact?.Invoke() ?? double.NaN;
        if (Time.time < nextInputTime)
        {
            if (getDashSecondsToContact != null)
                LogTimedPerfectInput("Dash", dashSeconds, false, "RetryLocked");
            if (getTailStabSecondsToContact != null)
                LogTimedPerfectInput("TailStab", tailSeconds, false, "RetryLocked");
            if (getDashSecondsToContact == null && getTailStabSecondsToContact == null)
                LogTimedPerfectInput("Bullet", double.NaN, false, "RetryLocked");
            return;
        }

        int count = 0;
        if (getDashSecondsToContact != null)
        {
            bool dashSucceeded = IsWithinPerfectWindow(dashSeconds);
            string failureReason = double.IsNaN(dashSeconds) ? "NoCrossing" : "OutOfWindow";
            LogTimedPerfectInput("Dash", dashSeconds, dashSucceeded,
                dashSucceeded ? null : failureReason);
            if (dashSucceeded)
            {
                completeDashPerfect();
                count++;
            }
        }
        if (getTailStabSecondsToContact != null)
        {
            bool tailSucceeded = IsWithinPerfectWindow(tailSeconds);
            string failureReason = double.IsNaN(tailSeconds) ? "NoCrossing" : "OutOfWindow";
            LogTimedPerfectInput("TailStab", tailSeconds, tailSucceeded,
                tailSucceeded ? null : failureReason);
            if (tailSucceeded)
            {
                completeTailStabPerfect();
                count++;
            }
        }

        BeginBulletInput(pressedAt, count);
    }

    private void BeginBulletInput(double pressedAt, int immediateSuccessCount)
    {
        hasPendingPerfectInput = true;
        perfectPressedAt = pressedAt;
        perfectGraceAtInput = PerfectGraceSeconds;
        pendingPerfectEndsAt = pressedAt + perfectGraceAtInput;
        pendingSuccessCount = immediateSuccessCount;

        if (pendingSuccessCount > 0)
        {
            playerHealth.GrantInvulnerability(stats.PerfectInvulnerability, isPerfect: true);
        }

        foreach (TestBullet bullet in bulletRegistry.ActiveBullets)
        {
            if (bullet != null && bullet.TryGetLastCoreCrossing(playerHealth, out double crossedAt))
            {
                OnBulletCoreCrossed(bullet, crossedAt);
            }
        }
    }

    /// <summary>활성 입력의 시간 범위에 속하는 탄 교차를 집계합니다.</summary>
    public void OnBulletCoreCrossed(TestBullet bullet, double crossedAt)
    {
        double secondsToContact = crossedAt - perfectPressedAt;
        if (!hasPendingPerfectInput || playerHealth.IsDead
            || Math.Abs(secondsToContact) > perfectGraceAtInput || !bullet.TryMarkPerfect())
        {
            return;
        }

        if (pendingSuccessCount++ == 0)
        {
            playerHealth.GrantInvulnerability(stats.PerfectInvulnerability, isPerfect: true);
        }

        LogTimedPerfectInput("Bullet", secondsToContact, true);
    }

    private void LateUpdate()
    {
        if (!hasPendingPerfectInput)
            return;

        if (playerHealth.IsDead)
        {
            hasPendingPerfectInput = false;
            processInputAfterPending = false;
            return;
        }

        if (Time.timeScale <= 0f || Time.timeAsDouble < pendingPerfectEndsAt)
            return;

        hasPendingPerfectInput = false;
        ResolveBulletInput();
        if (processInputAfterPending)
        {
            processInputAfterPending = false;
            if (playerMovement != null && playerMovement.IsHeldByBite)
                TryBiteBrace();
            else
                TryPerfectDodge();
        }
    }

    private void ResolveBulletInput()
    {
        int count = pendingSuccessCount;
        if (count == 0)
        {
            nextInputTime = Time.time + failureLockDuration;
            LogTimedPerfectInput("Bullet", double.NaN, false, "NoCrossing");
            return;
        }

        // 굳기 중 퍼펙트는 폭주 보상을 지급하지 않는다.
        int bonusCount = playerMovement != null && playerMovement.IsFrozen
            ? 0 : Mathf.Min(count, simultaneousBonusLimit);
        PlayLog.Write("Perfect", $"Success ({count} targets, bonus={bonusCount})");
        if (bonusCount == 0)
            return;

        if (frenzy.IsActive)
        {
            frenzy.Extend(bonusCount);
        }
        else
        {
            currentGauge = Mathf.Min(maxGauge, currentGauge + bonusCount * stats.GaugePerBullet);
            if (currentGauge >= maxGauge)
            {
                currentGauge = 0f;
                frenzy.Begin();
            }
        }
    }

    private void OnDisable()
    {
        hasPendingPerfectInput = false;
        processInputAfterPending = false;
    }

    public bool IsWithinPerfectWindow(double secondsToContact)
    {
        return Math.Abs(secondsToContact) <= perfectGraceSeconds;
    }

    private static void LogTimedPerfectInput(
        string kind, double secondsToContact, bool success, string failureReason = null)
    {
        string time = double.IsNaN(secondsToContact) ? "none"
            : secondsToContact.ToString("+0.000;-0.000;0.000",
                System.Globalization.CultureInfo.InvariantCulture);
        string reason = failureReason == null ? "" : $", reason={failureReason}";
        PlayLog.Write("Perfect",
            $"Input (kind={kind}, secondsToContact={time}, "
            + $"result={(success ? "Success" : "Fail")}{reason})");
    }

    private void OnValidate()
    {
        failureLockDuration = Mathf.Max(0f, failureLockDuration);
        simultaneousBonusLimit = Mathf.Max(1, simultaneousBonusLimit);
        perfectGraceSeconds = Mathf.Max(0f, perfectGraceSeconds);
        maxGauge = Mathf.Max(1f, maxGauge);
        currentGauge = Mathf.Clamp(currentGauge, 0f, maxGauge);
    }
}
