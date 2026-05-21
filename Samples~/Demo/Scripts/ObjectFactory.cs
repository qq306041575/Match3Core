using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 对象工厂（对象池管理器）
/// 用于管理游戏对象的创建和回收，避免频繁创建销毁对象造成的性能开销
/// </summary>
[CreateAssetMenu]
public class ObjectFactory : ScriptableObject
{
    /// <summary>
    /// 类型列表（与prefabs一一对应）
    /// </summary>
    [SerializeField] List<int> _types;
    
    /// <summary>
    /// 预制体列表（与types一一对应）
    /// </summary>
    [SerializeField] List<GameObject> _prefabs;
    
    /// <summary>
    /// 对象池列表（每个类型对应一个队列）
    /// </summary>
    List<Queue<Transform>> _pools;

    /// <summary>
    /// 初始化对象池
    /// </summary>
    void Setup()
    {
        _pools = new List<Queue<Transform>>();
        for (int i = 0; i < _types.Count; i++)
        {
            _pools.Add(new Queue<Transform>());
        }
    }

    /// <summary>
    /// 获取指定类型的对象
    /// </summary>
    /// <param name="type">对象类型</param>
    /// <returns>对象的Transform组件</returns>
    public Transform Get(int type)
    {
        var index = _types.IndexOf(type);
        if (index < 0)
        {
            Debug.LogError("error type: " + type);
            return null;
        }
        if (_pools == null)
        {
            Setup();
        }

        Transform result;
        if (_pools[index].Count > 0)
        {
            result = _pools[index].Dequeue();
            result.localScale = Vector3.one;
        }
        else
        {
            result = Instantiate(_prefabs[index]).transform;
        }
        return result;
    }

    /// <summary>
    /// 回收对象到对象池
    /// </summary>
    /// <param name="type">对象类型</param>
    /// <param name="obj">要回收的对象</param>
    public void Recycle(int type, Transform obj)
    {
        var index = _types.IndexOf(type);
        if (index < 0)
        {
            return;
        }
        if (_pools == null)
        {
            Setup();
        }

        _pools[index].Enqueue(obj);
    }
}
