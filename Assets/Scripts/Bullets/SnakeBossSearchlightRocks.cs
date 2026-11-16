using System.Collections.Generic;
using UnityEngine;

/// <summary>세트 4에서 표시용 돌을 배치하고 회수합니다.</summary>
public sealed class SnakeBossSearchlightRocks : MonoBehaviour
{
    private const int ANGLE_SAMPLES = 36;
    private const int SHADOW_SEGMENTS = 32;
    private const int MAX_ROCKS = 16;
    public const int MAX_SHADOW_VERTICES = MAX_ROCKS * SHADOW_SEGMENTS * 6;

    [SerializeField] private SpriteRenderer rockPrefab;

    // ##TEMP 돌 출구부터 적용하는 그림자 최대 길이. 월드 단위.
    [SerializeField, Min(0f)] private float shadowLength = 1f;

    // ##TEMP 돌의 개수, 배치 거리와 표시 크기.
    [SerializeField, Range(0, MAX_ROCKS)] private int rockCount = 3;
    [SerializeField, Min(0f)] private float spawnDistance = 1.5f;
    [SerializeField] private Vector2 rockSize = new Vector2(0.55f, 0.42f);

    private readonly List<SpriteRenderer> activeRocks = new List<SpriteRenderer>();
    private ComponentPool<SpriteRenderer> pool;

    /// <summary>돌을 풀에 사전 생성합니다.</summary>
    public void Initialize()
    {
        if (pool == null && rockPrefab != null && rockPrefab.sprite != null)
        {
            pool = new ComponentPool<SpriteRenderer>(
                new SearchlightRockFactory(rockPrefab, transform),
                Mathf.Clamp(rockCount, 0, MAX_ROCKS), transform);
        }
    }

    /// <summary>플레이어 주변의 필드 안에 돌을 배치합니다.</summary>
    public void SpawnAround(Vector2 playerPosition, Bounds bounds)
    {
        ReturnAll();
        int count = Mathf.Clamp(rockCount, 0, MAX_ROCKS);
        if (pool == null || count == 0)
        {
            return;
        }

        Vector2 inward = (Vector2)bounds.center - playerPosition;
        float firstAngle = inward.sqrMagnitude > 0f
            ? Mathf.Atan2(inward.y, inward.x)
            : Mathf.PI * 0.5f;
        Vector2 halfSize = rockSize * 0.5f;
        Vector3 spriteSize = rockPrefab.sprite.bounds.size;
        Vector3 scale = new Vector3(
            rockSize.x / spriteSize.x, rockSize.y / spriteSize.y, 1f);
        float fullTurn = Mathf.PI * 2f;
        float sampleStep = fullTurn / ANGLE_SAMPLES;

        for (int index = 0; index < count; index++)
        {
            float preferredAngle = firstAngle + fullTurn * index / count;

            for (int sample = 0; sample < ANGLE_SAMPLES; sample++)
            {
                // 기준 각도에서 양옆으로 번갈아 가까운 후보를 검사한다.
                int offset = (sample + 1) / 2;
                if (sample % 2 == 0)
                {
                    offset = -offset;
                }

                float angle = preferredAngle + offset * sampleStep;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 position = playerPosition + direction * spawnDistance;
                if (!CanPlace(position, halfSize, bounds))
                {
                    continue;
                }

                SpriteRenderer rock = pool.Rent(item =>
                {
                    item.transform.position = new Vector3(
                        position.x, position.y, bounds.center.z);
                    item.transform.localScale = scale;
                });
                activeRocks.Add(rock);
                break;
            }
        }
    }

    private bool CanPlace(Vector2 position, Vector2 halfSize, Bounds bounds)
    {
        if (position.x - halfSize.x < bounds.min.x
            || position.x + halfSize.x > bounds.max.x
            || position.y - halfSize.y < bounds.min.y
            || position.y + halfSize.y > bounds.max.y)
        {
            return false;
        }

        foreach (SpriteRenderer rock in activeRocks)
        {
            Vector2 offset = position - (Vector2)rock.transform.position;
            if (Mathf.Abs(offset.x) < rockSize.x && Mathf.Abs(offset.y) < rockSize.y)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>플레이어가 활성 돌의 유한 그림자 안에 있는지 반환합니다.</summary>
    public static bool BlocksSight(List<Vector4> vertices, Vector2 position)
    {
        for (int index = 0; index < vertices.Count; index += 3)
        {
            Vector2 a = vertices[index];
            Vector2 b = vertices[index + 1];
            Vector2 c = vertices[index + 2];
            if (Cross(b - a, c - a) == 0f)
            {
                continue;
            }

            float ab = Cross(b - a, position - a);
            float bc = Cross(c - b, position - b);
            float ca = Cross(a - c, position - c);
            if ((ab >= 0f && bc >= 0f && ca >= 0f)
                || (ab <= 0f && bc <= 0f && ca <= 0f))
            {
                return true;
            }
        }

        return false;
    }

    private static float Cross(Vector2 a, Vector2 b)
    {
        return a.x * b.y - a.y * b.x;
    }

    private static bool TryGetShadowGeometry(
        SpriteRenderer rock, Vector2 eye, out Vector2 localEye, out Vector2 extents)
    {
        localEye = default;
        extents = default;
        if (rock == null || rock.sprite == null)
        {
            return false;
        }

        extents = rock.sprite.bounds.extents;
        if (extents.x <= 0f || extents.y <= 0f)
        {
            return false;
        }

        localEye = ToUnitCircle(rock, eye, extents);
        return localEye.sqrMagnitude > 1f;
    }

    private static Vector2 ToUnitCircle(SpriteRenderer rock, Vector2 point, Vector2 extents)
    {
        Vector2 local = rock.transform.InverseTransformPoint(point) - rock.sprite.bounds.center;
        return new Vector2(local.x / extents.x, local.y / extents.y);
    }

    private static Vector2 ToWorldPoint(SpriteRenderer rock, Vector2 point, Vector2 extents)
    {
        return rock.transform.TransformPoint(
            rock.sprite.bounds.center + (Vector3)Vector2.Scale(point, extents));
    }

    private static bool TryGetShadowExit(Vector2 localEye, Vector2 direction, out float exitDistance)
    {
        float projection = -Vector2.Dot(localEye, direction);
        float discriminant = projection * projection - localEye.sqrMagnitude + 1f;
        if (discriminant < 0f || projection <= 0f)
        {
            exitDistance = 0f;
            return false;
        }

        exitDistance = projection + Mathf.Sqrt(discriminant);
        return true;
    }

    /// <summary>눈 하나의 유한 그림자를 월드 좌표 삼각형으로 채웁니다.</summary>
    public void BuildShadowTriangles(Vector2 eye, List<Vector4> vertices)
    {
        vertices.Clear();
        if (shadowLength <= 0f)
        {
            return;
        }

        foreach (SpriteRenderer rock in activeRocks)
        {
            if (!TryGetShadowGeometry(rock, eye, out Vector2 eyeInUnitCircle, out Vector2 extents))
            {
                continue;
            }

            Vector2 toRock = -eyeInUnitCircle;
            float centerAngle = Mathf.Atan2(toRock.y, toRock.x);
            float halfAngle = Mathf.Asin(1f / eyeInUnitCircle.magnitude);
            GetShadowRay(rock, eye, eyeInUnitCircle, extents, centerAngle - halfAngle,
                out Vector2 previousStart, out Vector2 previousEnd);

            for (int index = 1; index <= SHADOW_SEGMENTS; index++)
            {
                float fraction = (float)index / SHADOW_SEGMENTS;
                float angle = Mathf.Lerp(centerAngle - halfAngle, centerAngle + halfAngle, fraction);
                GetShadowRay(rock, eye, eyeInUnitCircle, extents, angle,
                    out Vector2 start, out Vector2 end);

                vertices.Add(previousStart);
                vertices.Add(previousEnd);
                vertices.Add(end);
                vertices.Add(previousStart);
                vertices.Add(end);
                vertices.Add(start);
                previousStart = start;
                previousEnd = end;
            }
        }
    }

    /// <summary>공유 그림자 윤곽과 빛 원 안에 들어오는 부분을 표시합니다.</summary>
    public static void DrawShadowGizmos(
        List<Vector4> vertices, Vector2 lightCenter, float lightRadius, Color color)
    {
        Color previousColor = Gizmos.color;
        Color outlineColor = color;
        outlineColor.a *= 0.35f;

        for (int index = 0; index < vertices.Count; index += 6)
        {
            Vector2 start = vertices[index];
            Vector2 end = vertices[index + 1];
            Vector2 nextEnd = vertices[index + 2];
            Vector2 nextStart = vertices[index + 5];
            int segment = index / 6 % SHADOW_SEGMENTS;

            Gizmos.color = outlineColor;
            Gizmos.DrawLine(start, nextStart);
            Gizmos.DrawLine(end, nextEnd);
            if (segment == 0)
            {
                Gizmos.DrawLine(start, end);
            }

            if (segment == SHADOW_SEGMENTS - 1)
            {
                Gizmos.DrawLine(nextStart, nextEnd);
            }

            Gizmos.color = color;
            DrawInsideCircle(start, nextStart, lightCenter, lightRadius);
            DrawInsideCircle(end, nextEnd, lightCenter, lightRadius);
            DrawInsideCircle(start, end, lightCenter, lightRadius);
            if (segment == SHADOW_SEGMENTS - 1)
            {
                DrawInsideCircle(nextStart, nextEnd, lightCenter, lightRadius);
            }
        }

        Gizmos.color = previousColor;
    }

    private void GetShadowRay(
        SpriteRenderer rock, Vector2 eye, Vector2 localEye,
        Vector2 extents, float angle, out Vector2 start, out Vector2 end)
    {
        Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        if (!TryGetShadowExit(localEye, direction, out float exitDistance))
        {
            // 접선에서는 부동소수점 오차로 판별식이 음수가 될 수 있다.
            exitDistance = -Vector2.Dot(localEye, direction);
        }

        Vector2 exitPoint = localEye + direction * exitDistance;
        start = ToWorldPoint(rock, exitPoint, extents);
        end = start + (start - eye).normalized * shadowLength;
    }

    private static void DrawInsideCircle(
        Vector2 start, Vector2 end, Vector2 center, float radius)
    {
        Vector2 segment = end - start;
        float lengthSquared = segment.sqrMagnitude;
        if (lengthSquared <= 0f)
        {
            return;
        }

        Vector2 offset = start - center;
        float projection = Vector2.Dot(offset, segment);
        float discriminant = projection * projection
            - lengthSquared * (offset.sqrMagnitude - radius * radius);
        if (discriminant < 0f)
        {
            return;
        }

        float root = Mathf.Sqrt(discriminant);
        float from = Mathf.Max(0f, (-projection - root) / lengthSquared);
        float to = Mathf.Min(1f, (-projection + root) / lengthSquared);
        if (from < to)
        {
            Gizmos.DrawLine(start + segment * from, start + segment * to);
        }
    }

    /// <summary>현재 필드 경계 밖에 걸친 돌을 반환합니다.</summary>
    public void ReturnOutside(Bounds fieldBounds)
    {
        for (int index = activeRocks.Count - 1; index >= 0; index--)
        {
            SpriteRenderer rock = activeRocks[index];
            Bounds bounds = rock.bounds;
            if (bounds.min.x >= fieldBounds.min.x && bounds.max.x <= fieldBounds.max.x
                && bounds.min.y >= fieldBounds.min.y && bounds.max.y <= fieldBounds.max.y)
            {
                continue;
            }

            pool.Return(rock);
            activeRocks.RemoveAt(index);
        }
    }

    /// <summary>배치한 돌을 모두 풀에 반환합니다.</summary>
    public void ReturnAll()
    {
        foreach (SpriteRenderer rock in activeRocks)
        {
            pool.Return(rock);
        }

        activeRocks.Clear();
    }

    private void OnDisable()
    {
        ReturnAll();
    }

    private void OnDestroy()
    {
        pool?.Clear();
    }

    private void OnValidate()
    {
        rockSize.x = Mathf.Max(0.01f, rockSize.x);
        rockSize.y = Mathf.Max(0.01f, rockSize.y);
    }
}
