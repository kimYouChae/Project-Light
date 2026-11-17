using UnityEngine;

/// <summary>코어 판정선에 가까운 탄과 실제 접촉 방향을 표시합니다.</summary>
public sealed class PlayerCoreBoundaryView : MonoBehaviour
{
    [SerializeField] private BulletRegistry bulletRegistry;
    [SerializeField] private MeshFilter coreBoundary;
    [SerializeField, Min(0.001f)] private float lineWidth = 0.035f;
    [SerializeField, Range(16, 256)] private int segmentCount = 96;
    [SerializeField] private Color coreColor = new Color(1f, 0.87f, 0.45f, 1f);

    // ##TEMP 판정선 근접과 접촉의 표시 수치.
    [SerializeField, Min(0.01f)] private float proximityDistance = 0.5f;
    [SerializeField, Min(0.001f)] private float maxLineWidth = 0.07f;
    [SerializeField] private Color maxBrightnessColor = new Color(1f, 0.98f, 0.85f, 1f);
    [SerializeField, Range(1f, 360f)] private float reactionArcDegrees = 30f;
    [SerializeField, Range(0f, 1f)] private float proximityStrengthLimit = 0.75f;
    [SerializeField, Min(0.01f)] private float contactFlashDuration = 0.08f;
    [SerializeField] private Color successRangeColor = new Color(1f, 0.25f, 0.20f, 1f);

    private PlayerHealth player;
    private PlayerLight playerLight;
    private PlayerPerfectDodge perfectDodge;
    private Mesh mesh;
    private Vector2[] directions;
    private Vector3[] vertices;
    private Color[] colors;
    private float[] closestDistances;
    private float[] proximityStrengths;
    private float[] successColorWeights;
    private float[] recentContactWeights;
    private float[] flashWeights;
    private double[] flashEndsAt;

    private float ArcEdgeDot => Mathf.Cos(reactionArcDegrees * 0.5f * Mathf.Deg2Rad);

    private void Awake()
    {
        player = GetComponent<PlayerHealth>();
        playerLight = GetComponent<PlayerLight>();
        perfectDodge = GetComponent<PlayerPerfectDodge>();
        if (coreBoundary == null || player == null || playerLight == null)
            return;

        int count = segmentCount;
        directions = new Vector2[count];
        vertices = new Vector3[count * 2];
        colors = new Color[count * 2];
        closestDistances = new float[count];
        proximityStrengths = new float[count];
        successColorWeights = new float[count];
        recentContactWeights = new float[count];
        flashWeights = new float[count];
        flashEndsAt = new double[count];
        int[] triangles = new int[count * 6];
        for (int index = 0; index < count; index++)
        {
            float angle = index * (2f * Mathf.PI / count);
            directions[index] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            int inner = index * 2;
            int nextInner = ((index + 1) % count) * 2;
            int triangle = index * 6;
            triangles[triangle] = inner;
            triangles[triangle + 1] = nextInner;
            triangles[triangle + 2] = inner + 1;
            triangles[triangle + 3] = inner + 1;
            triangles[triangle + 4] = nextInner;
            triangles[triangle + 5] = nextInner + 1;
        }

        mesh = new Mesh { name = "Player Core Boundary" };
        mesh.MarkDynamic();
        mesh.vertices = vertices;
        mesh.uv = new Vector2[vertices.Length];
        mesh.triangles = triangles;
        coreBoundary.sharedMesh = mesh;
    }

    /// <summary>일반 탄이 실제로 판정선에 닿은 방향에 번쩍임을 등록합니다.</summary>
    public void NotifyBulletContact(Vector2 direction)
    {
        if (!isActiveAndEnabled || mesh == null || player.IsDead)
            return;

        float arcEdgeDot = ArcEdgeDot;
        double now = Time.timeAsDouble;
        for (int index = 0; index < directions.Length; index++)
        {
            float weight = Mathf.InverseLerp(arcEdgeDot, 1f, Vector2.Dot(directions[index], direction));
            float activeWeight = flashWeights[index]
                * Mathf.Clamp01((float)((flashEndsAt[index] - now) / contactFlashDuration));
            if (weight <= activeWeight)
                continue;

            flashWeights[index] = weight;
            flashEndsAt[index] = now + contactFlashDuration;
        }
    }

    private void LateUpdate()
    {
        if (mesh == null)
            return;

        Vector3 center = playerLight.transform.position;
        float coreRadius = playerLight.CoreRadius;
        float arcEdgeDot = ArcEdgeDot;
        double now = Time.timeAsDouble;
        for (int index = 0; index < directions.Length; index++)
        {
            closestDistances[index] = float.PositiveInfinity;
            proximityStrengths[index] = 0f;
            successColorWeights[index] = 0f;
            recentContactWeights[index] = 0f;
        }

        if (bulletRegistry != null && !player.IsDead)
        {
            foreach (TestBullet bullet in bulletRegistry.ActiveBullets)
            {
                if (bullet != null && bullet.CanReactToCoreProximity(player))
                {
                    ApplyBulletProximity(bullet, (Vector2)bullet.transform.position - (Vector2)center,
                        coreRadius, arcEdgeDot, now);
                }
            }
        }

        var worldToLocal = coreBoundary.transform.worldToLocalMatrix;
        for (int index = 0; index < directions.Length; index++)
        {
            float flash = player.IsDead ? 0f : flashWeights[index]
                * Mathf.Clamp01((float)((flashEndsAt[index] - now) / contactFlashDuration));
            float strength = Mathf.Max(proximityStrengths[index] * proximityStrengthLimit, flash);
            float halfWidth = Mathf.Lerp(lineWidth, maxLineWidth, strength) * 0.5f;
            Vector3 direction = directions[index];
            vertices[index * 2] = worldToLocal.MultiplyPoint3x4(
                center + direction * Mathf.Max(0f, coreRadius - halfWidth));
            vertices[index * 2 + 1] = worldToLocal.MultiplyPoint3x4(
                center + direction * (coreRadius + halfWidth));
            Color normalColor = Color.Lerp(coreColor, maxBrightnessColor, strength);
            Color red = successRangeColor;
            float redBrightness = Mathf.Lerp(0.75f, 1f, flash);
            red.r *= redBrightness;
            red.g *= redBrightness;
            red.b *= redBrightness;
            float redWeight = recentContactWeights[index] > 0f
                ? recentContactWeights[index] : successColorWeights[index];
            colors[index * 2] = colors[index * 2 + 1] = Color.Lerp(normalColor, red, redWeight);
        }

        mesh.vertices = vertices;
        mesh.colors = colors;
    }

    private void ApplyBulletProximity(TestBullet bullet, Vector2 offset,
        float coreRadius, float arcEdgeDot, double now)
    {
        if (perfectDodge != null && bullet.TryGetLastCoreCrossing(player, out double crossedAt))
        {
            double elapsed = now - crossedAt;
            if (elapsed >= 0d && elapsed <= perfectDodge.PerfectGraceSeconds)
                ApplyRecentContactColor(bullet.LastCoreContactDirection, arcEdgeDot);
        }

        float distanceToCore = offset.magnitude - coreRadius;
        if (distanceToCore < 0f || distanceToCore >= proximityDistance)
            return;

        Vector2 direction = offset.normalized;
        float proximity = 1f - distanceToCore / proximityDistance;
        float closingSpeed = bullet.CoreClosingSpeed;
        bool showSuccessColor = perfectDodge != null && closingSpeed > 0f
            && distanceToCore / closingSpeed <= perfectDodge.PerfectGraceSeconds;
        for (int index = 0; index < directions.Length; index++)
        {
            float alignment = Vector2.Dot(directions[index], direction);
            if (alignment <= arcEdgeDot || distanceToCore >= closestDistances[index])
                continue;

            closestDistances[index] = distanceToCore;
            float angularWeight = Mathf.InverseLerp(arcEdgeDot, 1f, alignment);
            proximityStrengths[index] = proximity * angularWeight;
            successColorWeights[index] = showSuccessColor ? angularWeight : 0f;
        }
    }

    private void ApplyRecentContactColor(Vector2 direction, float arcEdgeDot)
    {
        for (int index = 0; index < directions.Length; index++)
        {
            float weight = Mathf.InverseLerp(arcEdgeDot, 1f, Vector2.Dot(directions[index], direction));
            recentContactWeights[index] = Mathf.Max(recentContactWeights[index], weight);
        }
    }

    private void OnDestroy()
    {
        if (mesh != null)
            Destroy(mesh);
    }
}
