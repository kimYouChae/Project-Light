using System;
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

    // ##TEMP 포착 후 플레이어 중앙까지 이동하는 시간.
    [SerializeField, Min(0.01f)] private float centerMoveDuration = 0.3f;

    private Vector3 originLocalPosition;
    private Transform target;
    private Func<Vector2, bool> isSightBlocked;
    private Func<Vector2, Vector2> selectTrackingDestination;
    private Vector2 center;
    private float radius;
    private float trackingElapsed;
    private bool isTracking;
    private bool isCentering;
    private bool isCaptured;
    private bool isCoordinated;
    private Vector2 centerMoveStart;
    private float centerMoveElapsed;

    /// <summary>추적, 중앙 이동 또는 감지가 진행 중인지 나타냅니다.</summary>
    public bool IsRunning => isTracking || isCentering || isCaptured;

    /// <summary>포착 후 플레이어 중앙으로 이동 중인지 반환합니다.</summary>
    public bool IsCentering => isCentering;

    /// <summary>현재 바닥 원의 중심을 반환합니다.</summary>
    public Vector2 Center => center;

    /// <summary>현재 바닥 원의 반경을 반환합니다.</summary>
    public float Radius => radius;

    private void Awake()
    {
        if (visual != null)
        {
            // 이동 전 프리팹의 위치를 저장해 매번 같은 자리에서 시작한다.
            originLocalPosition = visual.transform.localPosition;
        }
    }

    /// <summary>머리 아래 기준 위치에서 필드 탐색을 시작합니다.</summary>
    public void BeginTracking(Transform player, SpriteRenderer field)
    {
        if (field == null)
        {
            return;
        }

        TryBeginTracking(player, coordinated: false,
            selectDestination: CreateSweepDestination(() => field.bounds, moveRight: true));
    }

    /// <summary>필드의 좌우 절반에서 번갈아 목표를 고르는 함수를 만듭니다.</summary>
    public static Func<Vector2, Vector2> CreateSweepDestination(
        Func<Bounds> getBounds, bool moveRight)
    {
        Vector2? destination = null;

        return currentCenter =>
        {
            Bounds bounds = getBounds();
            bool reached = destination.HasValue
                && currentCenter.Equals(destination.Value);
            if (reached)
            {
                moveRight = !moveRight;
            }

            if (!destination.HasValue || reached
                || destination.Value.x < bounds.min.x || destination.Value.x > bounds.max.x
                || destination.Value.y < bounds.min.y || destination.Value.y > bounds.max.y)
            {
                float x = moveRight
                    ? UnityEngine.Random.Range(bounds.center.x, bounds.max.x)
                    : UnityEngine.Random.Range(bounds.min.x, bounds.center.x);
                float y = UnityEngine.Random.Range(bounds.min.y, bounds.max.y);
                destination = new Vector2(x, y);
            }

            return destination.Value;
        };
    }

    /// <summary>보스 기준 시작 위치에서 조율 패턴의 서치라이트를 시작합니다.</summary>
    public bool BeginCoordinatedTracking(
        Transform player,
        Func<Vector2, bool> isSightBlocked,
        Func<Vector2, Vector2> selectDestination)
    {
        return TryBeginTracking(
            player, coordinated: true, isSightBlocked: isSightBlocked,
            selectDestination: selectDestination);
    }

    private bool TryBeginTracking(
        Transform player, bool coordinated, Func<Vector2, Vector2> selectDestination,
        Func<Vector2, bool> isSightBlocked = null)
    {
        if (!isActiveAndEnabled || visual == null || freezePattern == null
            || player == null || selectDestination == null || IsRunning)
        {
            return false;
        }

        isCoordinated = coordinated;
        this.isSightBlocked = isSightBlocked;
        selectTrackingDestination = selectDestination;
        target = player;
        center = transform.TransformPoint(originLocalPosition);
        radius = startRadius;
        trackingElapsed = 0f;
        isTracking = true;
        visual.Show(center, radius);
        TryCapture();
        return true;
    }

    private void Update()
    {
        if (!isCoordinated)
        {
            Advance(Time.deltaTime);
        }
    }

    /// <summary>뱀 보스가 관리하는 서치라이트를 한 프레임 진행합니다.</summary>
    public void AdvanceCoordinated(float deltaTime)
    {
        if (isCoordinated)
        {
            Advance(deltaTime);
        }
    }

    /// <summary>합류 중인 원을 기존 반경으로 표시합니다.</summary>
    public void ShowJoiningAt(Vector2 position)
    {
        center = position;
        visual.Show(center, radius);
    }

    private void Advance(float deltaTime)
    {
        if (!IsRunning || deltaTime <= 0f)
        {
            return;
        }

        if (target == null)
        {
            StopTracking();
            return;
        }

        if (isCentering)
        {
            AdvanceCenterMove(deltaTime);
            return;
        }

        if (isCaptured)
        {
            if (freezePattern == null || !freezePattern.IsDetecting)
            {
                StopTracking();
            }

            return;
        }

        trackingElapsed += deltaTime;

        if (trackingElapsed >= trackingDuration)
        {
            PlayLog.Write("HeadBoss", "Searchlight Tracking Timeout");
            StopTracking();
            return;
        }

        Vector2 destination = selectTrackingDestination(center);
        center = Vector2.MoveTowards(center, destination, trackingSpeed * deltaTime);

        radius = Mathf.MoveTowards(
            radius, maxRadius, growthSpeed * deltaTime);

        visual.Show(center, radius);
        TryCapture();
    }

    private void TryCapture()
    {
        Vector2 playerPosition = target.position;
        Vector2 offset = playerPosition - center;
        if (offset.sqrMagnitude > radius * radius
            || isSightBlocked?.Invoke(playerPosition) == true)
        {
            return;
        }

        isTracking = false;
        isCentering = true;
        centerMoveStart = center;
        centerMoveElapsed = 0f;
        PlayLog.Write("HeadBoss", "Searchlight Capture");
    }

    private void AdvanceCenterMove(float deltaTime)
    {
        centerMoveElapsed += deltaTime;
        float progress = Mathf.Clamp01(centerMoveElapsed / centerMoveDuration);
        center = Vector2.Lerp(centerMoveStart, (Vector2)target.position, progress);
        visual.Show(center, radius);

        if (progress < 1f)
        {
            return;
        }

        isCentering = false;
        isCaptured = true;
        freezePattern.BeginDetection(center, radius, visual, isSightBlocked);

        if (!freezePattern.IsDetecting)
        {
            StopTracking();
        }
    }

    /// <summary>추적과 감지를 중단하고 서치라이트를 숨깁니다.</summary>
    public void StopTracking()
    {
        isCoordinated = false;
        isTracking = false;
        isCentering = false;
        target = null;
        isSightBlocked = null;
        selectTrackingDestination = null;

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
        centerMoveDuration = Mathf.Max(0.01f, centerMoveDuration);
    }
}
