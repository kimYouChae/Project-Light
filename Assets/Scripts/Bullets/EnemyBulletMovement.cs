using UnityEngine;

/// <summary>적탄의 직선·물결·추적 이동 상태를 발사별로 관리한다.</summary>
public sealed class EnemyBulletMovement : MonoBehaviour
{
    private enum MotionType { Straight, Wave, Homing, Accelerating }

    private MotionType motionType;
    private Vector2 startPosition;
    private Vector2 position;
    private Vector2 direction;
    private Vector2 perpendicular;
    private float speed;
    private float elapsed;
    private float amplitude;
    private float period;
    private float turnSpeed;
    private Transform target;
    private float acceleration;

    /// <summary>이전 발사의 이동 상태를 지우고 직선 이동으로 준비한다.</summary>
    public void PrepareStraight(Vector2 start, Vector2 aimDirection, float moveSpeed)
    {
        motionType = MotionType.Straight;
        startPosition = position = start;
        direction = aimDirection.sqrMagnitude > Mathf.Epsilon ? aimDirection.normalized : Vector2.down;
        perpendicular = new Vector2(-direction.y, direction.x);
        speed = Mathf.Max(0f, moveSpeed);
        elapsed = 0f;
        amplitude = 0f;
        period = 1f;
        turnSpeed = 0f;
        target = null;
        acceleration = 0f;
    }

    /// <summary>발사 시 조준 방향을 고정한 물결 이동을 준비한다.</summary>
    public void PrepareWave(Vector2 start, Vector2 aimDirection, float moveSpeed,
        float waveAmplitude, float wavePeriod)
    {
        PrepareStraight(start, aimDirection, moveSpeed);
        motionType = MotionType.Wave;
        amplitude = Mathf.Max(0f, waveAmplitude);
        period = Mathf.Max(0.01f, wavePeriod);
    }

    /// <summary>제한된 회전 속도로 타깃을 계속 따라가는 이동을 준비한다.</summary>
    public void PrepareHoming(Vector2 start, Vector2 aimDirection, float moveSpeed,
        Transform followTarget, float degreesPerSecond)
    {
        PrepareStraight(start, aimDirection, moveSpeed);
        motionType = MotionType.Homing;
        target = followTarget;
        turnSpeed = Mathf.Max(0f, degreesPerSecond);
    }

    /// <summary>고정 방향으로 점점 빨라지는 이동을 준비한다.</summary>
    public void PrepareAccelerating(Vector2 start, Vector2 aimDirection, float initialSpeed,
        float moveAcceleration)
    {
        PrepareStraight(start, aimDirection, initialSpeed);
        motionType = MotionType.Accelerating;
        acceleration = Mathf.Max(0f, moveAcceleration);
    }

    /// <summary>지정 시간만큼 이동한 월드 위치를 반환한다.</summary>
    public Vector2 Move(float deltaTime)
    {
        deltaTime = Mathf.Max(0f, deltaTime);
        if (motionType == MotionType.Homing && target != null)
        {
            Vector2 desired = (Vector2)target.position - position;
            if (desired.sqrMagnitude > Mathf.Epsilon)
            {
                float angle = Vector2.SignedAngle(direction, desired);
                float turn = Mathf.Clamp(angle, -turnSpeed * deltaTime, turnSpeed * deltaTime);
                direction = Quaternion.Euler(0f, 0f, turn) * direction;
            }
        }

        position = EvaluatePosition(deltaTime);
        if (motionType == MotionType.Wave)
            elapsed += deltaTime;
        else if (motionType == MotionType.Accelerating)
            speed += acceleration * deltaTime;

        return position;
    }

    /// <summary>현재 이동 상태에서 지정 게임 시간 뒤의 위치를 읽습니다.</summary>
    public Vector2 EvaluatePosition(float seconds)
    {
        if (motionType == MotionType.Wave)
        {
            float waveTime = elapsed + seconds;
            Vector2 forward = direction * speed * waveTime;
            Vector2 sideways = perpendicular * amplitude
                * Mathf.Sin(waveTime * Mathf.PI * 2f / period);
            return startPosition + forward + sideways;
        }

        float distance = speed * seconds;
        if (motionType == MotionType.Accelerating)
            distance += 0.5f * acceleration * seconds * seconds;
        return position + direction * distance;
    }

}
