using UnityEngine;

/// <summary>머리 보스의 등장과 패턴 실행을 관리합니다.</summary>
public sealed class HeadBossPattern : MonoBehaviour, IBossPattern
{
    [SerializeField] private HeadBossEntrance entrance;
    [SerializeField] private HeadBossFanPattern fanPattern;
    [SerializeField] private HeadBossFreezePattern freezePattern;
    [SerializeField] private HeadBossSearchlightPattern searchlightPattern;
    [SerializeField] private HeadBossDashPattern dashPattern;

    private enum PatternState
    {
        Idle,
        Fan,
        Searchlight,
        Finished
    }

    private PlayerHealth player;
    private PatternState state;

    /// <summary>머리 보스의 필수 컴포넌트 연결 상태입니다.</summary>
    public bool IsConfigured =>
        entrance != null && fanPattern != null
        && freezePattern != null && searchlightPattern != null;

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
        if (dashPattern != null)
        {
            dashPattern.Initialize(field);
        }
    }

    /// <summary>Flow의 요청으로 머리 등장을 시작합니다.</summary>
    public void BeginEntrance()
    {
        if (IsConfigured)
        {
            entrance.BeginEntrance();
        }
    }

    /// <summary>Flow의 요청으로 부채꼴부터 패턴을 한 번 시작합니다.</summary>
    public void StartPattern()
    {
        if (!IsConfigured || state != PatternState.Idle)
        {
            return;
        }

        state = PatternState.Fan;
        fanPattern.StartEmission();
    }

    private void Update()
    {
        if (Time.deltaTime <= 0f)
        {
            return;
        }

        if (state == PatternState.Fan && fanPattern.IsFinished
            && player != null && !player.IsDead)
        {
            state = PatternState.Searchlight;
            searchlightPattern.BeginTracking(player.transform);
        }
        else if (state == PatternState.Searchlight && !searchlightPattern.IsRunning)
        {
            state = PatternState.Finished;
        }
    }

    /// <summary>Flow의 요청으로 부채꼴과 서치라이트를 중단합니다.</summary>
    public void StopPattern()
    {
        state = PatternState.Idle;

        if (fanPattern != null)
        {
            fanPattern.StopForBattleEnd();
        }

        if (searchlightPattern != null)
        {
            searchlightPattern.StopTracking();
        }

        if (dashPattern != null)
        {
            dashPattern.StopDash();
        }
    }
}
