using System.Collections.Generic;
using UnityEngine;

/// <summary>세트 4에서 표시용 돌을 배치하고 회수합니다.</summary>
public sealed class SnakeBossSearchlightRocks : MonoBehaviour
{
    private const int ANGLE_SAMPLES = 36;

    [SerializeField] private SpriteRenderer rockPrefab;

    // ##TEMP 돌의 개수, 배치 거리와 표시 크기.
    [SerializeField, Min(0)] private int rockCount = 3;
    [SerializeField, Min(0f)] private float spawnDistance = 1.5f;
    [SerializeField] private Vector2 rockSize = new Vector2(0.55f, 0.42f);

    private readonly List<SpriteRenderer> activeRocks = new List<SpriteRenderer>();
    private SpriteRenderer field;
    private ComponentPool<SpriteRenderer> pool;

    /// <summary>배치에 사용할 필드를 연결하고 돌을 사전 생성합니다.</summary>
    public void Initialize(SpriteRenderer field)
    {
        this.field = field;
        if (pool == null && rockPrefab != null && rockPrefab.sprite != null)
        {
            pool = new ComponentPool<SpriteRenderer>(
                new SearchlightRockFactory(rockPrefab, transform), rockCount, transform);
        }
    }

    /// <summary>플레이어 주변의 필드 안에 돌을 배치합니다.</summary>
    public void SpawnAround(Vector2 playerPosition)
    {
        ReturnAll();
        if (field == null || pool == null || rockCount <= 0)
        {
            return;
        }

        Bounds bounds = field.bounds;
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

        for (int index = 0; index < rockCount; index++)
        {
            float preferredAngle = firstAngle + fullTurn * index / rockCount;

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

    /// <summary>눈에서 플레이어까지의 선분이 활성 돌을 지나는지 반환합니다.</summary>
    public bool BlocksSight(Vector2 eyePosition, Vector2 playerPosition)
    {
        foreach (SpriteRenderer rock in activeRocks)
        {
            Bounds spriteBounds = rock.sprite.bounds;
            Vector2 extents = spriteBounds.extents;
            Vector2 start = rock.transform.InverseTransformPoint(eyePosition)
                - spriteBounds.center;
            Vector2 end = rock.transform.InverseTransformPoint(playerPosition)
                - spriteBounds.center;

            // 타원을 단위원으로 바꾸고 선분에서 원점에 가장 가까운 점을 구한다.
            start = new Vector2(start.x / extents.x, start.y / extents.y);
            end = new Vector2(end.x / extents.x, end.y / extents.y);
            Vector2 segment = end - start;
            float lengthSquared = segment.sqrMagnitude;
            float progress = lengthSquared > 0f
                ? Mathf.Clamp01(-Vector2.Dot(start, segment) / lengthSquared)
                : 0f;

            if ((start + segment * progress).sqrMagnitude <= 1f)
            {
                return true;
            }
        }

        return false;
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
