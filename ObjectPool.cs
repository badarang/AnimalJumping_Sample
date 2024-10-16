using System;
using System.Collections.Generic;
using UnityEngine;
public class ObjectPool<T> where T : MonoBehaviour, IPoolable
{
    private Queue<T> pool;
    private T prefab;
    private int maxPoolSize;

    public ObjectPool(T prefab, int maxPoolSize = 1000)
    {
        if (prefab == null)
        {
            throw new ArgumentException("Prefab cannot be null.");
        }
        this.prefab = prefab;
        this.maxPoolSize = maxPoolSize;
        pool = new Queue<T>(maxPoolSize); // 초기 용량 지정
    }

    public T Get()
    {
        if (pool.Count > 0)
        {
            T obj = pool.Dequeue();
            obj.gameObject.SetActive(true);
            return obj;
        }
        else
        {
            return GameObject.Instantiate(prefab);
        }
    }

    public void Release(T obj)
    {
        if (obj != null)
        {
            obj.gameObject.SetActive(false);
            if (pool.Count < maxPoolSize)
            {
                pool.Enqueue(obj);
            }
            else
            {
                GameObject.Destroy(obj.gameObject);
            }
        }
        else
        {
            Debug.LogWarning("Tried to release a null object in the ObjectPool.");
        }
    }
}