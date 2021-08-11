using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Poolable : MonoBehaviour
{
    protected ObjectPool Pool;

    public virtual void Create(ObjectPool _pool)
    {
        this.Pool = _pool;
        gameObject.SetActive(false);
    }

    public virtual void Push()
    {
        Pool.Push(this);
    }
}
