using UnityEngine;

/// <summary>머리 보스의 등장과 패턴 실행을 관리합니다.</summary>
public sealed class HeadBossPattern : MonoBehaviour, IBossPattern
{
    [SerializeField] private HeadBossEntrance entrance;
    [SerializeField] private HeadBossFanPattern fanPattern;
    [SerializeField] private HeadBossFreezePattern freezePattern;
    [SerializeField] private HeadBossSearchlightPattern searchlightPattern;
    [SerializeField] private HeadBossDashPattern dashPattern;
    [SerializeField] private HeadBossAimPattern aimPattern;

    // ##TEMP 마지막 부채꼴 발사 후 돌진 조준까지의 대기 시간.
    [SerializeField, Min(0f)] private float fanEndDelay = 1f;

    private enum PatternState
    {
        Idle,
        AimDashAndReturn,
        Searchlight,
        Fan
    }

    private PlayerHealth player;
    private PatternState state;

    /// <summary>머리 보스의 필수 컴포넌트 연결 상태입니다.</summary>
    public bool IsConfigured =>
        entrance != null && fanPattern != null
        && freezePattern != null && searchlightPattern != null
        && dashPattern != null && aimPattern != null;

    /// <summary>머리 보스의 등장 완료 상태입니다.</summary>
    public bool IsEntranceComplete =>
        entrance != null && entrance.IsComplete;

    /// <summary>별도 회수를 기다릴 위험 영역이 없습니다.</summary>
    public bool IsRetreatComplete => true;

    /// <summary>Flow가 전달한 씬 참조로 패턴을 초기화합니다.</summary>
    public void Initialize(
        PlayerHealth player,
        SpriteRenderer field,
        BulletRegistry registry,
        Transform bulletPoolParent,
        Transform tailPoolParent,
        Transform lanePoolParent)
    {
        if (!IsConfigured)
        {
            return;
        }

        this.player = player;
        fanPattern.Initialize(player, field, registry, bulletPoolParent);
        freezePattern.Initialize(player, field, registry, bulletPoolParent);
        dashPattern.Initialize(field);
    }

    /// <summary>Flow의 요청으로 머리 등장을 시작합니다.</summary>
    public void BeginEntrance()
    {
        if (IsConfigured)
        {
            entrance.BeginEntrance();
        }
    }

    /// <summary>Flow의 요청으로 조준 돌진부터 패턴 반복을 시작합니다.</summary>
    public void StartPattern()
    {
        if (!IsConfigured || state != PatternState.Idle
            || player == null || player.IsDead)
        {
            return;
        }

        BeginAim();
    }

    private void BeginAim()
    {
        entrance.CancelReentry();
        state = PatternState.AimDashAndReturn;
        PlayLog.Write("HeadBoss", "Aim Dash Pattern Start");
        aimPattern.BeginTracking(player.transform);
    }

    private void Update()
    {
        if (Time.deltaTime <= 0f || player == null || player.IsDead)
        {
            return;
        }

        switch (state)
        {
            case PatternState.AimDashAndReturn when entrance.IsReentryComplete:
                PlayLog.Write("HeadBoss", "Aim Dash Pattern End (returned)");
                state = PatternState.Searchlight;
                PlayLog.Write("HeadBoss", "Searchlight Pattern Start");
                searchlightPattern.BeginTracking(player.transform);
                break;

            case PatternState.Searchlight when !searchlightPattern.IsRunning:
                PlayLog.Write("HeadBoss", "Searchlight Pattern End");
                state = PatternState.Fan;
                PlayLog.Write("HeadBoss", "Fan Pattern Start");
                fanPattern.StartEmission();
                break;

            case PatternState.Fan when fanPattern.HasFinishedFiringAfter(fanEndDelay):
                PlayLog.Write("HeadBoss", $"Fan Pattern End (delay={fanEndDelay:F3}s)");
                fanPattern.StopForBattleEnd();
                BeginAim();
                break;
        }
    }

    /// <summary>Flow의 요청으로 머리 보스의 패턴 반복을 중단합니다.</summary>
    public void StopPattern()
    {
        if (state != PatternState.Idle)
        {
            PlayLog.Write("HeadBoss", $"Pattern Stop (phase={state})");
        }

        state = PatternState.Idle;

        if (fanPattern != null)
        {
            fanPattern.StopForBattleEnd();
        }

        if (searchlightPattern != null)
        {
            searchlightPattern.StopTracking();
        }

        if (aimPattern != null)
        {
            aimPattern.StopTracking();
        }

        if (dashPattern != null)
        {
            dashPattern.StopDash();
        }
    }
}
