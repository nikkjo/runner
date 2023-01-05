using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public LayerMask damageLayer;
    public Vector3 velocity;
    public bool destroyOnHit = false;
    public float aliveTime = 0;
    public TrailRenderer trail;
    float aliveTimer = 0;

    Collider[] overlaps;

    public int levelIndex;
    float trailTime = 0;

    private void Start()
    {      
        overlaps = new Collider[1];
        trailTime = trail.time;
    }
    public void ResetBullet()
    {
        trail.Clear();
    }
    void Update()
    {
        if(levelIndex != Game.LevelIndex)
        {
            Game.BulletPool.ReturnObject(gameObject);
            return;
        }
        transform.forward = velocity;

        Vector3 rayOrigin = transform.position;
        Vector3 newPos = rayOrigin + velocity * Game.DeltaTime;
        Vector3 dir = newPos - rayOrigin;
        float dis = dir.magnitude;
        Ray ray = new Ray(rayOrigin, dir);

        Collider hitCollider = null;
        RaycastHit hit;
        if(Physics.Raycast(ray, out hit, dis, damageLayer))
        {
            newPos = hit.point;
            hitCollider = hit.collider;
        }
        else
        {
            int colls = Physics.OverlapSphereNonAlloc(newPos, 0.05f, overlaps, damageLayer);
            if(colls > 0)
            {
                hitCollider = overlaps[0];
            }
        }
        transform.position = newPos;

        if(hitCollider != null)
        {
            if (destroyOnHit)
            {
                hitCollider.gameObject.SendMessage("TakeDamage", this);
                Game.BulletPool.ReturnObject(gameObject);
                return;
            }
        }


        aliveTimer += Game.DeltaTime;
        if(aliveTimer >= aliveTime)
        {
            Game.BulletPool.ReturnObject(gameObject);
            aliveTimer = 0;
        }

        trail.time = Mathf.Lerp(999999, trailTime, Game.GameSpeed);
    }
}
