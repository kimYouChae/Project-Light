using System.Collections.Generic;
using UnityEngine;

/// <summary>활성 적탄을 검사하고 탄마다 헤일로 가장자리 경고를 풀링합니다.</summary>
[DisallowMultipleComponent]
public sealed class BushBulletWarningController : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private BulletRegistry bulletRegistry;
    [SerializeField] private PlayerHealth player;
    [SerializeField] private PlayerLight playerLight;
    [SerializeField] private BushBulletWarning warningPrefab;
    [SerializeField] private Transform warningPoolParent;

    // ##TEMP 보이는 빛 가장자리의 추정 비율입니다. 플레이 확인 후 조정합니다. 경고 표시에만 쓰며 헤일로 판정은 바꾸지 않습니다.
    [SerializeField, Range(0.01f, 1f)] private float visibleHaloRadiusRatio = 0.85f;

    [Header("풀")]
    // ##TEMP 사전 생성 수량입니다. 실제 최대 동시 경고 수에 맞춰 조정합니다.
    [SerializeField, Min(1)] private int initialCount = 256;

    private ComponentPool<BushBulletWarning> pool;
    private readonly Dictionary<TestBullet, BushBulletWarning> warnings =
        new Dictionary<TestBullet, BushBulletWarning>();
    private readonly HashSet<TestBullet> currentTargets =
        new HashSet<TestBullet>();
    private readonly List<TestBullet> releaseTargets =
        new List<TestBullet>();

    private void Awake()
    {
        if (bulletRegistry == null
            || player == null
            || playerLight == null
            || warningPrefab == null
            || warningPrefab.gameObject.activeSelf
            || warningPoolParent == null
            || initialCount < 1)
        {
            PlayLog.Write(
                "Bush",
                "수풀 탄 경고의 참조·비활성 프리팹·초기 수량을 확인해주세요.",
                LogType.Warning);
            enabled = false;
            return;
        }

        pool = new ComponentPool<BushBulletWarning>(
            new BushBulletWarningFactory(warningPrefab, warningPoolParent),
            initialCount,
            warningPoolParent);
    }

    private void LateUpdate()
    {
        if (pool == null)
            return;

        currentTargets.Clear();

        if (bulletRegistry != null
            && player != null
            && playerLight != null
            && player.isActiveAndEnabled
            && playerLight.isActiveAndEnabled
            && !player.IsDead
            && playerLight.HaloRadius > 0f)
        {
            Vector3 center = playerLight.transform.position;
            float haloRadius = playerLight.HaloRadius
                * Mathf.Clamp(visibleHaloRadiusRatio, 0.01f, 1f);

            foreach (TestBullet bullet in bulletRegistry.ActiveBullets)
            {
                if (bullet == null || !bullet.CanShowBushWarning(player))
                    continue;

                Vector2 offset =
                    (Vector2)bullet.transform.position - (Vector2)center;

                // 탄 반경이 아닌 탄 중심으로 헤일로 진입을 판단합니다.
                if (offset.sqrMagnitude <= haloRadius * haloRadius)
                    continue;

                Vector2 outwardDirection = offset.normalized;
                Vector3 tipPosition = center + new Vector3(
                    outwardDirection.x * haloRadius,
                    outwardDirection.y * haloRadius,
                    0f);

                currentTargets.Add(bullet);
                UpdateWarning(
                    bullet, tipPosition, outwardDirection, offset.magnitude - haloRadius);
            }
        }

        ReleaseUnusedWarnings();
    }

    private void UpdateWarning(
        TestBullet bullet,
        Vector3 tipPosition,
        Vector2 outwardDirection,
        float distanceToHalo)
    {
        if (warnings.TryGetValue(bullet, out BushBulletWarning warning))
        {
            warning.SetPose(
                tipPosition, outwardDirection, bullet.Radius, distanceToHalo);
            return;
        }

        // 크기·위치·방향을 설정한 뒤 풀에서 활성화합니다.
        warning = pool.Rent(
            instance => instance.Prepare(
                tipPosition, outwardDirection, bullet.Radius, distanceToHalo));
        warnings.Add(bullet, warning);
    }

    private void ReleaseUnusedWarnings()
    {
        // 순회 중 Dictionary를 수정하지 않고 반환 대상을 먼저 모읍니다.
        releaseTargets.Clear();

        foreach (KeyValuePair<TestBullet, BushBulletWarning> pair in warnings)
        {
            if (!currentTargets.Contains(pair.Key))
                releaseTargets.Add(pair.Key);
        }

        foreach (TestBullet bullet in releaseTargets)
        {
            BushBulletWarning warning = warnings[bullet];
            warnings.Remove(bullet);

            if (warning != null)
                pool.Return(warning);
        }

        releaseTargets.Clear();
    }

    private void ReleaseAllWarnings()
    {
        if (pool != null)
        {
            foreach (BushBulletWarning warning in warnings.Values)
            {
                if (warning != null)
                    pool.Return(warning);
            }
        }

        warnings.Clear();
        currentTargets.Clear();
        releaseTargets.Clear();
    }

    private void OnDisable()
    {
        ReleaseAllWarnings();
    }

    private void OnDestroy()
    {
        ReleaseAllWarnings();
        pool?.Clear();
        pool = null;
    }
}
