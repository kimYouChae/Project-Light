using UnityEngine;

/// <summary>
/// 씬에 배치한 컴포넌트의 단일 인스턴스를 관리합니다.
/// </summary>
public abstract class Singleton<T> : MonoBehaviour
    where T : Singleton<T>
{
    private static T instance;

    /// <summary>
    /// 현재 인스턴스를 반환합니다.
    /// 씬에 배치되지 않았거나 초기화 전이면 null입니다.
    /// </summary>
    public static T Instance => instance;

    protected virtual void Awake()
    {
        T current = (T)this;

        if (instance != null && instance != current)
        {
            Destroy(gameObject);
            return;
        }

        instance = current;
        OnSingletonAwake();
    }

    /// <summary>
    /// 인스턴스 등록 후 초기화합니다. 부모 Awake가 호출합니다.
    /// </summary>
    protected abstract void OnSingletonAwake();

    protected virtual void OnDestroy()
    {
        if (instance == (T)this)
        {
            instance = null;
        }
    }
}
