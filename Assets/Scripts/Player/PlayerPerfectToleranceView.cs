using UnityEngine;

/// <summary>PERFECT 추가 허용 폭의 안쪽·바깥쪽 기준선을 표시한다.</summary>
[ExecuteAlways]
public sealed class PlayerPerfectToleranceView : MonoBehaviour
{
    [SerializeField] private PlayerLight playerLight;
    [SerializeField] private PlayerPerfectDodge perfectDodge;
    [SerializeField] private LineRenderer innerLine;
    [SerializeField] private LineRenderer outerLine;

    // ##TEMP 허용 폭 표시선의 굵기·색·분할 수.
    [SerializeField, Min(0.001f)] private float lineWidth = 0.015f;
    [SerializeField] private Color lineColor = Color.yellow;
    [SerializeField, Range(16, 256)] private int segmentCount = 96;

    private void LateUpdate()
    {
        if (playerLight == null || perfectDodge == null)
            return;
        float tolerance = perfectDodge.PerfectTolerance;
        float innerRadius = Mathf.Max(0f, playerLight.CoreRadius - tolerance);
        float outerRadius = playerLight.CoreRadius + tolerance;
        DrawCircle(innerLine, innerRadius);
        DrawCircle(outerLine, outerRadius);
    }

    private void DrawCircle(LineRenderer line, float radius)
    {
        if (line == null)
            return;
        line.enabled = radius > 0f;
        if (!line.enabled)
            return;
        line.useWorldSpace = true;
        line.loop = true;
        line.positionCount = segmentCount;
        line.widthMultiplier = lineWidth;
        line.startColor = lineColor;
        line.endColor = lineColor;
        for (int i = 0; i < segmentCount; i++)
        {
            float angle = i * Mathf.PI * 2f / segmentCount;
            Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
            line.SetPosition(i, transform.position + offset);
        }
    }

    private void OnDisable()
    {
        if (innerLine != null)
            innerLine.enabled = false;
        if (outerLine != null)
            outerLine.enabled = false;
    }

    private void OnValidate()
    {
        lineWidth = Mathf.Max(0.001f, lineWidth);
        segmentCount = Mathf.Clamp(segmentCount, 16, 256);
    }
}
