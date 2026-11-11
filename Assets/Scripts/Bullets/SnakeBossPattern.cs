using UnityEngine;

/// <summary>1스테이지 뱀 보스의 등장과 공격 세트 순환을 관리합니다.</summary>
public sealed class SnakeBossPattern : MonoBehaviour, IBossPattern
{
    [SerializeField] private HeadBossEntrance entrance;
    [SerializeField] private HeadBossAimPattern aimPattern;
    [SerializeField] private HeadBossDashPattern dashPattern;
    [SerializeField] private HeadBossFanPattern fanPattern;
    [SerializeField] private TailLanePattern lanePattern;
    [SerializeField] private TailAttackPattern eruptionPattern;
    [SerializeField] private BossBulletPattern wavePattern;
    [SerializeField] private SnakeBossSearchlightPattern searchlightPattern;

    // ##TEMP 최종 보스 단독 플레이용 시작값.
    [SerializeField, Min(0f)] private float laneAimDelay = 3.5f;
    // ##TEMP 세트 2의 S자 발사 횟수와 줄 사이 대기 시간.
    [SerializeField, Min(1)] private int waveSpreadCount = 3;
    [SerializeField, Range(0.01f, 10f)] private float waveSpreadMinInterval = 0.8f;
    [SerializeField, Range(0.01f, 10f)] private float waveSpreadMaxInterval = 1.5f;

    private enum AttackSet
    {
        LaneAndDash,
        WaveSpread,
        EruptionAndFan,
        Searchlight
    }

    private readonly AttackSet[] setOrder =
    {
        AttackSet.LaneAndDash,
        AttackSet.WaveSpread,
        AttackSet.EruptionAndFan,
        AttackSet.Searchlight
    };

    private PlayerHealth player;
    private BulletRegistry bulletRegistry;
    private AttackSet currentSet;
    private int nextSetIndex;
    private bool running;
    private bool waitingForSearchlight;
    private bool aimStarted;
    private float aimDelayRemaining;
    private int remainingWaveSpreads;
    private double? nextWaveSpreadAt;

    /// <summary>등장과 세트 1~4에 필요한 컴포넌트 연결 상태입니다.</summary>
    public bool IsConfigured =>
        entrance != null && aimPattern != null && dashPattern != null
        && fanPattern != null && lanePattern != null && eruptionPattern != null
        && wavePattern != null && searchlightPattern != null;

    /// <summary>최초 등장 완료 상태입니다.</summary>
    public bool IsEntranceComplete => entrance != null && entrance.IsComplete;

    /// <summary>처치 뒤 남아 있던 꼬리 공격의 회수 완료 상태입니다.</summary>
    public bool IsRetreatComplete =>
        lanePattern.IsRetreatComplete && eruptionPattern.IsRetreatComplete;

    /// <summary>전투 Flow가 전달한 플레이어·필드·풀 참조를 패턴에 연결합니다.</summary>
    public void Initialize(
        PlayerHealth player,
        SpriteRenderer field,
        BulletRegistry registry,
        Transform bulletPoolParent,
        Transform tailPoolParent,
        Transform lanePoolParent)
    {
        this.player = player;
        bulletRegistry = registry;
        searchlightPattern.Initialize(player, field, registry, bulletPoolParent);
        dashPattern.Initialize(field);
        fanPattern.Initialize(player, field, registry, bulletPoolParent);
        wavePattern.Initialize(player, field, registry, bulletPoolParent);
        lanePattern.Initialize(player, field, lanePoolParent);
        eruptionPattern.Initialize(player, field, registry, tailPoolParent);
    }

    /// <summary>전투 Flow의 요청으로 뱀 보스를 등장시킵니다.</summary>
    public void BeginEntrance()
    {
        entrance.BeginEntrance();
    }

    /// <summary>전투 Flow의 요청으로 첫 바퀴를 섞어 시작합니다.</summary>
    public void StartPattern()
    {
        if (running || player == null || player.IsDead)
        {
            return;
        }

        running = true;
        ShuffleSets(avoidRepeat: false);
        BeginNextSet();
    }

    private void Update()
    {
        if (!running || Time.deltaTime <= 0f || player == null || player.IsDead)
        {
            return;
        }

        if (waitingForSearchlight)
        {
            if (bulletRegistry.ActiveBullets.Count == 0)
            {
                BeginNextSet();
            }

            return;
        }

        if (currentSet == AttackSet.LaneAndDash && !aimStarted)
        {
            aimDelayRemaining -= Time.deltaTime;
            if (aimDelayRemaining <= 0f)
            {
                BeginAim();
            }
        }

        if (currentSet == AttackSet.WaveSpread
            && remainingWaveSpreads > 0
            && wavePattern.IsSingleWaveComplete)
        {
            if (!nextWaveSpreadAt.HasValue)
            {
                float maxInterval = Mathf.Max(waveSpreadMinInterval, waveSpreadMaxInterval);
                float interval = Random.Range(waveSpreadMinInterval, maxInterval);
                nextWaveSpreadAt = wavePattern.SingleWaveFinishedAt + interval;
            }

            if (Time.timeAsDouble >= nextWaveSpreadAt.Value)
            {
                BeginSingleWave();
            }
        }

        bool isComplete = currentSet switch
        {
            AttackSet.LaneAndDash =>
                aimStarted && entrance.IsReentryComplete && lanePattern.IsSingleAttackComplete,
            AttackSet.WaveSpread =>
                remainingWaveSpreads == 0 && wavePattern.IsSingleWaveComplete,
            AttackSet.EruptionAndFan =>
                eruptionPattern.IsSingleAttackComplete && fanPattern.HasFinishedFiringAfter(0f),
            AttackSet.Searchlight => !searchlightPattern.IsRunning,
            _ => false
        };

        if (!isComplete)
        {
            return;
        }

        PlayLog.Write("SnakeBoss", $"Set End ({currentSet})");
        if (nextSetIndex == setOrder.Length)
        {
            ShuffleSets(avoidRepeat: true);
        }

        BeginNextSet();
    }

    private void ShuffleSets(bool avoidRepeat)
    {
        for (int index = setOrder.Length - 1; index > 0; index--)
        {
            int swapIndex = Random.Range(0, index + 1);
            (setOrder[index], setOrder[swapIndex]) = (setOrder[swapIndex], setOrder[index]);
        }

        if (avoidRepeat && setOrder[0] == currentSet)
        {
            int swapIndex = Random.Range(1, setOrder.Length);
            (setOrder[0], setOrder[swapIndex]) = (setOrder[swapIndex], setOrder[0]);
        }

        nextSetIndex = 0;
    }

    private void BeginNextSet()
    {
        if (setOrder[nextSetIndex] == AttackSet.Searchlight
            && bulletRegistry.ActiveBullets.Count > 0)
        {
            waitingForSearchlight = true;
            return;
        }

        waitingForSearchlight = false;
        currentSet = setOrder[nextSetIndex++];
        PlayLog.Write("SnakeBoss", $"Set Start ({currentSet})");

        switch (currentSet)
        {
            case AttackSet.LaneAndDash:
                lanePattern.StartSingleAttack();
                aimStarted = false;
                aimDelayRemaining = laneAimDelay;
                if (aimDelayRemaining <= 0f)
                {
                    BeginAim();
                }
                break;

            case AttackSet.WaveSpread:
                remainingWaveSpreads = Mathf.Max(1, waveSpreadCount);
                BeginSingleWave();
                break;

            case AttackSet.EruptionAndFan:
                eruptionPattern.StartSingleAttack();
                fanPattern.StartEmission();
                break;

            case AttackSet.Searchlight:
                searchlightPattern.BeginTracking();
                break;
        }
    }

    private void BeginSingleWave()
    {
        wavePattern.StartSingleWave();
        remainingWaveSpreads--;
        nextWaveSpreadAt = null;
    }

    private void BeginAim()
    {
        entrance.CancelReentry();
        aimStarted = true;
        aimPattern.BeginTracking(player.transform);
    }

    /// <summary>전투 Flow의 요청으로 예약 공격과 진행 중인 위험 영역을 정리합니다.</summary>
    public void StopPattern()
    {
        if (!running)
        {
            return;
        }

        running = false;
        waitingForSearchlight = false;
        searchlightPattern.StopTracking();
        remainingWaveSpreads = 0;
        nextWaveSpreadAt = null;
        aimPattern.StopTracking();
        dashPattern.StopDash();
        lanePattern.StopForBattleEnd();
        eruptionPattern.StopForBattleEnd();
        wavePattern.StopForBattleEnd();
        fanPattern.StopForBattleEnd();
    }
}
