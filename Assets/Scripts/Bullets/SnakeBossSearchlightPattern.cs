using System.Collections.Generic;
using UnityEngine;

/// <summary>뱀 보스의 두 서치라이트를 시작하고 포착과 합류를 조율합니다.</summary>
public sealed class SnakeBossSearchlightPattern : MonoBehaviour
{
    [SerializeField] private HeadBossSearchlightPattern left;
    [SerializeField] private HeadBossSearchlightPattern right;
    [SerializeField] private HeadBossFreezePattern freezePattern;
    [SerializeField] private SnakeBossSearchlightRocks rocks;
    [SerializeField] private HeadBossSearchlightVisual leftVisual;
    [SerializeField] private HeadBossSearchlightVisual rightVisual;
    [SerializeField] private Transform leftEye;
    [SerializeField] private Transform rightEye;

    // ##TEMP 다른 원이 포착한 원에 합류하는 시간.
    [SerializeField, Min(0.01f)] private float joinDuration = 0.12f;

    private readonly List<Vector4> leftShadow =
        new List<Vector4>(SnakeBossSearchlightRocks.MAX_SHADOW_VERTICES);
    private readonly List<Vector4> rightShadow =
        new List<Vector4>(SnakeBossSearchlightRocks.MAX_SHADOW_VERTICES);

    private PlayerHealth player;
    private SpriteRenderer field;
    private FieldManager fieldManager;
    private Bounds previousFieldBounds;
    private HeadBossSearchlightPattern captured;
    private HeadBossSearchlightPattern joining;
    private Vector2 joiningStart;
    private float joinElapsed;

    /// <summary>두 원의 추적 또는 포착한 원의 감지가 진행 중인지 반환합니다.</summary>
    public bool IsRunning { get; private set; }

    /// <summary>공유 감지 컴포넌트에 전투 참조를 연결합니다.</summary>
    public void Initialize(
        PlayerHealth player,
        SpriteRenderer field,
        FieldManager fieldManager)
    {
        this.player = player;
        this.field = field;
        this.fieldManager = fieldManager;
        freezePattern.Initialize(player);
        rocks.Initialize();
    }

    /// <summary>두 원의 추적을 동시에 시작합니다.</summary>
    public void BeginTracking()
    {
        if (!isActiveAndEnabled || IsRunning || player == null || player.IsDead
            || left == null || right == null || freezePattern == null
            || rocks == null || field == null || leftEye == null || rightEye == null)
        {
            return;
        }

        Transform target = player.transform;
        Vector2 playerPosition = target.position;
        previousFieldBounds = GetCurrentFieldBounds();

        rocks.SpawnAround(playerPosition, previousFieldBounds);
        RefreshShadows();
        if (!left.BeginCoordinatedTracking(
                target, position => SnakeBossSearchlightRocks.BlocksSight(leftShadow, position),
                HeadBossSearchlightPattern.CreateSweepDestination(GetCurrentFieldBounds, moveRight: true))
            || !right.BeginCoordinatedTracking(
                target, position => SnakeBossSearchlightRocks.BlocksSight(rightShadow, position),
                HeadBossSearchlightPattern.CreateSweepDestination(GetCurrentFieldBounds, moveRight: false)))
        {
            StopTracking();
            return;
        }

        IsRunning = true;
        TrySelectCapture(playerPosition);
    }

    private Bounds GetCurrentFieldBounds()
    {
        return fieldManager != null ? fieldManager.CurrentBounds : field.bounds;
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;
        if (!IsRunning || deltaTime <= 0f)
        {
            return;
        }

        if (player == null || player.IsDead)
        {
            StopTracking();
            return;
        }

        Bounds currentBounds = GetCurrentFieldBounds();
        if (!previousFieldBounds.Equals(currentBounds))
        {
            rocks.ReturnOutside(currentBounds);
            previousFieldBounds = currentBounds;
        }

        RefreshShadows();
        if (captured == null)
        {
            left.AdvanceCoordinated(deltaTime);
            right.AdvanceCoordinated(deltaTime);
            TrySelectCapture(player.transform.position);

            if (!left.IsRunning && !right.IsRunning)
            {
                StopTracking();
            }

            return;
        }

        captured.AdvanceCoordinated(deltaTime);
        if (!captured.IsRunning)
        {
            StopTracking();
            return;
        }

        if (joining != null)
        {
            AdvanceJoin(deltaTime);
        }
    }

    private void RefreshShadows()
    {
        rocks.BuildShadowTriangles(leftEye.position, leftShadow);
        rocks.BuildShadowTriangles(rightEye.position, rightShadow);
        leftVisual?.SetShadowTriangles(leftShadow);
        rightVisual?.SetShadowTriangles(rightShadow);
    }

    private void TrySelectCapture(Vector2 playerPosition)
    {
        if (!left.IsCentering && !right.IsCentering)
        {
            return;
        }

        bool useLeft = left.IsCentering && (!right.IsCentering
            || (playerPosition - left.Center).sqrMagnitude
                <= (playerPosition - right.Center).sqrMagnitude);

        captured = useLeft ? left : right;
        joining = useLeft ? right : left;

        if (!joining.IsRunning)
        {
            joining = null;
            return;
        }

        joiningStart = joining.Center;
        joinElapsed = 0f;
        joining.StopTracking();
        joining.ShowJoiningAt(joiningStart);
    }

    private void AdvanceJoin(float deltaTime)
    {
        joinElapsed += deltaTime;
        float progress = Mathf.Clamp01(joinElapsed / joinDuration);

        if (progress >= 1f || !captured.IsCentering)
        {
            joining.StopTracking();
            joining = null;
            return;
        }

        Vector2 position = Vector2.Lerp(joiningStart, captured.Center, progress);
        joining.ShowJoiningAt(position);
    }

    /// <summary>두 원을 숨기고 포착한 원의 감지를 종료합니다.</summary>
    public void StopTracking()
    {
        left?.StopTracking();
        right?.StopTracking();
        rocks?.ReturnAll();
        leftShadow.Clear();
        rightShadow.Clear();
        leftVisual?.SetShadowTriangles(leftShadow);
        rightVisual?.SetShadowTriangles(rightShadow);
        captured = null;
        joining = null;
        IsRunning = false;
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || !IsRunning || rocks == null)
        {
            return;
        }

        if (leftEye != null && left != null && left.IsRunning
            && (captured == null || captured == left))
        {
            SnakeBossSearchlightRocks.DrawShadowGizmos(leftShadow, left.Center, left.Radius, Color.cyan);
        }

        if (rightEye != null && right != null && right.IsRunning
            && (captured == null || captured == right))
        {
            SnakeBossSearchlightRocks.DrawShadowGizmos(rightShadow, right.Center, right.Radius,
                new Color(1f, 0.55f, 0.15f));
        }
    }

    private void OnDisable()
    {
        StopTracking();
    }
}
