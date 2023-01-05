using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile
{
    public enum Type
    {
        none,
        block,
        pipe,
        hole,
        turret,
        platform
    }
    public int x;
    public int y;
    public Type type;

    public Tile(int _x, int _y)
    {
        x = _x;
        y = _y;
        
    }
    public Vector3 Position
    {
        get
        {
            float hs = Game.TileSize / 2;
            float z = Game.TilesTraveled * Game.TileSize;
            return new Vector3(x * Game.TileSize + hs, 0, y * Game.TileSize + hs) + new Vector3(0, 0, z) + Game.GridOffset;
        }
    }
}
public class Level : MonoBehaviour
{
    public GameObject floorPrefab;
    public GameObject blockPrefab;
    public GameObject pipePrefab;
    public GameObject turretPrefab;
    public GameObject platformPrefab;

    public GameObject batteryPrefab;

    GameObject[] rows;
    System.Random rand;

    public Tile[,] grid;

    public void CreateGrid()
    {
        grid = new Tile[Game.GridSize.x, Game.GridSize.y];
        for (int y = 0; y < Game.GridSize.y; y++)
        {
            for (int x = 0; x < Game.GridSize.x; x++)
            {
                grid[x, y] = new Tile(x, y);
            }
        }
        rows = new GameObject[Game.GridSize.y];
        rand = new System.Random(System.DateTime.Now.Second);
    }
    Tile.Type GetTileType(int x, int y)
    {
        if (x >= 0 && x < Game.GridSize.x && y >= 0 && y < Game.GridSize.y)
        {
            return grid[x, y].type;
        }
        return Tile.Type.none;
    }
    Vector2Int[] offsets = new Vector2Int[]
    {
        new Vector2Int(0, 1),
        new Vector2Int(1, 1),
        new Vector2Int(1, 0),
        new Vector2Int(1, -1),
        new Vector2Int(0, -1),
        new Vector2Int(-1, -1),
        new Vector2Int(-1, 0),
        new Vector2Int(-1, 1)
    };
    public void Clear()
    {
        for(int y = 0; y < Game.GridSize.y; y++)
        {
            for(int x = 0; x < Game.GridSize.x; x++)
            {
                grid[x, y].type = Tile.Type.none;
            }
            RemoveRow(y);
        }
        rand = new System.Random(System.DateTime.Now.Second);
    }
    public void GenerateRow(int y, bool createObstacles, float difficultyValue)
    {
        if (rows[y] != null)
        {
            RemoveRow(y);
        }
        

        GameObject row = new GameObject();
        row.name = "row_" + y;
        row.transform.SetParent(transform);
        rows[y] = row;

        if (createObstacles)
        {
            //block
            for (int x = 0; x < Game.GridSize.x; x++)
            {

                int minChance = 0;
                int maxChance = GetTileType(x, y - 1) == Tile.Type.block ? (int)Mathf.Lerp(2, 4, difficultyValue) : (int)Mathf.Lerp(20, 10, difficultyValue);

                bool hasDiagonal = false;
                bool hasSide = false;
                for (int i = 0; i < offsets.Length; i++)
                {
                    if (GetTileType(x + offsets[i].x, y + offsets[i].y) == Tile.Type.block)
                    {
                        if (i % 2 == 0)
                        {
                            hasSide = true;
                        }
                        else
                        {
                            hasDiagonal = true;
                        }
                    }
                }
                if ((hasDiagonal && !hasSide) || GetTileType(x, y - 1) == Tile.Type.pipe || GetTileType(x, y - 2) == Tile.Type.pipe)
                {
                    minChance = 1;
                    maxChance = 1;
                }

                if (rand.Next(minChance, maxChance) == 0)
                {
                    grid[x, y].type = Tile.Type.block;
                }
            }

            //turret
            for (int x = 0; x < Game.GridSize.x; x++)
            {
                if (grid[x, y].type == Tile.Type.none)
                {
                    bool clear = true;
                    for(int yy = -2; yy < 0; yy++)
                    {
                        if(GetTileType(x, y + yy) != Tile.Type.none)
                        {
                            clear = false;
                        }
                    }

                    if (rand.Next(0, (int)Mathf.Lerp(50, 10, difficultyValue)) == 0 && clear)
                    {
                        grid[x, y].type = Tile.Type.turret;
                    }
                }
            }
            //hole
            int holesCount = 0;
            for (int x = 0; x < Game.GridSize.x; x++)
            {
                if (grid[x, y].type == Tile.Type.none)
                {
                    if (rand.Next(0, (int)Mathf.Lerp(40, 10, difficultyValue)) == 0)
                    {
                        grid[x, y].type = Tile.Type.hole;
                        holesCount++;
                    }
                }
            }

            //pipe
            for (int x = 0; x < Game.GridSize.x; x++)
            {
                if (grid[x, y].type == Tile.Type.none)
                {
                    if ((GetTileType(x - 1, y) == Tile.Type.block || GetTileType(x + 1, y) == Tile.Type.block)
                        && (GetTileType(x, y - 1) == Tile.Type.none && GetTileType(x, y - 2) != Tile.Type.pipe)
                        && rand.Next(0, (int)Mathf.Lerp(20, 10, difficultyValue)) == 0)
                    {
                        grid[x, y].type = Tile.Type.pipe;
                    }
                }
            }

            //platform
            for (int x = 0; x < Game.GridSize.x; x++)
            {
                if (grid[x, y].type == Tile.Type.none)
                {
                    if(holesCount == 2 &&
                        rand.Next(0, (int)Mathf.Lerp(5, 1, difficultyValue)) == 0)
                    {
                        grid[x, y].type = Tile.Type.platform;
                    }
                }
            }
        }

        //instantiate
        for (int x = 0; x < Game.GridSize.x; x++)
        {
            bool hasFloor = true;
            switch (grid[x,y].type)
            {
                case Tile.Type.block:
                    GameObject.Instantiate(blockPrefab, grid[x, y].Position, Quaternion.identity, row.transform);
                    break;
                case Tile.Type.turret:
                    GameObject.Instantiate(turretPrefab, grid[x, y].Position, Quaternion.identity, row.transform);
                    break;
                case Tile.Type.pipe:
                    {
                        GameObject pipe = GameObject.Instantiate(pipePrefab, grid[x, y].Position, Quaternion.identity, row.transform);
                        if (GetTileType(x - 1, y) == Tile.Type.block)
                        {
                            pipe.transform.Find("Side_Left").gameObject.SetActive(false);
                        }
                        else
                        {
                            pipe.transform.Find("Side_Right").gameObject.SetActive(false);
                        }
                    }
                    break;
                case Tile.Type.hole:
                    hasFloor = false;
                    break;
                case Tile.Type.platform:
                    hasFloor = false;
                    MovingPlatform mp = GameObject.Instantiate(platformPrefab, grid[x, y].Position, Quaternion.identity, row.transform).GetComponent<MovingPlatform>();
                    mp.currentLane = x;
                    break;
            }

            if (hasFloor)
            {
                if(rand.Next(0, 100) == 0 && createObstacles)
                {
                   
                    if(grid[x,y].type == Tile.Type.block || grid[x,y].type == Tile.Type.none)
                    {
                        Vector3 pos = grid[x, y].type == Tile.Type.block ? grid[x, y].Position + new Vector3(0, 2, 0) : grid[x,y].Position;
                        GameObject.Instantiate(batteryPrefab, pos, Quaternion.identity, row.transform);
                    }
                    
                }

                GameObject.Instantiate(floorPrefab, grid[x, y].Position, Quaternion.identity, row.transform);
            }
        }
    }
    public void MoveRow(int from, int to)
    {
        rows[to] = rows[from];
        rows[from] = null;
        rows[to].name = "row_" + from + " -> " + to;
        for(int x = 0; x < Game.GridSize.x; x++)
        {
            grid[x, to].type = grid[x, from].type;
            grid[x, from].type = Tile.Type.none;
        }

        int diff = to - from;
        Vector3 v = rows[to].transform.position;
        v.z += diff * Game.TileSize;
        rows[to].transform.position = v;
    }
    public void RemoveRow(int y)
    {
        GameObject.Destroy(rows[y]);
        rows[y] = null;

        for (int x = 0; x < Game.GridSize.x; x++)
        {
            grid[x, y].type = Tile.Type.none;
        }
    }
    private void OnDrawGizmos()
    {
        /*
            if(grid != null)
            {
                Gizmos.color = Color.green;
                for (int y = 0; y < Game.GridSize.y; y++)
                {
                    for (int x = 0; x < Game.GridSize.x; x++)
                    {
                        Vector3 pos = grid[x, y].Position;
                        Vector3 size = new Vector3(Game.TileSize, 0, Game.TileSize);

                        Gizmos.color = Color.white;
                        Gizmos.DrawWireCube(pos, size);

                        switch(grid[x,y].type)
                        {
                            case Tile.Type.block:
                                Gizmos.color = Color.green;
                                Gizmos.DrawCube(pos, size);
                                break;
                            case Tile.Type.turret:
                                Gizmos.color = Color.red;
                                Gizmos.DrawCube(pos, size);
                                break;
                            case Tile.Type.pipe:
                                Gizmos.color = Color.yellow;
                                Gizmos.DrawCube(pos, size);
                                break;
                            case Tile.Type.hole:
                                Gizmos.color = new Color(0, 0, 0, 0.5f);
                                Gizmos.DrawCube(pos, size);
                                break;
                        }
                    }
                }
            }
        */
    }
}
