using System;
using UnityEngine;

/// <summary>머리 보스의 등장 이동과 등장 완료 알림을 관리합니다.</summary>
public sealed class HeadBossEntrance : MonoBehaviour
{
    // ##TEMP 머리 보스 등장 시작 위치. 화면 위에서 시작한다.
    [SerializeField] private Vector3 startPosition =
        new Vector3(0f, 6.5f, 0f);

    // ##TEMP 3-1에서 승인한 등장 완료 위치.
    [SerializeField] private Vector3 endPosition =
        new Vector3(0f, 3.5f, 0f);

    // ##TEMP 승인한 등장 이동 시작값.
    [SerializeField, Min(0.01f)] private float duration = 1f;

    private float elapsed;
    private bool isEntering;
    private bool isComplete;

    private Action entranceCompleted;

    /// <summary>등장 이동이 완료됐는지 반환합니다.</summary>
    public bool IsComplete => isComplete;

    /// <summary>등장 완료 알림을 등록합니다.</summary>
    public void RegisterEntranceCompleted(Action callback)
    {
        entranceCompleted += callback;
    }

    /// <summary>등장 완료 알림을 해제합니다.</summary>
    public void UnregisterEntranceCompleted(Action callback)
    {
        entranceCompleted -= callback;
    }

    /// <summary>
    /// 머리 보스의 등장 이동을 한 번 시작합니다.
    /// HeadBossPattern.BeginEntrance가 호출합니다.
    /// </summary>
    public void BeginEntrance()
    {
        if (isEntering || isComplete)
        {
            return;
        }

        transform.position = startPosition;
        elapsed = 0f;
        isComplete = false;
        isEntering = true;
    }

    private void Update()
    {
        if (!isEntering || Time.timeScale <= 0f)
        {
            return;
        }

        elapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(elapsed / duration);

        transform.position = Vector3.Lerp(
            startPosition,
            endPosition,
            Mathf.SmoothStep(0f, 1f, progress));

        if (progress < 1f)
        {
            return;
        }

        // 완료 상태를 먼저 설정해 알림을 한 번만 실행한다.
        isEntering = false;
        isComplete = true;
        entranceCompleted?.Invoke();
    }

    private void OnValidate()
    {
        duration = Mathf.Max(0.01f, duration);
    }
}
