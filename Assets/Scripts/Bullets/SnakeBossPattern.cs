using System.Collections.Generic;
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
    [SerializeField] private SnakeBossTailStabPattern tailStabPattern;

    // ##TEMP 최종 보스 단독 플레이용 시작값.
    [SerializeField, Min(0f)] private float laneAimDelay = 3.5f;
    // ##TEMP 세트 2의 S자 발사 횟수와 줄 사이 대기 시간.
    [SerializeField, Min(1)] private int waveSpreadCount = 3;
    [SerializeField, Range(0.01f, 10f)] private float waveSpreadMinInterval = 0.8f;
    [SerializeField, Range(0.01f, 10f)] private float waveSpreadMaxInterval = 1.5f;

    // ##TEMP 전투 시작 시점 필드 크기 대비 가로·세로 비율.
    [SerializeField, Range(0.01f, 1f)] private float firstFieldSizeRatio = 0.85f;
    [SerializeField, Range(0.01f, 1f)] private float secondFieldSizeRatio = 0.70f;

    private enum AttackSet
    {
        LaneAndDash,
        WaveSpread,
        EruptionAndFan,
        Searchlight,
        TailStab
    }

    private enum TailStabTransition
    {
        None,
        WaitingForShrink,
        Stabbing
    }

    private readonly List<AttackSet> setOrder = new List<AttackSet>
    {
        AttackSet.LaneAndDash,
        AttackSet.WaveSpread,
        AttackSet.EruptionAndFan,
        AttackSet.Searchlight
    };

    private PlayerHealth player;
    private BossHealth bossHealth;
    private FieldManager fieldManager;
    private PlayerMovement playerMovement;
    private Vector3 homePosition;
    private float homeTopInset;
    private int fieldShrinkStage;
    private TailStabTransition tailStabTransition;
    private BulletRegistry bulletRegistry;
    private AttackSet currentSet;
    private int nextSetIndex;
    private bool running;
    private bool restoreFieldOnDisable;
    private bool waitingForSearchlight;
    private bool aimStarted;
    private float aimDelayRemaining;
    private int remainingWaveSpreads;
    private double? nextWaveSpreadAt;

    /// <summary>등장과 세트 1~4에 필요한 컴포넌트 연결 상태입니다.</summary>
    public bool IsConfigured =>
        entrance != null && aimPattern != null && dashPattern != null
        && fanPattern != null && lanePattern != null && eruptionPattern != null
        && wavePattern != null && searchlightPattern != null && tailStabPattern != null;

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
        bossHealth = GetComponent<BossHealth>();
        bulletRegistry = registry;
        fieldManager = FieldManager.Instance;
        searchlightPattern.Initialize(player, field, registry, bulletPoolParent, fieldManager);
        dashPattern.Initialize(field);
        fanPattern.Initialize(player, field, registry, bulletPoolParent, fieldManager);
        wavePattern.Initialize(player, field, registry, bulletPoolParent);
        lanePattern.Initialize(player, field, lanePoolParent, fieldManager);
        eruptionPattern.Initialize(player, field, registry, tailPoolParent);
        tailStabPattern.Initialize(tailPoolParent);
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

        if (fieldManager != null)
        {
            playerMovement = player.GetComponent<PlayerMovement>();
            homePosition = transform.position;
            homeTopInset = fieldManager.CurrentBounds.max.y - homePosition.y;
            fieldManager.BeginShrinkSequence(playerMovement, UpdateHomePosition);
        }
        else
        {
            PlayLog.Write("SnakeBoss", "FieldManager 연결을 확인해주세요.",
                LogType.Error, this);
        }

        fieldShrinkStage = 0;
        tailStabTransition = TailStabTransition.None;
        setOrder.Remove(AttackSet.TailStab);
        running = true;
        ShuffleSets(avoidRepeat: false);
        BeginNextSet();
    }

    private void Update()
    {
        if (!running)
        {
            return;
        }

        if (player == null || player.IsDead)
        {
            StopPattern();
            return;
        }

        if (Time.deltaTime <= 0f)
        {
            return;
        }

        CheckFieldShrinkThresholds();

        if (tailStabTransition != TailStabTransition.None)
        {
            AdvanceFirstTailStab();
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

        if (currentSet == AttackSet.TailStab)
        {
            tailStabPattern.Advance(Time.deltaTime);
            if (player.IsDead)
            {
                return;
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
            AttackSet.TailStab => !tailStabPattern.IsRunning,
            _ => false
        };

        if (!isComplete)
        {
            return;
        }

        PlayLog.Write("SnakeBoss", $"Set End ({currentSet})");
        if (nextSetIndex == setOrder.Count)
        {
            ShuffleSets(avoidRepeat: true);
        }

        BeginNextSet();
    }

    private void UpdateHomePosition(Bounds bounds)
    {
        homePosition.y = bounds.max.y - homeTopInset;
        bool isHoldingPlayer = playerMovement != null && playerMovement.IsHeldByBite;
        entrance.SetHomePosition(
            homePosition, moveImmediately: !dashPattern.IsMovingAway && !isHoldingPlayer);
    }

    private void CheckFieldShrinkThresholds()
    {
        if (fieldManager == null)
        {
            return;
        }

        while (fieldShrinkStage < 2)
        {
            bool isFirstStage = fieldShrinkStage == 0;
            float threshold = isFirstStage ? 0.5f : 0.25f;
            if (bossHealth.CurrentHealth > bossHealth.MaxHealth * threshold)
            {
                return;
            }

            float sizeRatio = isFirstStage
                ? firstFieldSizeRatio
                : Mathf.Min(secondFieldSizeRatio, firstFieldSizeRatio);

            fieldShrinkStage++;
            Vector2 targetSize = fieldManager.RequestShrink(fieldShrinkStage, sizeRatio);
            PlayLog.Write("SnakeBoss",
                $"Field Shrink Threshold (stage={fieldShrinkStage}, "
                + $"health={bossHealth.CurrentHealth}/{bossHealth.MaxHealth}, "
                + $"targetWidth={targetSize.x:F2}, targetHeight={targetSize.y:F2})");

            if (fieldShrinkStage == 2)
            {
                tailStabTransition = TailStabTransition.WaitingForShrink;
                StopCurrentAttacks();
                PlayLog.Write("SnakeBoss", "First Tail Stab Waiting For Shrink");
            }
        }
    }

    private void AdvanceFirstTailStab()
    {
        if (tailStabTransition == TailStabTransition.WaitingForShrink
            && fieldManager.AppliedShrinkStage >= 2)
        {
            tailStabPattern.BeginStab(player, fieldManager.CurrentBounds, isFirstStab: true);
            tailStabTransition = TailStabTransition.Stabbing;
            return;
        }

        if (tailStabTransition == TailStabTransition.Stabbing)
        {
            tailStabPattern.Advance(Time.deltaTime);
            if (!tailStabPattern.IsRunning && !player.IsDead)
            {
                tailStabTransition = TailStabTransition.None;
                setOrder.Add(AttackSet.TailStab);
                currentSet = AttackSet.TailStab;
                ShuffleSets(avoidRepeat: true);
                BeginNextSet();
            }
        }
    }

    private void ShuffleSets(bool avoidRepeat)
    {
        for (int index = setOrder.Count - 1; index > 0; index--)
        {
            int swapIndex = Random.Range(0, index + 1);
            (setOrder[index], setOrder[swapIndex]) = (setOrder[swapIndex], setOrder[index]);
        }

        if (avoidRepeat && setOrder[0] == currentSet)
        {
            int swapIndex = Random.Range(1, setOrder.Count);
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

            case AttackSet.TailStab:
                tailStabPattern.BeginStab(
                    player, fieldManager.CurrentBounds, isFirstStab: false);
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
        bool bossDefeated = bossHealth != null && bossHealth.IsDefeated;
        fieldManager?.EndShrinkSequence();
        tailStabPattern.StopStab();
        StopCurrentAttacks();

        if (bossDefeated)
        {
            // 전투 Flow가 남은 탄을 흡수하고 보스를 퇴장시킨다.
            restoreFieldOnDisable = true;
            return;
        }

        foreach (TestBullet bullet in new List<TestBullet>(bulletRegistry.ActiveBullets))
        {
            if (bullet != null)
            {
                bullet.Expire();
            }
        }

        fieldManager?.RestoreOriginalSize();
    }

    private void OnDisable()
    {
        StopPattern();

        if (restoreFieldOnDisable)
        {
            restoreFieldOnDisable = false;
            fieldManager?.RestoreOriginalSize();
        }
    }

    private void StopCurrentAttacks()
    {
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
