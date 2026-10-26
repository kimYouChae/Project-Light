using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Component를 사전 생성하고 준비 콜백 실행 후 활성화하는 로컬 풀.</summary>
public sealed class ComponentPool<T> where T : Component
{
    private readonly Queue<T> available = new Queue<T>();
    private readonly HashSet<T> instances = new HashSet<T>();
    private readonly HashSet<T> rented = new HashSet<T>();
    private readonly IObjectFactory<T> factory;
    private readonly Transform parent;
    private bool cleared;

    /// <summary>현재 풀에서 빌려간 객체 수.</summary>
    public int ActiveCount => rented.Count;

    /// <summary>비활성 객체를 초기 수량만큼 사전 생성한다.</summary>
    public ComponentPool(IObjectFactory<T> factory, int initialCount, Transform parent = null)
    {
        this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
        this.parent = parent;
        try
        {
            for (int i = 0; i < Mathf.Max(0, initialCount); i++)
                available.Enqueue(CreateInstance());
        }
        catch
        {
            Clear();
            throw;
        }
    }

    private T CreateInstance()
    {
        T instance = factory.CreateInstance();
        if (instance == null)
            throw new InvalidOperationException("팩토리가 null을 반환했습니다.");

        instance.gameObject.SetActive(false);
        instance.transform.InitTransform(parent);
        instances.Add(instance);
        return instance;
    }

    /// <summary>월드 위치와 탄 상태 등을 준비한 뒤 활성화해 대여한다.</summary>
    public T Rent(Action<T> prepare)
    {
        if (cleared)
            throw new ObjectDisposedException(nameof(ComponentPool<T>));
        if (prepare == null)
            throw new ArgumentNullException(nameof(prepare));

        T instance = null;
        while (available.Count > 0 && instance == null)
        {
            instance = available.Dequeue();
            if (instance == null)
                instances.Remove(instance);
        }

        if (instance == null)
        {
            Debug.LogWarning($"[{nameof(ComponentPool<T>)}] {typeof(T).Name} 풀 고갈: 초기 수량을 늘려주세요.");
            instance = CreateInstance();
        }

        rented.Add(instance);
        try
        {
            prepare(instance);
            instance.gameObject.SetActive(true);
            return instance;
        }
        catch
        {
            Return(instance);
            throw;
        }
    }

    /// <summary>null·다른 풀 객체·중복 반환을 거부하고 비활성 큐로 돌려보낸다.</summary>
    public void Return(T instance)
    {
        if (instance == null || !rented.Remove(instance))
        {
            Debug.LogWarning($"[{nameof(ComponentPool<T>)}] null, 다른 풀 소속 또는 이미 반환된 객체입니다.");
            return;
        }

        instance.gameObject.SetActive(false);
        available.Enqueue(instance);
    }

    /// <summary>대여 중인 객체를 포함해 전부 제거한다. 정리된 풀은 재사용하지 않는다.</summary>
    public void Clear()
    {
        if (cleared)
            return;

        cleared = true;
        foreach (T instance in instances)
        {
            if (instance == null)
                continue;
            instance.gameObject.SetActive(false);
            if (Application.isPlaying)
                UnityEngine.Object.Destroy(instance.gameObject);
            else
                UnityEngine.Object.DestroyImmediate(instance.gameObject);
        }
        available.Clear();
        rented.Clear();
        instances.Clear();
    }
}
