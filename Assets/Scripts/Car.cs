using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Car : MonoBehaviour
{
    public float speed = 50;

    void Start()
    {
    }
    void Update()
    {
        transform.Translate(transform.forward * Game.DeltaTime * speed, Space.World);

        float disFromCamera = Vector3.Distance(transform.position, Camera.main.transform.position);
        if(disFromCamera > 400)
        {
            Game.CarsPool.ReturnObject(gameObject);
        }
    }
}
