using UnityEngine;

public static class TransformExtensions
{
    /// <summary>부모가 지정되면 연결하고 로컬 위치·회전·크기를 초기화한다.</summary>
    public static void InitTransform(this Transform trs, Transform parent = null)
    {
        if (parent != null)
            trs.SetParent(parent);

        trs.localPosition = Vector3.zero;
        trs.localRotation = Quaternion.identity;
        trs.localScale = Vector3.one;
    }
}
