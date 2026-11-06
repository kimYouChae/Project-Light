using UnityEngine;

/// <summary>꼬리 보스의 세 공격 패턴을 묶어 관리합니다.</summary>
public sealed class TailBossPattern : MonoBehaviour, IBossPattern
{
    [SerializeField] private BossBulletPattern bulletPattern;
    [SerializeField] private TailAttackPattern tailPattern;
    [SerializeField] private TailLanePattern lanePattern;

    /// <summary>세 패턴의 필수 참조가 연결됐는지 반환합니다.</summary>
    public bool IsConfigured =>
        bulletPattern != null
        && tailPattern != null
        && lanePattern != null;

    /// <summary>꼬리 보스는 현재 별도 등장 이동 없이 시작합니다.</summary>
    public bool IsEntranceComplete => true;

    /// <summary>현재 꼬리 보스에는 별도 등장 이동이 없습니다.</summary>
    public void BeginEntrance()
    {
    }

    /// <summary>발밑 꼬리와 레인 꼬리의 회수가 끝났는지 반환합니다.</summary>
    public bool IsRetreatComplete =>
        IsConfigured
        && tailPattern.IsRetreatComplete
        && lanePattern.IsRetreatComplete;

    /// <summary>
    /// 세 패턴에 실행에 필요한 씬 참조를 전달합니다.
    /// MiniBossBattleFlow.Start가 인터페이스를 통해 호출합니다.
    /// </summary>
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

        bulletPattern.Initialize(
            player, field, registry, bulletPoolParent);

        tailPattern.Initialize(
            player, field, registry, tailPoolParent);

        lanePattern.Initialize(
            player, field, lanePoolParent);
    }

    /// <summary>세 공격 패턴을 함께 시작합니다.</summary>
    public void StartPattern()
    {
        if (!IsConfigured)
        {
            return;
        }

        bulletPattern.StartEmission();
        tailPattern.StartEmission();
        lanePattern.StartEmission();
    }

    /// <summary>새 공격을 중단하고 경고·꼬리 회수를 시작합니다.</summary>
    public void StopPattern()
    {
        if (bulletPattern != null)
        {
            bulletPattern.StopForBattleEnd();
        }

        if (tailPattern != null)
        {
            tailPattern.StopForBattleEnd();
        }

        if (lanePattern != null)
        {
            lanePattern.StopForBattleEnd();
        }
    }
}
