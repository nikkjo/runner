using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPool
{
    GameObject[] objects;
    Transform parent;
    int max = 0;
    int count = 0;

    public ObjectPool(int _max, Transform _parent, System.Func<GameObject> createFunc)
    {
        max = _max;
        objects = new GameObject[max];
        parent = _parent;

        for (int i = 0; i < max; i++)
        {
            objects[i] = createFunc();
            objects[i].SetActive(false);
            objects[i].transform.SetParent(parent);
        }
        count = max;
    }
    public GameObject GetObject()
    {
        if (count == 0)
        {
            return null;
        }
        GameObject result = objects[0];
        objects[0] = objects[count - 1];
        objects[count - 1] = null;
        count--;

        result.SetActive(true);
        return result;
    }
    public void ReturnObject(GameObject go)
    {
        if (count == max)
        {
            return;
        }
        objects[count] = go;
        objects[count].transform.SetParent(parent);
        go.SetActive(false);
        count++;
    }
}
