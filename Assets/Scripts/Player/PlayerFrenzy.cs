using System;
using UnityEngine;

/// <summary>폭주 지속 시간과 PERFECT에 의한 시간 연장을 관리한다.</summary>
[DefaultExecutionOrder(-200)]
[DisallowMultipleComponent]
public sealed class PlayerFrenzy : MonoBehaviour
{
    // ##TEMP 기획서의 프로토타입 조정값.
    [SerializeField, Min(0.01f)] private float duration = 6f;
    [SerializeField, Min(0f)] private float extensionPerBullet = 0.5f;
    [SerializeField, Min(0f)] private float maxExtension = 3f;

    private float remainingTime;
    private float extendedTime;
    private float activeDuration;

    /// <summary>폭주 종료 알림. 구독자는 비활성화 시 해제한다.</summary>
    public event Action Ended;

    /// <summary>현재 폭주 중인지 여부.</summary>
    public bool IsActive => remainingTime > 0f;

    /// <summary>게이지에 표시할 남은 시간 비율.</summary>
    public float RemainingRatio => IsActive
        ? Mathf.Clamp01(remainingTime / activeDuration) : 0f;

    /// <summary>폭주를 시작한다. 이미 진행 중이면 시작하지 않는다.</summary>
    public void Begin()
    {
        if (IsActive)
            return;
        activeDuration = duration;
        remainingTime = activeDuration;
        extendedTime = 0f;
    }

    /// <summary>동시 보너스가 적용된 탄 수만큼 상한 내에서 연장한다.</summary>
    public void Extend(int bonusCount)
    {
        if (!IsActive || bonusCount <= 0)
            return;
        float extension = Mathf.Min(bonusCount * extensionPerBullet,
            Mathf.Max(0f, maxExtension - extendedTime));
        remainingTime += extension;
        extendedTime += extension;
    }

    private void Update()
    {
        if (!IsActive)
            return;
        remainingTime = Mathf.Max(0f, remainingTime - Time.deltaTime);
        if (!IsActive)
            Ended?.Invoke();
    }

    private void OnValidate()
    {
        duration = Mathf.Max(0.01f, duration);
        extensionPerBullet = Mathf.Max(0f, extensionPerBullet);
        maxExtension = Mathf.Max(0f, maxExtension);
    }
}
