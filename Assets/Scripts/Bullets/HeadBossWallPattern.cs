using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>판정 없는 양쪽 벽을 생성하고 코어 바깥까지 접근시킵니다.</summary>
public sealed class HeadBossWallPattern : MonoBehaviour
{
    [SerializeField] private SpriteRenderer wallVisualPrefab;
    [SerializeField] private SpriteRenderer areaWarningPrefab;

    // ##TEMP 승인한 벽 접근 전 경고 시간.
    [SerializeField, Min(0.01f)] private float warningDuration = 2f;

    // ##TEMP 벽 표시 크기·세로 간격.
    [SerializeField, Min(0.01f)] private float visualRadius = 0.12f;
    [SerializeField, Min(0.01f)] private float verticalSpacing = 0.3f;
    // ##TEMP 플레이어 중심에서 좌우 벽 중심까지의 거리.
    [SerializeField, Min(0f)] private float wallDistance = 2f;

    // ##TEMP 승인한 접근 시간과 풀 사전 생성 시작값.
    [SerializeField, Min(0.01f)] private float approachDuration = 1f;
    [SerializeField, Min(1)] private int initialPoolCount = 64;

    // 초기화 시 전달받는 씬 참조
    private PlayerHealth playerHealth;
    private SpriteRenderer playfield;
    private Transform wallParent;

    private readonly List<SpriteRenderer> leftWall =
        new List<SpriteRenderer>();

    private readonly List<SpriteRenderer> rightWall =
        new List<SpriteRenderer>();

    private ComponentPool<SpriteRenderer> pool;
    private Action<SpriteRenderer> prepareVisual;

    private ComponentPool<SpriteRenderer> warningPool;
    private Action<SpriteRenderer> prepareWarning;
    private SpriteRenderer leftWarning;
    private SpriteRenderer rightWarning;

    private Bounds capturedBounds;
    private float safeHalfWidth;
    private float warningElapsed;
    private bool showingWarning;

    // 경고 표시물을 대여하기 전에 설정할 정보
    private Vector3 warningPosition;
    private Vector3 warningScale;

    private Vector3 preparingPosition;
    private float leftStartX;
    private float rightStartX;
    private float leftEndX;
    private float rightEndX;
    private float elapsed;
    private bool approaching;

    /// <summary>경고 종료 후 양쪽 벽이 정지 위치에 도착했는지 반환합니다.</summary>
    public bool IsApproachComplete =>
        !showingWarning && leftWall.Count > 0 && !approaching;

    /// <summary>벽과 경고 표시물이 모두 정리됐는지 반환합니다.</summary>
    public bool IsCleared =>
        !showingWarning
        && leftWarning == null
        && rightWarning == null
        && leftWall.Count == 0
        && rightWall.Count == 0;

    /// <summary>
    /// 플레이어·필드·풀 부모를 연결합니다.
    /// HeadBossPattern.Initialize가 호출합니다.
    /// </summary>
    public void Initialize(
        PlayerHealth player,
        SpriteRenderer field,
        Transform poolParent)
    {
        playerHealth = player;
        playfield = field;
        wallParent = poolParent;
    }

    /// <summary>
    /// 현재 위치를 기준으로 좌우 경고를 표시한 뒤 벽 접근을 시작합니다.
    /// HeadBossPattern.Update가 호출합니다.
    /// </summary>
    public void BeginWalls()
    {
        if (!isActiveAndEnabled || !IsCleared)
        {
            return;
        }

        if (wallVisualPrefab == null || areaWarningPrefab == null
            || playerHealth == null
            || playfield == null || wallParent == null)
        {
            Debug.LogError(
                "머리 벽 패턴의 표시·경고 프리팹과 씬 참조를 확인해주세요.",
                this);
            return;
        }

        try
        {
            if (pool == null)
            {
                prepareVisual = PrepareVisual;

                pool = new ComponentPool<SpriteRenderer>(
                    new HeadBossWallVisualFactory(
                        wallVisualPrefab, wallParent),
                    initialPoolCount,
                    wallParent);
            }
            if (warningPool == null)
            {
                prepareWarning = PrepareWarning;

                // 좌우 경고 두 개를 반복 대여한다.
                warningPool = new ComponentPool<SpriteRenderer>(
                    new HeadBossWallVisualFactory(
                        areaWarningPrefab, wallParent),
                    2,
                    wallParent);
            }
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
            return;
        }

        // 경고와 벽이 같은 위치를 사용하도록 시작 순간의 값을 고정한다.
        capturedBounds = playfield.bounds;
        float centerX = playerHealth.transform.position.x;

        // 벽 중심과 안전 통로를 직접 설정한 거리로 계산한다.
        safeHalfWidth = Mathf.Max(0f, wallDistance - visualRadius);

        leftStartX = capturedBounds.min.x - visualRadius;
        rightStartX = capturedBounds.max.x + visualRadius;
        leftEndX = centerX - wallDistance;
        rightEndX = centerX + wallDistance;

        leftWarning = RentWarning(
            capturedBounds.min.x,
            centerX - safeHalfWidth);

        rightWarning = RentWarning(
            centerX + safeHalfWidth,
            capturedBounds.max.x);

        warningElapsed = 0f;
        elapsed = 0f;
        approaching = false;
        showingWarning = true;
    }

    private SpriteRenderer RentWarning(float minX, float maxX)
    {
        // 필드 밖으로 나가는 부분과 음수 폭을 제외한다.
        float left = Mathf.Clamp(
            minX, capturedBounds.min.x, capturedBounds.max.x);

        float right = Mathf.Clamp(
            maxX, capturedBounds.min.x, capturedBounds.max.x);

        if (right <= left)
        {
            return null;
        }

        warningPosition = new Vector3(
            (left + right) * 0.5f,
            capturedBounds.center.y,
            capturedBounds.center.z);

        warningScale = new Vector3(
            right - left,
            capturedBounds.size.y,
            1f);

        return warningPool.Rent(prepareWarning);
    }

    private void PrepareWarning(SpriteRenderer visual)
    {
        visual.transform.position = warningPosition;
        visual.transform.rotation = Quaternion.identity;
        visual.transform.localScale = warningScale;
    }

    private void HideWarnings()
    {
        if (leftWarning != null)
        {
            warningPool.Return(leftWarning);
            leftWarning = null;
        }

        if (rightWarning != null)
        {
            warningPool.Return(rightWarning);
            rightWarning = null;
        }
    }

    private void SpawnWalls()
    {
        float bottomY = capturedBounds.min.y + visualRadius;
        float topY = Mathf.Max(
            bottomY, capturedBounds.max.y - visualRadius);

        int rowCount = Mathf.Max(
            1,
            Mathf.CeilToInt((topY - bottomY) / verticalSpacing) + 1);

        for (int i = 0; i < rowCount; i++)
        {
            float y = rowCount == 1
                ? bottomY
                : Mathf.Lerp(
                    bottomY, topY, (float)i / (rowCount - 1));

            preparingPosition =
                new Vector3(leftStartX, y, capturedBounds.center.z);
            leftWall.Add(pool.Rent(prepareVisual));

            preparingPosition =
                new Vector3(rightStartX, y, capturedBounds.center.z);
            rightWall.Add(pool.Rent(prepareVisual));
        }

        elapsed = 0f;
        approaching = true;
    }

    private void PrepareVisual(SpriteRenderer visual)
    {
        // 비활성 상태에서 위치·방향·크기를 설정한다.
        visual.transform.position = preparingPosition;
        visual.transform.rotation = Quaternion.identity;
        visual.transform.localScale =
            Vector3.one * (visualRadius * 2f);
    }

    private void Update()
    {
        if (Time.timeScale <= 0f)
        {
            return;
        }

        if (showingWarning)
        {
            warningElapsed += Time.deltaTime;

            if (warningElapsed >= warningDuration)
            {
                showingWarning = false;
                HideWarnings();
                SpawnWalls();
            }

            return;
        }

        if (!approaching)
        {
            return;
        }

        elapsed += Time.deltaTime;
        float progress =
            Mathf.Clamp01(elapsed / approachDuration);

        float leftX = Mathf.Lerp(leftStartX, leftEndX, progress);
        float rightX = Mathf.Lerp(rightStartX, rightEndX, progress);

        foreach (SpriteRenderer visual in leftWall)
        {
            if (visual != null)
            {
                Vector3 position = visual.transform.position;
                position.x = leftX;
                visual.transform.position = position;
            }
        }

        foreach (SpriteRenderer visual in rightWall)
        {
            if (visual != null)
            {
                Vector3 position = visual.transform.position;
                position.x = rightX;
                visual.transform.position = position;
            }
        }

        if (progress >= 1f)
        {
            approaching = false;
        }
    }

    /// <summary>
    /// 벽 표시물을 즉시 풀에 반환합니다.
    /// 전투 종료·컴포넌트 비활성 시 호출합니다.
    /// </summary>
    public void ClearWalls()
    {
        showingWarning = false;
        warningElapsed = 0f;
        HideWarnings();
        approaching = false;

        foreach (SpriteRenderer visual in leftWall)
        {
            if (visual != null)
            {
                pool.Return(visual);
            }
        }

        foreach (SpriteRenderer visual in rightWall)
        {
            if (visual != null)
            {
                pool.Return(visual);
            }
        }

        leftWall.Clear();
        rightWall.Clear();
    }

    private void OnDisable()
    {
        ClearWalls();
    }

    private void OnDestroy()
    {
        pool?.Clear();
        warningPool?.Clear();
    }

    private void OnValidate()
    {
        warningDuration = Mathf.Max(0.01f, warningDuration);
        visualRadius = Mathf.Max(0.01f, visualRadius);
        verticalSpacing = Mathf.Max(0.01f, verticalSpacing);
        wallDistance = Mathf.Max(0f, wallDistance);
        approachDuration = Mathf.Max(0.01f, approachDuration);
        initialPoolCount = Mathf.Max(1, initialPoolCount);
    }
}
