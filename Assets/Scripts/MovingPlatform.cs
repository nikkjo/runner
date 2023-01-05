using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    public int currentLane;
    public float moveSpeed = 1;
    int moveDirection;
    int targetLane;
    Vector3 startPos;
    Vector3 targetPos;
    float timer = 0;

    void NextTarget()
    {
        targetLane = currentLane + moveDirection;
        if (targetLane < 0 || targetLane > Game.GridSize.x - 1)
        {
            targetLane = currentLane - moveDirection;
            moveDirection = -moveDirection;
        }
        startPos = transform.position;
        targetPos = new Vector3(Game.GridOffset.x + targetLane * Game.TileSize + Game.TileSize / 2, startPos.y, startPos.z);
    }
    void Start()
    {
        moveDirection = Random.Range(0, 2) == 0 ? -1 : 1;
        NextTarget();
    }
    void Update()
    {
        if(currentLane != targetLane)
        {
            timer += Time.deltaTime * moveSpeed;
            transform.position = Vector3.Lerp(startPos, targetPos, Mathf.Min(1, timer));
            
            if(timer >= 1.0f)
            {
                currentLane = targetLane;
                timer = 0;
                NextTarget();
            }
        }
    }
}
