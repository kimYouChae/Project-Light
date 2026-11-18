using System.Collections.Generic;
using UnityEngine;

/// <summary>사각 수풀 영역과 적탄의 반경 접촉을 계산합니다.</summary>
[DisallowMultipleComponent]
public sealed class BushArea : MonoBehaviour
{
    // ##TEMP 스프라이트 사각형 전체를 판정 영역으로 사용. 수풀 1-6에서 재검토
    [SerializeField] private SpriteRenderer areaRenderer;

    private static readonly List<BushArea> activeAreas = new List<BushArea>();

    private Bounds scrollStartBounds;
    private float scrollDistance;
    private float distanceToRecycle;
    private float loopHeight;
    private int scrollFrame = -1;

    private void OnEnable()
    {
        if (areaRenderer == null)
            areaRenderer = GetComponent<SpriteRenderer>();

        scrollFrame = -1;
        activeAreas.Add(this);
    }

    private void OnDisable()
    {
        activeAreas.Remove(this);
        scrollFrame = -1;
    }

    /// <summary>스크롤 관리자가 위치 변경 전에 이번 이동 구간을 기록합니다.</summary>
    public void RecordScroll(float distance, float recycleCenterY, float height)
    {
        if (areaRenderer == null)
            return;

        scrollStartBounds = areaRenderer.bounds;
        scrollDistance = distance;
        distanceToRecycle = Mathf.Max(0f, transform.position.y - recycleCenterY);
        loopHeight = height;
        scrollFrame = Time.frameCount;
    }

    /// <summary>적탄의 생성 준비에서 현재 수풀과의 겹침을 검사합니다.</summary>
    public static bool OverlapsAny(Vector2 position, float radius)
    {
        foreach (BushArea area in activeAreas)
        {
            if (area.areaRenderer == null)
                continue;

            Bounds bounds = area.areaRenderer.bounds;
            Vector2 relative = position - (Vector2)bounds.center;
            if (TouchesRectangle(relative, relative, bounds.extents, radius))
                return true;
        }

        return false;
    }

    /// <summary>적탄의 이동 갱신에서 수풀과의 상대 이동 접촉을 검사합니다.</summary>
    public static bool CrossesAny(
        Vector2 start, Vector2 end, float radius, bool includeScroll)
    {
        foreach (BushArea area in activeAreas)
        {
            if (area.areaRenderer != null
                && area.Crosses(start, end, radius, includeScroll))
            {
                return true;
            }
        }

        return false;
    }

    private bool Crosses(
        Vector2 start, Vector2 end, float radius, bool includeScroll)
    {
        if (!includeScroll || scrollFrame != Time.frameCount
            || scrollDistance <= 0f || loopHeight <= 0f)
        {
            Bounds bounds = areaRenderer.bounds;
            return TouchesRectangle(
                start - (Vector2)bounds.center,
                end - (Vector2)bounds.center,
                bounds.extents,
                radius);
        }

        // 재배치 전후의 실제 하강 구간만 검사합니다.
        // 화면 아래에서 위로 순간 이동하는 구간은 검사하지 않습니다.
        Vector2 center = scrollStartBounds.center;
        Vector2 halfSize = scrollStartBounds.extents;
        float consumed = 0f;
        float toRecycle = distanceToRecycle;

        while (consumed < scrollDistance)
        {
            float step = Mathf.Min(scrollDistance - consumed, toRecycle);
            float startFraction = consumed / scrollDistance;
            float endFraction = (consumed + step) / scrollDistance;
            Vector2 nextCenter = center + Vector2.down * step;

            if (TouchesRectangle(
                Vector2.Lerp(start, end, startFraction) - center,
                Vector2.Lerp(start, end, endFraction) - nextCenter,
                halfSize,
                radius))
            {
                return true;
            }

            consumed += step;
            if (consumed >= scrollDistance)
                return false;

            center = nextCenter + Vector2.up * loopHeight;
            toRecycle = loopHeight;
        }

        return false;
    }

    private static bool TouchesRectangle(
        Vector2 start, Vector2 end, Vector2 halfSize, float radius)
    {
        // 사각형의 변 주변과 네 모서리의 원을 합쳐 원·사각형 접촉을 구합니다.
        if (IntersectsRectangle(
                start, end, halfSize + new Vector2(radius, 0f))
            || IntersectsRectangle(
                start, end, halfSize + new Vector2(0f, radius)))
        {
            return true;
        }

        float radiusSquared = radius * radius;
        return DistanceSquaredToSegment(
                   new Vector2(-halfSize.x, -halfSize.y), start, end)
                   <= radiusSquared
            || DistanceSquaredToSegment(
                   new Vector2(-halfSize.x, halfSize.y), start, end)
                   <= radiusSquared
            || DistanceSquaredToSegment(
                   new Vector2(halfSize.x, -halfSize.y), start, end)
                   <= radiusSquared
            || DistanceSquaredToSegment(
                   new Vector2(halfSize.x, halfSize.y), start, end)
                   <= radiusSquared;
    }

    private static bool IntersectsRectangle(
        Vector2 start, Vector2 end, Vector2 halfSize)
    {
        float enter = 0f;
        float exit = 1f;
        Vector2 delta = end - start;

        return ClipAxis(start.x, delta.x, halfSize.x, ref enter, ref exit)
            && ClipAxis(start.y, delta.y, halfSize.y, ref enter, ref exit);
    }

    private static bool ClipAxis(
        float start, float delta, float halfSize, ref float enter, ref float exit)
    {
        if (delta == 0f)
            return start >= -halfSize && start <= halfSize;

        float first = (-halfSize - start) / delta;
        float second = (halfSize - start) / delta;
        enter = Mathf.Max(enter, Mathf.Min(first, second));
        exit = Mathf.Min(exit, Mathf.Max(first, second));
        return enter <= exit;
    }

    private static float DistanceSquaredToSegment(
        Vector2 point, Vector2 start, Vector2 end)
    {
        Vector2 delta = end - start;
        float lengthSquared = delta.sqrMagnitude;
        float fraction = lengthSquared > 0f
            ? Mathf.Clamp01(Vector2.Dot(point - start, delta) / lengthSquared)
            : 0f;

        return (point - (start + delta * fraction)).sqrMagnitude;
    }
}
