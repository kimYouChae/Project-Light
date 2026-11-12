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
    // ##TEMP PERFECT 판정선 안팎의 추가 허용 폭.
    [SerializeField, Min(0f)] private float perfectTolerance = 0.5f;

    [Header("게이지")]
    // ##TEMP 승인한 프로토타입 시작값.
    [SerializeField, Min(1f)] private float maxGauge = 100f;
    [SerializeField] private float currentGauge;

    private PlayerHealth playerHealth;
    private PlayerLight playerLight;
    private PlayerMovement playerMovement;
    private PlayerFrenzy frenzy;
    private float nextInputTime;
    private Func<bool> tryDashPerfect;
    private Func<bool> tryTailStabPerfect;
    private Action biteBraceInput;

    /// <summary>현재 충전된 퍼펙트 게이지.</summary>
    public float CurrentGauge => frenzy != null && frenzy.IsActive
        ? maxGauge * frenzy.RemainingRatio : currentGauge;

    /// <summary>게이지 최대 충전량.</summary>
    public float MaxGauge => maxGauge;

    /// <summary>PERFECT 판정선 안팎의 추가 허용 폭.</summary>
    public float PerfectTolerance => perfectTolerance;

    /// <summary>현재 돌진의 패링 판정을 스페이스 입력에 연결합니다.</summary>
    public void RegisterDashPerfect(Func<bool> tryPerfect)
    {
        tryDashPerfect = tryPerfect;
    }

    /// <summary>같은 돌진이 등록한 패링 판정을 해제합니다.</summary>
    public void UnregisterDashPerfect(Func<bool> tryPerfect)
    {
        if (tryDashPerfect == tryPerfect)
        {
            tryDashPerfect = null;
        }
    }

    /// <summary>꼬리 꽂기의 퍼펙트 판정을 스페이스 입력에 연결합니다.</summary>
    public void RegisterTailStabPerfect(Func<bool> tryPerfect)
    {
        tryTailStabPerfect = tryPerfect;
    }

    /// <summary>같은 꼬리 꽂기가 등록한 퍼펙트 판정을 해제합니다.</summary>
    public void UnregisterTailStabPerfect(Func<bool> tryPerfect)
    {
        if (tryTailStabPerfect == tryPerfect)
        {
            tryTailStabPerfect = null;
        }
    }

    /// <summary>물림 중 스페이스 입력을 흔들기 판정에 연결합니다.</summary>
    public void RegisterBiteBraceInput(Action input)
    {
        biteBraceInput = input;
    }

    /// <summary>같은 물림이 등록한 흔들기 입력을 해제합니다.</summary>
    public void UnregisterBiteBraceInput(Action input)
    {
        if (biteBraceInput == input)
        {
            biteBraceInput = null;
        }
    }

    private void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        playerLight = GetComponent<PlayerLight>();
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
            biteBraceInput?.Invoke();
            return;
        }

        TryPerfectDodge();
    }

    private void TryPerfectDodge()
    {
        if (!isActiveAndEnabled || playerHealth.IsDead || bulletRegistry == null)
            return;

        if (Time.time < nextInputTime)
        {
            PlayLog.Write("Perfect", "Input Blocked (retry lock)");
            return;
        }

        int count = tryDashPerfect?.Invoke() == true ? 1 : 0;
        if (tryTailStabPerfect?.Invoke() == true)
        {
            count++;
        }

        // 입력 프레임에만 전체 목록을 순회한다. 충전 상한을 넘어도 모두 처리 표시한다.
        foreach (TestBullet bullet in bulletRegistry.ActiveBullets)
        {
            if (bullet != null && !bullet.PerfectProcessed
                && playerLight.IsOnCoreBoundary(
                    bullet.transform.position, bullet.Radius, perfectTolerance)
                && bullet.TryMarkPerfect())
            {
                count++;
            }
        }

        if (count == 0)
        {
            nextInputTime = Time.time + failureLockDuration;
            PlayLog.Write("Perfect", "Miss (no target in range)");
            return;
        }

        playerHealth.GrantInvulnerability(stats.PerfectInvulnerability);
        PlayLog.Write("Perfect", $"Success ({count} targets)");

        // 굳기 중 퍼펙트는 폭주 보상을 지급하지 않는다.
        if (playerMovement != null && playerMovement.IsFrozen)
        {
            return;
        }

        int bonusCount = Mathf.Min(count, simultaneousBonusLimit);
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

    private void OnValidate()
    {
        failureLockDuration = Mathf.Max(0f, failureLockDuration);
        simultaneousBonusLimit = Mathf.Max(1, simultaneousBonusLimit);
        perfectTolerance = Mathf.Max(0f, perfectTolerance);
        maxGauge = Mathf.Max(1f, maxGauge);
        currentGauge = Mathf.Clamp(currentGauge, 0f, maxGauge);
    }
}
