using UnityEngine;

/// <summary>꼬리 꽂기의 하강과 퍼펙트 판정을 처리합니다.</summary>
public sealed class SnakeBossTailStabPattern : MonoBehaviour
{
    [SerializeField] private SpriteRenderer tailPrefab;
    [SerializeField] private LineRenderer perfectRing;

    // ##TEMP 기본 하강 속도(게임 시간 1초당 이동 거리)와 표시 크기.
    [SerializeField, Min(0.01f)] private float descentSpeed = 8f;
    [SerializeField] private Vector2 tailSize = new Vector2(1f, 2.5f);
    [SerializeField, Min(1)] private int initialPoolCount = 1;

    // ##TEMP 첫 꼬리 꽂기의 슬로모션 및 판정선 설정.
    [SerializeField, Min(0f)] private float slowLeadDistance = 2f;
    [SerializeField, Range(0.01f, 1f)] private float slowTimeScale = 0.25f;
    [SerializeField] private Color approachingColor = new Color(1f, 0.3f, 0.2f);
    [SerializeField] private Color readyColor = new Color(1f, 0.9f, 0.35f);

    // ##TEMP 반복 꽂기용 감속 설정. 첫 꽂기와 같은 값에서 시작한다.
    [SerializeField, Min(0f)] private float repeatSlowLeadDistance = 2f;
    [SerializeField, Range(0.01f, 1f)] private float repeatSlowTimeScale = 0.25f;

    private ComponentPool<SpriteRenderer> pool;
    private SpriteRenderer activeTail;
    private Transform target;
    private PlayerHealth targetHealth;
    private PlayerLight playerLight;
    private PlayerPerfectDodge perfectDodge;
    private bool isSlowing;
    private float previousTimeScale;
    private float appliedTimeScale;
    private bool isFirstStab;
    private float activeSlowLeadDistance;
    private float activeSlowTimeScale;
    private double slowStartedAt;
    private float tipY;

    /// <summary>꼬리가 내려오는 중인지 반환합니다.</summary>
    public bool IsRunning => activeTail != null;

    /// <summary>전투에서 사용할 꼬리 표시를 사전 생성합니다.</summary>
    public void Initialize(Transform poolParent)
    {
        pool = new ComponentPool<SpriteRenderer>(
            new TailStabFactory(tailPrefab, poolParent), initialPoolCount, poolParent);
    }

    /// <summary>화면 위에서 플레이어 위치를 향해 꼬리 하강을 시작합니다.</summary>
    public void BeginStab(PlayerHealth player, Bounds fieldBounds, bool isFirstStab)
    {
        if (IsRunning || player == null || pool == null)
        {
            return;
        }

        playerLight = player.GetComponent<PlayerLight>();
        perfectDodge = player.GetComponent<PlayerPerfectDodge>();
        if (playerLight == null || perfectDodge == null)
        {
            StopStab();
            return;
        }

        targetHealth = player;
        this.isFirstStab = isFirstStab;
        activeSlowLeadDistance = isFirstStab ? slowLeadDistance : repeatSlowLeadDistance;
        activeSlowTimeScale = isFirstStab ? slowTimeScale : repeatSlowTimeScale;
        target = player.transform;
        tipY = fieldBounds.max.y;

        Camera camera = Camera.main;
        if (camera != null)
        {
            float depth = camera.WorldToViewportPoint(target.position).z;
            float screenTop = camera.ViewportToWorldPoint(
                new Vector3(0.5f, 1f, depth)).y;
            tipY = Mathf.Max(tipY, screenTop);
        }

        activeTail = pool.Rent(tail =>
        {
            Vector3 spriteSize = tail.sprite.bounds.size;
            tail.transform.localScale = new Vector3(
                tailSize.x / spriteSize.x, tailSize.y / spriteSize.y, 1f);
            SetTailPose(tail);
        });

        perfectDodge.RegisterTailStabPerfect(TryPerfect);
        PlayLog.Write("SnakeBoss", isFirstStab ? "First Tail Stab Start" : "Tail Stab Start");
    }

    /// <summary>플레이어의 현재 위치에 맞춰 꼬리 하강을 진행합니다.</summary>
    public void Advance(float deltaTime)
    {
        if (!IsRunning || targetHealth == null || targetHealth.IsDead)
        {
            StopStab();
            return;
        }

        float playerY = target.position.y;
        float nextTipY = tipY - descentSpeed * deltaTime;
        float perfectOuterDistance =
            playerLight.CoreRadius + perfectDodge.PerfectTolerance;
        float slowStartY = playerY + perfectOuterDistance + activeSlowLeadDistance;

        if (!isSlowing && nextTipY <= slowStartY)
        {
            if (tipY > slowStartY)
            {
                nextTipY = slowStartY;
            }

            previousTimeScale = Time.timeScale;
            appliedTimeScale = previousTimeScale;
            isSlowing = true;
            slowStartedAt = Time.unscaledTimeAsDouble;
            PlayLog.Write("SnakeBoss",
                $"Tail Stab Slow Start (targetScale={previousTimeScale * activeSlowTimeScale:F2})");
        }

        tipY = nextTipY;
        SetTailPose(activeTail);

        if (isSlowing && Time.timeScale == appliedTimeScale)
        {
            float tipDistance = Mathf.Max(0f, tipY - playerY);
            float progress = activeSlowLeadDistance > 0f
                ? Mathf.InverseLerp(
                    perfectOuterDistance + activeSlowLeadDistance, perfectOuterDistance, tipDistance)
                : 1f;
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);
            float nextScale = previousTimeScale * Mathf.Lerp(1f, activeSlowTimeScale, easedProgress);

            appliedTimeScale = Mathf.Min(appliedTimeScale, nextScale);
            Time.timeScale = appliedTimeScale;
        }

        if (tipY <= playerY)
        {
            PlayerHealth hitPlayer = targetHealth;
            PlayLog.Write("SnakeBoss", isFirstStab
                ? "First Tail Stab Reached Player"
                : "Tail Stab Reached Player");
            PlayLog.Write("SnakeBoss", "Tail Stab Perfect Failed");
            StopStab();
            hitPlayer.ApplyTailStabDamage();
        }
    }

    private bool CanPerfect()
    {
        if (!IsRunning || !isSlowing || targetHealth == null || targetHealth.IsDead)
        {
            return false;
        }

        Vector2 tipPosition = activeTail.transform.position
            - Vector3.up * (tailSize.y * 0.5f);
        return playerLight.IsOnCoreBoundary(
            tipPosition, 0f, perfectDodge.PerfectTolerance);
    }

    private bool TryPerfect()
    {
        if (!CanPerfect())
        {
            return false;
        }

        PlayLog.Write("SnakeBoss", "Tail Stab Perfect Success");
        StopStab();
        return true;
    }

    private void LateUpdate()
    {
        if (!IsRunning)
        {
            return;
        }

        if (targetHealth == null || targetHealth.IsDead)
        {
            StopStab();
            return;
        }

        if (!isSlowing || perfectRing == null)
        {
            return;
        }

        Vector3 center = playerLight.transform.position;
        float radius = playerLight.CoreRadius;
        Color color = CanPerfect() ? readyColor : approachingColor;
        perfectRing.startColor = perfectRing.endColor = color;

        for (int index = 0; index < perfectRing.positionCount; index++)
        {
            float angle = 2f * Mathf.PI * index / perfectRing.positionCount;
            Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
            perfectRing.SetPosition(index, center + offset * radius);
        }

        perfectRing.enabled = true;
    }

    private void SetTailPose(SpriteRenderer tail)
    {
        Vector3 position = target.position;
        position.y = tipY + tailSize.y * 0.5f;
        tail.transform.position = position;
    }

    /// <summary>진행 중인 꼬리 표시를 풀에 반환합니다.</summary>
    public void StopStab()
    {
        if (perfectDodge != null)
        {
            perfectDodge.UnregisterTailStabPerfect(TryPerfect);
        }

        if (perfectRing != null)
        {
            perfectRing.enabled = false;
        }

        if (isSlowing)
        {
            bool restored = Time.timeScale == appliedTimeScale;
            if (restored)
            {
                Time.timeScale = previousTimeScale;
            }

            double realSeconds = Time.unscaledTimeAsDouble - slowStartedAt;
            PlayLog.Write("SnakeBoss",
                $"Tail Stab Slow End (first={isFirstStab}, realSeconds={realSeconds:F3}, "
                + $"restored={restored}, timeScale={Time.timeScale:F2})");
        }

        isSlowing = false;

        if (activeTail != null)
        {
            pool.Return(activeTail);
            activeTail = null;
        }

        target = null;
        targetHealth = null;
        playerLight = null;
        perfectDodge = null;
    }

    private void OnDisable() => StopStab();

    private void OnDestroy() => pool?.Clear();
}
