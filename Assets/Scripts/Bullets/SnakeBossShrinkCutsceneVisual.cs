using UnityEngine;

/// <summary>필드 축소 경고 동안 임시 몸통 이동과 화면 흔들림을 표시합니다.</summary>
public sealed class SnakeBossShrinkCutsceneVisual : MonoBehaviour
{
    [SerializeField] private Transform[] bodySegments;
    // ##TEMP 프로토타입 컷씬에서 사용하는 월드 단위 수치.
    [SerializeField, Min(0f)] private float shakeDistance = 0.08f;
    [SerializeField, Min(0f)] private float bodySpacing = 0.65f;

    private Vector3[] bodyPositions;
    private Transform cameraTransform;
    private Vector3 cameraPosition;
    private bool isShowing;

    public void Begin()
    {
        if (isShowing)
            return;

        isShowing = true;
        Camera camera = Camera.main;
        if (camera != null)
        {
            cameraTransform = camera.transform;
            cameraPosition = cameraTransform.localPosition;
        }

        bodyPositions ??= new Vector3[bodySegments.Length];
        for (int index = 0; index < bodySegments.Length; index++)
        {
            if (bodySegments[index] != null)
                bodyPositions[index] = bodySegments[index].localPosition;
        }
    }

    public void Show(Bounds bounds, float progress)
    {
        if (!isShowing)
            return;

        if (cameraTransform != null)
        {
            // ##TEMP 컷씬 진행률에 맞춘 프로토타입 흔들림.
            float phase = progress * Mathf.PI * 2f;
            Vector3 offset = new Vector3(Mathf.Sin(phase * 19f), Mathf.Sin(phase * 23f), 0f);
            cameraTransform.localPosition = cameraPosition + offset * (shakeDistance * (1f - progress));
        }

        float perimeter = 2f * (bounds.size.x + bounds.size.y);
        for (int index = 0; index < bodySegments.Length; index++)
        {
            Transform segment = bodySegments[index];
            if (segment == null)
                continue;

            float distance = bounds.extents.x + perimeter * progress - index * bodySpacing;
            Vector2 edge = GetEdgePosition(bounds, Mathf.Repeat(distance, perimeter));
            Vector3 start = segment.parent.TransformPoint(bodyPositions[index]);
            Vector3 end = new Vector3(edge.x, edge.y, start.z);
            // ##TEMP 시작과 종료 때 기본 몸통 자세로 부드럽게 연결한다.
            float blend = Mathf.Clamp01(Mathf.Min(progress, 1f - progress) / 0.12f);
            segment.position = Vector3.Lerp(start, end, blend);
        }
    }

    private static Vector2 GetEdgePosition(Bounds bounds, float distance)
    {
        float width = bounds.size.x;
        float height = bounds.size.y;
        if (distance < width)
            return new Vector2(bounds.min.x + distance, bounds.max.y);

        distance -= width;
        if (distance < height)
            return new Vector2(bounds.max.x, bounds.max.y - distance);

        distance -= height;
        if (distance < width)
            return new Vector2(bounds.max.x - distance, bounds.min.y);

        return new Vector2(bounds.min.x, bounds.min.y + distance - width);
    }

    public void Restore()
    {
        if (!isShowing)
            return;

        isShowing = false;
        if (cameraTransform != null)
            cameraTransform.localPosition = cameraPosition;
        cameraTransform = null;

        for (int index = 0; index < bodySegments.Length; index++)
        {
            if (bodySegments[index] != null)
                bodySegments[index].localPosition = bodyPositions[index];
        }
    }

    private void OnDisable()
    {
        Restore();
    }
}
