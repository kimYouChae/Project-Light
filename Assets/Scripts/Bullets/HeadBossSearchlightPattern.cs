using UnityEngine;

/// <summary>서치라이트 중심을 플레이어 쪽으로 옮기고 반경을 키웁니다.</summary>
public sealed class HeadBossSearchlightPattern : MonoBehaviour
{
    [SerializeField] private HeadBossSearchlightVisual visual;
    [SerializeField] private HeadBossFreezePattern freezePattern;

    // ##TEMP 승인한 서치라이트 프로토타입 시작값.
    [SerializeField, Min(0.01f)] private float startRadius = 0.6f;
    [SerializeField, Min(0.01f)] private float maxRadius = 2.6f;
    [SerializeField, Min(0f)] private float trackingSpeed = 2f;
    [SerializeField, Min(0f)] private float growthSpeed = 0.5f;

    // ##TEMP 포착하지 못했을 때 추적을 끝내는 시간.
    [SerializeField, Min(0.01f)] private float trackingDuration = 6f;

    private Vector3 originLocalPosition;
    private Transform target;
    private Vector2 center;
    private float radius;
    private float trackingElapsed;
    private bool isTracking;
    private bool isCaptured;

    /// <summary>추적 또는 포착 후 감지가 진행 중인지 나타냅니다.</summary>
    public bool IsRunning => isTracking || isCaptured;

    private void Awake()
    {
        if (visual != null)
        {
            // 이동 전 프리팹의 위치를 저장해 매번 같은 자리에서 시작한다.
            originLocalPosition = visual.transform.localPosition;
        }
    }

    /// <summary>머리 아래 기준 위치에서 플레이어 추적을 시작합니다.</summary>
    public void BeginTracking(Transform player)
    {
        if (!isActiveAndEnabled || visual == null || freezePattern == null
            || player == null || isTracking || isCaptured)
        {
            return;
        }

        target = player;
        center = transform.TransformPoint(originLocalPosition);
        radius = startRadius;
        trackingElapsed = 0f;
        isTracking = true;
        visual.Show(center, radius);
        TryCapture();
    }

    private void Update()
    {
        if (!IsRunning || Time.deltaTime <= 0f)
        {
            return;
        }

        if (target == null)
        {
            StopTracking();
            return;
        }

        if (isCaptured)
        {
            if (freezePattern == null || !freezePattern.IsDetecting)
            {
                StopTracking();
                return;
            }

            if (freezePattern.HasTriggeredFreeze)
            {
                // 플레이어가 굳으면 원의 크기를 유지한 채 중앙으로 옮긴다.
                center = Vector2.MoveTowards(
                    center, target.position, trackingSpeed * Time.deltaTime);
                visual.Show(center, radius);
            }

            return;
        }

        float deltaTime = Time.deltaTime;
        trackingElapsed += deltaTime;

        if (trackingElapsed >= trackingDuration)
        {
            PlayLog.Write("HeadBoss", "Searchlight Tracking Timeout");
            StopTracking();
            return;
        }

        center = Vector2.MoveTowards(
            center, target.position, trackingSpeed * deltaTime);

        radius = Mathf.MoveTowards(
            radius, maxRadius, growthSpeed * deltaTime);

        visual.Show(center, radius);
        TryCapture();
    }

    private void TryCapture()
    {
        Vector2 offset = (Vector2)target.position - center;
        if (offset.sqrMagnitude > radius * radius)
        {
            return;
        }

        // 접촉 순간의 원을 고정하고 이동 감지를 시작한다.
        isTracking = false;
        isCaptured = true;
        PlayLog.Write("HeadBoss", "Searchlight Capture");
        freezePattern.BeginDetection(center, radius);

        if (!freezePattern.IsDetecting)
        {
            StopTracking();
        }
    }

    /// <summary>추적과 감지를 중단하고 서치라이트를 숨깁니다.</summary>
    public void StopTracking()
    {
        isTracking = false;
        target = null;

        if (isCaptured && freezePattern != null)
        {
            freezePattern.StopDetection();
        }

        isCaptured = false;

        if (visual != null)
        {
            visual.Hide();
        }
    }

    private void OnDisable()
    {
        StopTracking();
    }

    private void OnValidate()
    {
        startRadius = Mathf.Max(0.01f, startRadius);
        maxRadius = Mathf.Max(startRadius, maxRadius);
        trackingSpeed = Mathf.Max(0f, trackingSpeed);
        growthSpeed = Mathf.Max(0f, growthSpeed);
        trackingDuration = Mathf.Max(0.01f, trackingDuration);
    }
}
