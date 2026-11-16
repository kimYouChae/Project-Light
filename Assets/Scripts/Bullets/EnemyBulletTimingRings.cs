using UnityEngine;

/// <summary>예측된 코어 교차 시각에 맞춰 활성 적탄의 표시 원을 그립니다.</summary>
[RequireComponent(typeof(PlayerHealth), typeof(PlayerMovement), typeof(PlayerLight))]
public sealed class EnemyBulletTimingRings : MonoBehaviour
{
    private const float CURVE_TIME_RESOLUTION = 0.001f;

    [SerializeField] private BulletRegistry bulletRegistry;
    // ##TEMP 타이밍 원 등장 시간과 탄 지름 대비 시작 크기.
    [SerializeField, Min(0.01f)] private float appearanceLeadTime = 0.5f;
    [SerializeField, Min(1f)] private float startSizeMultiplier = 3f;

    private PlayerHealth player;
    private PlayerMovement movement;
    private PlayerLight playerLight;

    private void Awake()
    {
        player = GetComponent<PlayerHealth>();
        movement = GetComponent<PlayerMovement>();
        playerLight = GetComponent<PlayerLight>();
    }

    private void LateUpdate()
    {
        if (bulletRegistry == null || Time.timeScale <= 0f)
            return;

        var prediction = new PlayerPrediction(
            movement, transform.position, movement.GetRequestedVelocity(), playerLight.CoreRadius);
        bool canShow = !player.IsDead && !movement.IsHeldByBite;
        foreach (TestBullet bullet in bulletRegistry.ActiveBullets)
        {
            if (bullet == null)
                continue;

            if (canShow && bullet.CanShowTimingRing(player)
                && TryPredictContact(bullet, prediction, out float seconds))
            {
                float remainingRatio = Mathf.Clamp01(seconds / appearanceLeadTime);
                bullet.ShowTimingRing(Mathf.Lerp(1f, startSizeMultiplier, remainingRatio));
            }
            else
            {
                bullet.HideTimingRing();
            }
        }
    }

    private bool TryPredictContact(TestBullet bullet, PlayerPrediction playerState, out float seconds)
    {
        seconds = 0f;
        float horizon = Mathf.Min(appearanceLeadTime, bullet.RemainingLifetime);
        Vector2 start = playerState.RelativePosition(bullet, 0f);
        float distance = start.magnitude - playerState.Radius;
        float speedBound = bullet.GetMaxSpeed(horizon) + playerState.Velocity.magnitude;
        if (horizon <= 0f || distance < 0f || distance > speedBound * horizon)
            return false;

        Vector2 end = playerState.RelativePosition(bullet, horizon);
        if (bullet.Movement == null || bullet.Movement.IsStraight)
            return TryPredictStraight(bullet, playerState, start, horizon, out seconds);

        return TryFindCurveEntry(bullet, playerState, speedBound, 0f, start, horizon, end,
            out seconds);
    }

    private static bool TryPredictStraight(TestBullet bullet, PlayerPrediction playerState,
        Vector2 start, float horizon, out float seconds)
    {
        Vector2 unclamped = playerState.Position + playerState.Velocity * horizon;
        Vector2 clamped = playerState.Clamp(unclamped);
        Vector2 velocity = playerState.Velocity;
        float xStop = velocity.x == 0f ? horizon
            : Mathf.Clamp((clamped.x - playerState.Position.x) / velocity.x, 0f, horizon);
        float yStop = velocity.y == 0f ? horizon
            : Mathf.Clamp((clamped.y - playerState.Position.y) / velocity.y, 0f, horizon);

        float segmentStart = 0f;
        for (int index = 0; index < 3; index++)
        {
            float segmentEnd = index == 0 ? Mathf.Min(xStop, yStop)
                : index == 1 ? Mathf.Max(xStop, yStop) : horizon;
            if (segmentEnd <= segmentStart)
                continue;

            Vector2 end = playerState.RelativePosition(bullet, segmentEnd);
            if (TryGetEntryFraction(start, end, playerState.Radius, out float fraction))
            {
                seconds = Mathf.Lerp(segmentStart, segmentEnd, fraction);
                return true;
            }

            start = end;
            segmentStart = segmentEnd;
        }

        seconds = 0f;
        return false;
    }

    private static bool TryFindCurveEntry(TestBullet bullet, PlayerPrediction playerState,
        float speedBound, float from, Vector2 start, float to, Vector2 end, out float seconds)
    {
        seconds = 0f;
        float duration = to - from;
        float radius = playerState.Radius;
        if (Mathf.Min(start.magnitude, end.magnitude) > radius + speedBound * duration * 0.5f)
            return false;

        if (duration <= CURVE_TIME_RESOLUTION)
        {
            if (end.sqrMagnitude >= radius * radius
                || !TryGetEntryFraction(start, end, radius, out float fraction))
            {
                return false;
            }

            seconds = Mathf.Lerp(from, to, fraction);
            return true;
        }

        float middle = (from + to) * 0.5f;
        Vector2 midpoint = playerState.RelativePosition(bullet, middle);
        return TryFindCurveEntry(bullet, playerState, speedBound, from, start, middle, midpoint, out seconds)
            || TryFindCurveEntry(bullet, playerState, speedBound, middle, midpoint, to, end, out seconds);
    }

    private static bool TryGetEntryFraction(Vector2 start, Vector2 end, float radius, out float fraction)
    {
        fraction = 0f;
        Vector2 step = end - start;
        float outside = start.sqrMagnitude - radius * radius;
        float approach = Vector2.Dot(start, step);
        float discriminant = approach * approach - step.sqrMagnitude * outside;
        if (outside < 0f || approach >= 0f || discriminant <= 0f)
            return false;

        fraction = outside / (-approach + Mathf.Sqrt(discriminant));
        return fraction <= 1f;
    }

    private void OnDisable()
    {
        if (bulletRegistry == null)
            return;

        foreach (TestBullet bullet in bulletRegistry.ActiveBullets)
        {
            if (bullet != null)
                bullet.HideTimingRing();
        }
    }

    private readonly struct PlayerPrediction
    {
        private readonly PlayerMovement movement;
        public readonly Vector2 Position;
        public readonly Vector2 Velocity;
        public readonly float Radius;

        public PlayerPrediction(PlayerMovement movement, Vector2 position, Vector2 velocity, float radius)
        {
            this.movement = movement;
            Position = position;
            Velocity = velocity;
            Radius = radius;
        }

        public Vector2 Clamp(Vector2 position) => movement.ClampToPlayfield(position);

        public Vector2 RelativePosition(TestBullet bullet, float seconds) =>
            bullet.PredictPosition(seconds) - Clamp(Position + Velocity * seconds);
    }
}
