using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IObjectFactory<T>
    where T : Component
{
    public T CreateInstance();
}
