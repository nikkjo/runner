using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct Detachable
{
    public Transform transform;

    Transform parent;
    Vector3 localPosition;
    Quaternion localRotation;
    Vector3 localScale;

    public void Setup()
    {
        if (transform != null)
        {
            parent = transform.parent;
            localPosition = transform.localPosition;
            localRotation = transform.localRotation;
            localScale = transform.localScale;
        }
    }
    public void Reset()
    {
        if(transform != null)
        {
            transform.SetParent(parent);
            transform.localPosition = localPosition;
            transform.localRotation = localRotation;
            transform.localScale = localScale;
        }
    }
}
[System.Serializable]
public struct Ragdoll
{
    public Vector3 inheritedVelocity;
    public Transform[] parts;
    public Detachable[] detachable;

    bool setupDone;

    public void Enable(bool yes)
    {
        if (!setupDone)
        {
            for (int i = 0; i < detachable.Length; i++)
            {
                detachable[i].Setup();
            }
            setupDone = true;
        }
        foreach (Transform t in parts)
        {
            Collider c = t.GetComponent<Collider>();
            c.enabled = yes;

            Rigidbody r = t.GetComponent<Rigidbody>();
            r.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            r.velocity = inheritedVelocity;
            r.isKinematic = !yes;
        }

        for (int i = 0; i < detachable.Length; i++)
        {
            Collider c = detachable[i].transform.GetComponent<Collider>();
            c.enabled = yes;

            Rigidbody r = detachable[i].transform.GetComponent<Rigidbody>();
            r.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            r.velocity = inheritedVelocity;
            r.isKinematic = !yes;

            if (yes)
            {
                detachable[i].transform.SetParent(null);
            }
            else
            {
                detachable[i].Reset();                
            }
        }
    }
}
public class Robot : MonoBehaviour
{
    public enum State
    {
        normal,
        jump,
        strafe,
        slide,
        dead
    }
    public enum DeathType
    {
        wall,
        fallen,
        shot,
        battery
    }
    public Animator animator;
    public Transform model;
    public TrailRenderer swordTrail;

    public AudioClip[] footsteps;
    public AudioClip landSound;
    public AudioClip jumpSound;
    public AudioClip strafeSound;
    public AudioClip slideSound;
    public AudioClip attackSound;
    public AudioClip deathSound;
    public AudioClip hitWallSound;
    public AudioClip gotShotSound;
    public AudioClip batteryDeathSound;
    public AudioClip batteryPickupSound;

    public LayerMask groundLayer;
    public LayerMask enemyLayer;
    public LayerMask batteryLayer;

    public float strafeSpeed = 5;
    public Vector3 attackOffset;
    public float attackRadius;
   
    public float slideTime = 0.5f;
    public float jumpStrength = 15;
    public float gravityStrength = 30;

    public int batteryHealth;
    public float batteryDepleteRatePerSecond = 2;
    public int currentLane = 1;


    State currentState;

    float batteryTimer = 0;
    
    //strafe
    int strafeTargetLane;
    float strafeTimer = 0;
    float slideTimer = 0;

    //movement
    public Vector3 velocity;
    bool wantsMove = false;

    public CapsuleCollider coll;
    public Ragdoll ragdoll;
    public Collider[] overlaps;

    float colliderHeight = 0;
    Vector3 colliderCenter;

    private void Awake()
    {
        overlaps = new Collider[5];
        colliderHeight = coll.height;
        colliderCenter = coll.center;
        ResetRobot();
    }
    public State CurrentState
    {
        get
        {
            return currentState;
        }
    }
    Vector3 GetLanePos(int index)
    {
        return new Vector3(Game.GridOffset.x + index * Game.TileSize + Game.TileSize / 2, transform.position.y, transform.position.z);
    }
    public void ResetRobot()
    {
        batteryHealth = 100;
        model.transform.rotation = Quaternion.identity;
        ragdoll.Enable(false);
        animator.enabled = true;

        coll.enabled = true;
        coll.height = colliderHeight;
        coll.center = colliderCenter;
        velocity = Vector3.zero;

        transform.position = Vector3.zero;
        currentLane = 1;
        
        swordTrail.emitting = false;
        swordTrail.Clear();

        animator.Play("idle", 0, 0);
        animator.Play("nothing", 1, 0);
        animator.SetBool("running", false);
        animator.SetBool("sliding", false);
    }
    public void BackToNormal()
    {             
        currentState = State.normal;
    }
    public void Move()
    {
        AnimatorTransitionInfo ati = animator.GetAnimatorTransitionInfo(0);
        if(ati.IsName("run -> idle") || ati.IsName("idle -> run"))
        {
            animator.Play("run", 0, ati.normalizedTime);
        }

        wantsMove = true;
    }
    public void Dead(DeathType death, Vector3 inheritVelocity = default(Vector3))
    {
        if (currentState != State.dead)
        {
            animator.enabled = false;
            coll.enabled = false;
            ragdoll.inheritedVelocity = inheritVelocity;
            ragdoll.Enable(true);
            velocity = Vector3.zero;
            wantsMove = false;
            currentState = State.dead;
            
            switch(death)
            {
                case DeathType.wall:
                    Game.PlaySoundEffect(hitWallSound, transform.position);
                    Game.PlaySoundEffect(deathSound, transform.position, 1, 1, true);
                    break;
                case DeathType.shot:
                    Game.PlaySoundEffect(gotShotSound, transform.position);
                    Game.PlaySoundEffect(deathSound, transform.position, 1, 1, true);
                    break;
                case DeathType.fallen:
                    Game.PlaySoundEffect(deathSound, transform.position, 1, 1, true);
                    break;
                case DeathType.battery:
                    Game.PlaySoundEffect(batteryDeathSound, transform.position, 0.8f, Random.Range(0.5f, 1f), false);
                    break;
            }

        }
    }
    public void Jump()
    {
        bool canJump = currentState == State.normal;
        if (canJump)
        {
            bool falling = animator.GetCurrentAnimatorStateInfo(0).IsName("fall");
            if (!falling)
            {
                currentState = State.jump;
                velocity.y = jumpStrength;
                Game.PlaySoundEffect(jumpSound, transform.position, 0.5f, Random.Range(0.95f, 1.05f), false);
            }
        }
    }
    public void Slide()
    {
        if(currentState == State.normal)
        {
            animator.Play("nothing", 1, 0);
            animator.SetBool("sliding", true);
            slideTimer = 0;
            currentState = State.slide;
            Game.PlaySoundEffect(slideSound, transform.position);
        }
    }
    public void Strafe(int targetLaneIndex)
    {
        if (currentState == State.normal)
        {
            if (transform.position.x == GetLanePos(currentLane).x)
            {
                strafeTargetLane = targetLaneIndex;
                strafeTimer = 0;
                currentState = State.strafe;
                Game.PlaySoundEffect(strafeSound, transform.position, 0.8f, Random.Range(0.9f, 1.1f), false);
            }
        }
    }  
    void DoAttackAnimation()
    {
        AnimatorStateInfo asi = animator.GetCurrentAnimatorStateInfo(0);
        if (currentState != State.slide && currentState != State.dead)
        {           
            animator.Play("attack", 1, 0);
            if (asi.IsName("run"))
            {
                animator.Play("run", 0, 0);
            }
            if (asi.IsName("land"))
            {
                if (wantsMove)
                {
                    animator.Play("run", 0, 0);
                }
                else
                {
                    animator.Play("idle", 0, 0);
                }
            }
            swordTrail.Clear();
        }
    }
    public void TakeDamage(object o)
    {
        if(o.GetType() == typeof(Bullet))
        {
            if (currentState != State.dead)
            {
                Bullet b = (Bullet)o;
                Dead(DeathType.shot, b.velocity * 0.3f);
                
            }
        }  
    }
    bool Grounded(out Vector3 point, float rayLength)
    {
        point = Vector3.zero;
        RaycastHit hit;

        Vector3 origin = transform.position + new Vector3(0, 0.5f, 0);
        if (Physics.Raycast(origin, Vector3.down, out hit, rayLength + 0.5f, groundLayer))
        {
            point = hit.point;
            return true;
        }
        return false;
    }

    List<GameObject> detectedEnemies = new List<GameObject>();
    void DetectEnemies()
    {
        for(int i = detectedEnemies.Count - 1; i >= 0; i--)
        {
            if(detectedEnemies[i] == null)
            {
                detectedEnemies.RemoveAt(i);
            }
        }

        bool newDetections = false;
        int count = Physics.OverlapSphereNonAlloc(transform.position + attackOffset, attackRadius, overlaps, enemyLayer);
        for (int i = 0; i < count; i++)
        {
            Transform t = overlaps[i].transform;
            if (t.position.z > transform.position.z)
            {
                if (!detectedEnemies.Contains(overlaps[i].gameObject))
                {
                    detectedEnemies.Add(overlaps[i].gameObject);
                    Debug.DrawLine(transform.position, t.position, Color.white, 5);
                    newDetections = true;
                }
            }
        }
        if(newDetections)
        {
            DoAttackAnimation();
        }
    }
    void DamageEnemies()
    {
        AnimatorTransitionInfo ati = animator.GetAnimatorTransitionInfo(0);
        AnimatorStateInfo upperAsi = animator.GetCurrentAnimatorStateInfo(1);
        bool sliding = currentState == State.slide || ati.IsName("slide -> run") || ati.IsName("slide -> idle");
        if (sliding ? true : upperAsi.IsName("attack") && upperAsi.normalizedTime >= 0.5)
        {
            bool hitSomething = false;
            int count = Physics.OverlapSphereNonAlloc(transform.position, attackRadius, overlaps, enemyLayer);
            for (int i = 0; i < count; i++)
            {
                overlaps[i].gameObject.SendMessage("TakeDamage");
                hitSomething = true;
            }
            if(hitSomething)
            {
                Game.PlaySoundEffect(attackSound, transform.position);
            }
        }
    }
    void WallCollision()
    {
        float offset = coll.height / 2.0f - coll.radius;
        Vector3 localPoint0 = coll.center - transform.up * offset;
        Vector3 localPoint1 = coll.center + transform.up * offset;

        Vector3 point0 = transform.TransformPoint(localPoint0);
        Vector3 point1 = transform.TransformPoint(localPoint1);

        bool hitWall = false;
        int count = Physics.OverlapCapsuleNonAlloc(point0, point1, coll.radius, overlaps, groundLayer);
        for (int i = 0; i < count; i++)
        {
            Vector3 dir;
            float dis;
            if (Physics.ComputePenetration(coll, transform.position, transform.rotation, overlaps[i], overlaps[i].transform.position, overlaps[i].transform.rotation, out dir, out dis))
            {
                float dot = Vector3.Dot(dir, Vector3.back);
                if (dot > -0.5f)
                {
                    transform.position += new Vector3(dir.x, dir.y, 0) * dis;
                    hitWall = true;
                }
            }
        }
        if(hitWall)
        {            
            Dead(DeathType.wall);
        }
    }
    public void PlaySoundEffect(string name)
    {
        if(name == "footstep")
        {
            Game.PlaySoundEffect(footsteps[Random.Range(0, footsteps.Length)], transform.position, 0.1f, Random.Range(0.9f, 1.1f), false);
        }
    }
    void CheckForBattery()
    {
        float offset = coll.height / 2.0f - coll.radius;
        Vector3 localPoint0 = coll.center - transform.up * offset;
        Vector3 localPoint1 = coll.center + transform.up * offset;

        Vector3 point0 = transform.TransformPoint(localPoint0);
        Vector3 point1 = transform.TransformPoint(localPoint1);

        int count = Physics.OverlapCapsuleNonAlloc(point0, point1, coll.radius, overlaps, batteryLayer);
        for (int i = 0; i < count; i++)
        {
            GameObject.Destroy(overlaps[i].gameObject);
            batteryHealth += 50;
            if(batteryHealth >= 100)
            {
                batteryHealth = 100;
            }
            Game.PlaySoundEffect(batteryPickupSound, transform.position, 0.8f, 1, true);
        }
    }
    public void UpdateRobot()
    {
        animator.speed = Game.GameSpeed;

        AnimatorStateInfo baseAsi = animator.GetCurrentAnimatorStateInfo(0);
        AnimatorStateInfo upperAsi = animator.GetCurrentAnimatorStateInfo(1);

        Vector3 groundPoint;
        bool grounded = Grounded(out groundPoint, 0.3f);

        //fall death
        if(transform.position.y < -10)
        {
            Dead(DeathType.fallen);
        }
            
        //update sword trail
        if (upperAsi.IsName("attack"))
        {
            if (upperAsi.normalizedTime > 0.25f && upperAsi.normalizedTime < 0.5)
            {
                swordTrail.emitting = true;
            }
            else
            {
                swordTrail.emitting = false;
            }
        }
        else
        {
            swordTrail.emitting = false;
        }

        //update collider height
        AnimatorTransitionInfo ati = animator.GetAnimatorTransitionInfo(0);
        float slideHeight = colliderHeight * 0.5f;
        Vector3 slideCenter = new Vector3(coll.center.x, colliderCenter.y * 0.5f, coll.center.z);

        if (baseAsi.IsName("slide"))
        {
            if (ati.IsName("slide -> run") || ati.IsName("slide -> idle"))
            {
                coll.height = Mathf.Lerp(slideHeight, colliderHeight, ati.normalizedTime);
                coll.center = Vector3.Lerp(slideCenter, colliderCenter, ati.normalizedTime);
            }
            else
            {
                coll.height = slideHeight;
                coll.center = slideCenter;
            }          
        }
        else
        {
            if (ati.IsName("run -> slide") || ati.IsName("idle -> slide") || ati.IsName("land -> slide"))
            {
                coll.height = Mathf.Lerp(colliderHeight, slideHeight, ati.normalizedTime);
                coll.center = Vector3.Lerp(colliderCenter, slideCenter, ati.normalizedTime);
            }
            else
            { 
                coll.height = colliderHeight;
                coll.center = colliderCenter;
            }

        }

        //states
        switch (currentState)
        {
            case State.normal:
                {
                    animator.SetBool("running", wantsMove);   
                    velocity.z = wantsMove ? Game.RunSpeed : 0;

                    if(!grounded)
                    {
                        if(!baseAsi.IsName("fall"))
                        {
                            animator.Play("fall");
                        }
                        velocity.y -= gravityStrength * Game.DeltaTime;
                    }
                    else
                    {
                        velocity.y = 0;
                        if (baseAsi.IsName("strafe"))
                        {
                            animator.Play(wantsMove ? "run" : "idle");
                        }
                        else if(baseAsi.IsName("fall"))
                        {
                            animator.Play("land");
                            Game.PlaySoundEffect(landSound, transform.position);
                        }
                    }
                }
                break;
            case State.jump:
                {
                    if (!baseAsi.IsName("jump") && !baseAsi.IsName("fall"))
                    {
                        animator.Play("jump");
                        transform.position += Vector3.up * 0.31f;
                    }
                    else if(grounded)
                    {                      
                        transform.position = groundPoint;
                        animator.Play("land");
                        Game.PlaySoundEffect(landSound, transform.position);
                        currentState = State.normal;
                    }
                    else
                    {
                        velocity.y -= gravityStrength * Game.DeltaTime;
                    }
                    
                }
                break;
            case State.strafe:
                {
                    velocity.z = wantsMove ? Game.RunSpeed : 0;
                    if (!grounded)
                    {
                        if (!baseAsi.IsName("fall"))
                        {
                            animator.Play("fall");
                        }
                        velocity.y -= gravityStrength * Game.DeltaTime;
                    }
                    else
                    {
                        if(!baseAsi.IsName("strafe"))
                        {
                            animator.Play("strafe");
                        }
                    }

                    strafeTimer += Game.DeltaTime * strafeSpeed;
                    transform.position = Vector3.Lerp(GetLanePos(currentLane), GetLanePos(strafeTargetLane), Mathf.Min(1, strafeTimer));
                    float yRot = strafeTargetLane - currentLane > 0 ? 45 : -45;
                    model.transform.localRotation = Quaternion.Euler(0, yRot, 0);

                    if (strafeTimer >= 1)
                    {
                        model.transform.localRotation = Quaternion.identity;
                        currentLane = strafeTargetLane;                        
                        currentState = State.normal;
                    }
                }
                break;
            case State.slide:
                {
                    slideTimer += Game.DeltaTime;
                    velocity.z = Game.RunSpeed;
                    
                    if(!grounded)
                    {
                        velocity.y -= gravityStrength * Game.DeltaTime;
                        if(baseAsi.IsName("fall"))
                        {
                            animator.Play("land");
                        }
                    }
                    else
                    {
                        velocity.y = 0;
                    }
                   
                    if (slideTimer >= slideTime)
                    {                      
                        animator.SetBool("sliding", false);
                        currentState = State.normal;
                    }
                }
                break;
        }

        //update battery health
        if(currentState != State.dead)
        {
            batteryTimer += Game.DeltaTime;
            float time = 1.0f / batteryDepleteRatePerSecond;
            while (batteryTimer >= time)
            {
                batteryHealth--;
                if(batteryHealth <= 0)
                {
                    batteryHealth = 0;
                    Dead(DeathType.battery);
                    break;
                }
                batteryTimer -= time;
            }
        }

        wantsMove = false;

        
        //move and colide
        transform.Translate(velocity * Game.DeltaTime);
        if (Grounded(out groundPoint, 0.3f))
        {
            transform.position = new Vector3(transform.position.x, groundPoint.y, transform.position.z);
            velocity.y = 0;
        }
        WallCollision();

        //attack enemies
        DetectEnemies();
        DamageEnemies();

        //battery pick up
        CheckForBattery();
    }
    private void OnDrawGizmos()
    {
        if (Application.isEditor)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position + attackOffset, attackRadius);
        }
    }
}
