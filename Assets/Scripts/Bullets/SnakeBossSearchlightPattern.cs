using UnityEngine;

/// <summary>뱀 보스의 두 서치라이트를 시작하고 포착과 합류를 조율합니다.</summary>
public sealed class SnakeBossSearchlightPattern : MonoBehaviour
{
    [SerializeField] private HeadBossSearchlightPattern left;
    [SerializeField] private HeadBossSearchlightPattern right;
    [SerializeField] private HeadBossFreezePattern freezePattern;
    [SerializeField] private SnakeBossSearchlightRocks rocks;
    [SerializeField] private Transform leftEye;
    [SerializeField] private Transform rightEye;

    // ##TEMP 다른 원이 포착한 원에 합류하는 시간.
    [SerializeField, Min(0.01f)] private float joinDuration = 0.12f;

    private PlayerHealth player;
    private SpriteRenderer field;
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
        BulletRegistry registry,
        Transform bulletPoolParent)
    {
        this.player = player;
        this.field = field;
        freezePattern.Initialize(player, field, registry, bulletPoolParent);
        rocks.Initialize(field);
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
        Bounds bounds = field.bounds;

        rocks.SpawnAround(playerPosition);
        if (!left.BeginCoordinatedTracking(
                target, position => rocks.BlocksSight(leftEye.position, position),
                CreateSweepDestination(bounds, moveRight: true))
            || !right.BeginCoordinatedTracking(
                target, position => rocks.BlocksSight(rightEye.position, position),
                CreateSweepDestination(bounds, moveRight: false)))
        {
            StopTracking();
            return;
        }

        IsRunning = true;
        TrySelectCapture(playerPosition);
    }

    private System.Func<Vector2, Vector2> CreateSweepDestination(
        Bounds bounds, bool moveRight)
    {
        Vector2? destination = null;

        return currentCenter =>
        {
            if (!destination.HasValue || currentCenter.Equals(destination.Value))
            {
                float x = moveRight
                    ? Random.Range(bounds.center.x, bounds.max.x)
                    : Random.Range(bounds.min.x, bounds.center.x);

                destination = new Vector2(x, currentCenter.y);
                moveRight = !moveRight;
            }

            return destination.Value;
        };
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
        captured = null;
        joining = null;
        IsRunning = false;
    }

    private void OnDisable()
    {
        StopTracking();
    }
}
