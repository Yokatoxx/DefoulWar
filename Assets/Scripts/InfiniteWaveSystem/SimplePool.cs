using System.Collections.Generic;
using UnityEngine;

public class SimplePool
{
    private readonly GameObject _prefab;
    private readonly Transform _root;
    private readonly Stack<GameObject> _stack = new Stack<GameObject>();
    private int _active;

    public int ActiveCount => _active;
    public int InactiveCount => _stack.Count;

    public SimplePool(GameObject prefab, string rootName, Transform parent)
    {
        _prefab = prefab;
        _root = new GameObject(rootName).transform;
        _root.SetParent(parent, false);
    }

    public void Prewarm(int count)
    {
        if (_prefab == null || count <= 0) return;
        for (int i = 0; i < count; i++)
        {
            var go = Object.Instantiate(_prefab, _root);
            go.SetActive(false);
            EnsurePooledObject(go);
            _stack.Push(go);
        }
    }

    public GameObject Spawn(Vector3 position, Quaternion rotation)
    {
        if (_prefab == null) return null;

        GameObject go = _stack.Count > 0 ? _stack.Pop() : Object.Instantiate(_prefab, _root);
        EnsurePooledObject(go);
        go.transform.SetPositionAndRotation(position, rotation);
        go.SetActive(true);
        _active++;
        return go;
    }

    public void Despawn(GameObject go)
    {
        if (go == null) return;
        go.SetActive(false);
        go.transform.SetParent(_root, false);
        _stack.Push(go);
        if (_active > 0) _active--;
        var spawnable = go.GetComponent<ISpawnableEnemy>();
        if (spawnable != null)
        {
            spawnable.OnDespawn();
        }
    }

    private void EnsurePooledObject(GameObject go)
    {
        var po = go.GetComponent<PooledObject>();
        if (po == null) po = go.AddComponent<PooledObject>();
        po.SetOwner(this);
    }
}