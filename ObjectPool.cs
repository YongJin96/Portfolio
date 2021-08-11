using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    [SerializeField]
    private Poolable PoolObject;
    [SerializeField]
    private int AllocateCount;

    private Stack<Poolable> PoolStack = new Stack<Poolable>();

    private void Start()
    {
        Allocate();
    }

    public void Allocate()
    {
        for (int i = 0; i < AllocateCount; i++)
        {
            Poolable allocateObject = Instantiate(PoolObject, this.gameObject.transform);
            allocateObject.Create(this);
            PoolStack.Push(allocateObject);
        }
    }

    public GameObject Pop()
    {
        Poolable poolObject = PoolStack.Pop();
        poolObject.gameObject.SetActive(true);
        return poolObject.gameObject;
    }

    public void Push(Poolable _poolObject)
    {
        _poolObject.gameObject.SetActive(false);
        PoolStack.Push(_poolObject);
    }
}
