using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct ShootPoint
{
    public Transform transform;
    public Vector3 offset;

    public Vector3 Point
    {
        get
        {
            return transform.position + transform.right * offset.x + transform.up * offset.y + transform.forward * offset.z;
        }
    }
}
public class Turret : MonoBehaviour
{
    public Collider coll;
    public LayerMask attackMask;
    public LayerMask raycastMask;

    public AudioClip[] gunshots;

    public AudioClip lockOnSound;
    public AudioClip errorSound;
    public AudioClip techSound;
    public AudioClip detectSound;
    public AudioClip damageSound;

    public Rigidbody[] parts;
    public ShootPoint[] shootPoints;
    public Vector3 detectionOffset;
    public LineRenderer laser;

    Transform model;
    Animator animator;

    public float attackCooldown = 0.5f;
    float attackTimer = 0;
    float armTimer = 0;

    bool attack = false;
    bool wantsAttack = false;

    public float shootSpeed = 0.1f;
    float shootTimer = 0;
    int shootIndex = 0;


    [HideInInspector]
    public bool destroyed = false;
    void Awake()
    {
        model = transform.Find("Model");
        model.transform.forward = -Vector3.forward;

        animator = GetComponent<Animator>();

        laser.enabled = false;
    }
    public void TakeDamage()
    {
        if (!destroyed)
        {
            Game.PlaySoundEffect(damageSound, transform.position, 0.5f, 1, false);

            if (Random.Range(0, 5) == 0)
            {
                Game.PlaySoundEffect(errorSound, transform.position);
            }

            coll.enabled = false;
            foreach (Rigidbody r in parts)
            {
                r.isKinematic = false;
                Vector3 dir = r.transform.position - transform.position;
                r.AddForce(dir.normalized * Random.Range(20, 50));
            }
            destroyed = true;
        }
    }
    void Shoot(int index)
    {
        Game.PlaySoundEffect(gunshots[Random.Range(0, gunshots.Length)], shootPoints[index].transform.position, 0.8f, 1, false);
 
        GameObject g = Game.BulletPool.GetObject();
        g.transform.position = shootPoints[index].Point;
        Bullet b = g.GetComponent<Bullet>();
        b.velocity = shootPoints[index].transform.forward * 20;
        b.levelIndex = Game.LevelIndex;
        b.ResetBullet();
        g.transform.forward = b.velocity;

        animator.Play("shoot_" + index, 0, 0);
    }
    void Update()
    {
        animator.speed = Game.GameSpeed;
        if (destroyed)
        {
            laser.enabled = false;
            return;
        }

        //detect
        wantsAttack = false;
        Ray ray = new Ray(transform.position + detectionOffset, Vector3.back);
        RaycastHit hit;
        if(Physics.Raycast(ray, out hit, 20, raycastMask))
        {
            if (((1 << hit.collider.gameObject.layer) & attackMask) != 0)
            {
                wantsAttack = true;
            }
        }

        //start attack
        if(!attack && wantsAttack)
        {
            Game.PlaySoundEffect(detectSound, transform.position);

            if (Random.Range(0, 5) == 0)
            {
                Game.PlaySoundEffect(lockOnSound, transform.position);
            }

            attack = true;
        }

        //attack
        if (attack)
        {
            //arm animation
            if (armTimer < 1)
            {
                armTimer = Mathf.MoveTowards(armTimer, 1, Game.DeltaTime * 5);
                animator.SetFloat("ArmValue", armTimer);
            }

            attackTimer += Game.DeltaTime;
            if (attackTimer > attackCooldown)
            {
                shootTimer += Game.DeltaTime;
                if (shootTimer >= shootSpeed)
                {
                    Shoot(shootIndex);
                    shootIndex = (shootIndex + 1) % 2;
                    shootTimer = 0;
                }
            }
            if (attackTimer > attackCooldown * 2)
            {
                attackTimer = 0;
                attack = false;
                if (Random.Range(0, 15) == 0)
                {
                    Game.PlaySoundEffect(techSound, transform.position);
                }
            }
        }
        else
        {
            if (armTimer > 0)
            {
                armTimer = Mathf.MoveTowards(armTimer, 0, Game.DeltaTime);
                animator.SetFloat("ArmValue", armTimer);

            }
        }

        if(wantsAttack && attackTimer < attackCooldown)
        {
            laser.positionCount = 2;
            laser.SetPosition(0, laser.transform.position);
            laser.SetPosition(1, hit.point);

            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            laser.GetPropertyBlock(mpb);
            mpb.SetColor("_Color", Color.Lerp(new Color(1, 0, 0, 0), new Color(0, 1, 0, 1), armTimer));
            laser.SetPropertyBlock(mpb);

            laser.enabled = true;
        }
        else
        {
            laser.enabled = false;
        }
    }
    private void OnDrawGizmos()
    {
        if(shootPoints != null)
        {
            for(int i = 0; i < shootPoints.Length; i++)
            {
                if(shootPoints[i].transform != null)
                {
                    Gizmos.DrawWireSphere(shootPoints[i].Point, 0.1f);
                }
            }
        }

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position + detectionOffset, 0.1f);
    }
}
