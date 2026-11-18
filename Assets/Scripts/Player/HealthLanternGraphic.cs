using UnityEngine;

/// <summary>
/// 전달받은 칸 수와 켜진 칸 수를 임시 등불 도형으로 표시합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class HealthLanternGraphic : UnityEngine.UI.MaskableGraphic
{
    // ##TEMP 인스펙터에서 모양과 배치를 확인하기 위한 값.
    [Header("Preview ##TEMP")]
    [SerializeField, Min(0)] private int previewSlotCount = 10;
    [SerializeField, Min(0)] private int previewLitCount = 7;

    // ##TEMP 플레이 확인 후 조정할 등불 크기와 간격.
    [Header("Layout ##TEMP")]
    [SerializeField, Min(1f)] private float maxSlotSize = 48f;
    [SerializeField, Min(1f)] private float minSlotSize = 12f;
    [SerializeField, Min(0f)] private float spacingRatio = 0.15f;

    // ##TEMP 켜진 등불과 꺼진 등불의 색.
    [Header("Colors ##TEMP")]
    [SerializeField] private Color litBodyColor =
        new Color(0.75f, 0.45f, 0.12f, 1f);
    [SerializeField] private Color flameColor =
        new Color(1f, 0.85f, 0.25f, 1f);
    [SerializeField] private Color unlitBodyColor =
        new Color(0.32f, 0.32f, 0.35f, 1f);

    private int slotCount;
    private int litCount;

    /// <summary>
    /// 전체 칸과 켜진 칸을 표시합니다. 체력 표시를 담당하는 쪽에서 호출합니다.
    /// </summary>
    public void SetSlots(int total, int lit)
    {
        slotCount = Mathf.Max(0, total);
        litCount = Mathf.Clamp(lit, 0, slotCount);
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
    {
        mesh.Clear();

        if (slotCount == 0)
            return;

        Rect bounds = rectTransform.rect;
        float gapRatio = Mathf.Max(0f, spacingRatio);
        float availableSize = bounds.height
            / (slotCount + Mathf.Max(0, slotCount - 1) * gapRatio);

        // 공간이 최소 크기보다 작으면 화면 안에 들어가도록 더 축소한다.
        float preferredMin = Mathf.Max(1f, minSlotSize);
        float preferredMax = Mathf.Max(preferredMin, maxSlotSize);
        float size = Mathf.Min(preferredMax, availableSize, bounds.width);
        if (size <= 0f)
            return;

        float gap = size * gapRatio;
        float centerX = bounds.xMax - size * 0.5f;

        for (int i = 0; i < slotCount; i++)
        {
            float bottom = bounds.yMin + i * (size + gap);
            bool isLit = i < litCount;

            // ##TEMP 몸통은 사각형, 켜진 불꽃은 작은 사각형.
            AddRect(mesh, centerX - size * 0.35f, bottom,
                size * 0.7f, size * 0.6f,
                isLit ? litBodyColor : unlitBodyColor);

            if (isLit)
            {
                AddRect(mesh, centerX - size * 0.15f,
                    bottom + size * 0.6f, size * 0.3f,
                    size * 0.4f, flameColor);
            }
        }
    }

    private void AddRect(
        UnityEngine.UI.VertexHelper mesh,
        float x, float y, float width, float height, Color tint)
    {
        int first = mesh.currentVertCount;
        Color32 vertexColor = tint * color;
        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = vertexColor;

        vertex.position = new Vector3(x, y);
        mesh.AddVert(vertex);
        vertex.position = new Vector3(x, y + height);
        mesh.AddVert(vertex);
        vertex.position = new Vector3(x + width, y + height);
        mesh.AddVert(vertex);
        vertex.position = new Vector3(x + width, y);
        mesh.AddVert(vertex);

        mesh.AddTriangle(first, first + 1, first + 2);
        mesh.AddTriangle(first, first + 2, first + 3);
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();

        // ##TEMP 에디터(Play 아님)에서만 인스펙터 변경을 미리보기에 반영. Play 중에는 PlayerHealthView가 실제 체력을 표시한다.
        if (!Application.isPlaying)
        {
            SetSlots(previewSlotCount, previewLitCount);
        }
    }
#endif
}
