using UnityEngine;

/// <summary>보스 초기화·등장·패턴 시작·중단을 정의합니다.</summary>
public interface IBossPattern
{
    /// <summary>필수 컴포넌트가 연결됐는지 반환합니다.</summary>
    bool IsConfigured { get; }

    /// <summary>등장이 완료됐는지 반환합니다.</summary>
    bool IsEntranceComplete { get; }

    /// <summary>위험 영역과 꼬리 등의 회수가 완료됐는지 반환합니다.</summary>
    bool IsRetreatComplete { get; }

    /// <summary>Flow가 보스 실행에 필요한 씬 참조를 전달합니다.</summary>
    void Initialize(
        PlayerHealth player,
        SpriteRenderer field,
        BulletRegistry registry,
        Transform bulletPoolParent,
        Transform tailPoolParent,
        Transform lanePoolParent);

    /// <summary>보스 등장을 시작합니다.</summary>
    void BeginEntrance();

    /// <summary>보스의 공격 패턴을 시작합니다.</summary>
    void StartPattern();

    /// <summary>새 공격을 중단하고 회수를 시작합니다.</summary>
    void StopPattern();
}
