using UnityEngine;

/// <summary>머리 보스의 등장과 현재 구현된 패턴 순서를 관리합니다.</summary>
public sealed class HeadBossPattern : MonoBehaviour, IBossPattern
{
    private enum PatternState
    {
        Idle,
        Fan,
        Waiting,
        WallApproach,
        Warning,
        Detecting,
        Finished
    }

    [SerializeField] private HeadBossEntrance entrance;
    [SerializeField] private HeadBossFanPattern fanPattern;
    [SerializeField] private HeadBossWallPattern wallPattern;
    [SerializeField] private HeadBossFreezeWarning freezeWarning;
    [SerializeField] private HeadBossFreezePattern freezePattern;

    // ##TEMP 승인한 패턴 사이 대기 시간.
    [SerializeField, Min(0f)] private float patternWaitDuration = 1f;

    private PatternState state;
    private float waitElapsed;

    /// <summary>머리 보스의 필수 컴포넌트 연결 상태입니다.</summary>
    public bool IsConfigured =>
        entrance != null
        && fanPattern != null
        && wallPattern != null
        && freezeWarning != null
        && freezePattern != null;

    /// <summary>머리 보스의 등장 완료 상태입니다.</summary>
    public bool IsEntranceComplete =>
        entrance != null && entrance.IsComplete;

    /// <summary>
    /// 현재 종료 시 벽은 즉시 반환합니다.
    /// 발사한 탄의 흡수는 Flow가 처리합니다.
    /// </summary>
    public bool IsRetreatComplete => true;

    /// <summary>Flow가 전달한 씬 참조로 각 패턴을 초기화합니다.</summary>
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

        fanPattern.Initialize(
            player,
            field,
            registry,
            bulletPoolParent);

        wallPattern.Initialize(player, field, bulletPoolParent);

        freezePattern.Initialize(
            player,
            field,
            registry,
            bulletPoolParent);
    }

    /// <summary>Flow의 요청으로 머리 등장을 시작합니다.</summary>
    public void BeginEntrance()
    {
        if (!IsConfigured)
        {
            return;
        }

        entrance.BeginEntrance();
    }

    /// <summary>Flow의 요청으로 부채꼴부터 패턴 실행을 시작합니다.</summary>
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
        if (Time.timeScale <= 0f)
        {
            return;
        }

        switch (state)
        {
            case PatternState.Fan:
                if (!fanPattern.IsEmitting)
                {
                    waitElapsed = 0f;
                    state = PatternState.Waiting;
                }
                break;

            case PatternState.Waiting:
                waitElapsed += Time.deltaTime;

                if (waitElapsed >= patternWaitDuration)
                {
                    wallPattern.BeginWalls();
                    state = PatternState.WallApproach;
                }
                break;

            case PatternState.WallApproach:
                if (wallPattern.IsApproachComplete)
                {
                    freezeWarning.BeginWarning();
                    state = PatternState.Warning;
                }
                break;

            case PatternState.Warning:
                if (freezeWarning.IsComplete)
                {
                    freezePattern.BeginDetection();
                    state = PatternState.Detecting;
                }
                break;

            case PatternState.Detecting:
                if (freezePattern.IsComplete)
                {
                    // 이번 연결은 감지 구간까지 한 번 실행한 뒤 대기한다.
                    state = PatternState.Finished;
                }
                break;
        }
    }

    /// <summary>Flow의 요청으로 패턴 진행과 새 발사를 중단합니다.</summary>
    public void StopPattern()
    {
        // 다음 단계가 시작되지 않도록 진행 상태를 먼저 정리한다.
        state = PatternState.Idle;
        waitElapsed = 0f;

        if (fanPattern != null)
        {
            fanPattern.StopForBattleEnd();
        }

        if (wallPattern != null)
        {
            wallPattern.ClearWalls();
        }

        if (freezeWarning != null)
        {
            freezeWarning.StopWarning();
        }

        if (freezePattern != null)
        {
            freezePattern.StopDetection();
        }
    }

    private void OnValidate()
    {
        patternWaitDuration = Mathf.Max(0f, patternWaitDuration);
    }
}
