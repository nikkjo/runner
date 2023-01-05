using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class Game : MonoBehaviour
{
    public Level level;
    public Robot robot;
    public Transform cameraLookAt;

    public Text text;
    public Button muteButton;
    public Sprite unmutedSprite;
    public Sprite mutedSprite;

    public Image fadePanel;
    public UIFade deathPanelFade;
    public GameObject menuPanel;

    public Color fadeStartColor;
    public Color fadeEndColor;
    public AnimationCurve fadeCurve;

    public Image batteryUIImage;
    public Sprite[] batteryImages;
    public Color[] batteryColors;

    int currentBatteryImage = -1;
    float batteryTimer = 0;

    public AudioMixerGroup soundEffectMixerGroup;
    public AudioMixer audioMixer;
    public AudioSource musicAudioSource;

    public AudioClip menuButtonPress;
    public AudioClip muteButtonPress;
    public AudioClip restartButtonPress;
    public AudioClip exitButtonPress;
    public AudioClip buttonHover;

    public GameObject bulletPrefab;
    public GameObject soundEffectPrefab;
    public GameObject[] carPrefabs;
    public GameObject[] buildingPrefabs;

    

    public float tileSize;
    public Vector2Int gridSize;
    public float runSpeed = 1;
    public float cameraLookAhead = 4;
    public float carSpawnRate = 5;
    public float buildingSpawnDistance = 100;
    public Vector3 buildingSpawnPositionOffset;
    public float nextBuildingSpawnDistance;
    public LayerMask buildingLayerMask;

    Vector3 gridOffset;
    float distanceTraveled;
    int tilesTraveled;
    int levelIndex = 0;
    bool gameRunning = false;
    bool gameStarted = false;
    float startTimer = 0;
    float deathTimer = 0;
    float carSpawnTimer = 0;
    

    ObjectPool bulletPool;
    ObjectPool sfxPool;
    ObjectPool carsPool;

    public float gameSpeed = 1;

    static Game instance;

    private void Awake()
    {
        instance = this;
        gridOffset = new Vector3(-gridSize.x * tileSize, 0, -gridSize.y * tileSize) / 2.0f;

        fadePanel.color = fadeStartColor;
        levelIndex = 0;

        menuPanel.SetActive(false);
        
    }
    private void Start()
    {
        audioMixer.SetFloat("MusicVolume", -80);
        bulletPool = new ObjectPool(200, transform, () =>
        {
            return GameObject.Instantiate(bulletPrefab, Vector3.zero, Quaternion.identity);
        });

        sfxPool = new ObjectPool(200, transform, () =>
        {
            GameObject g = GameObject.Instantiate(soundEffectPrefab);
            AudioSource source = g.GetComponent<AudioSource>();
            source.outputAudioMixerGroup = soundEffectMixerGroup;

            return g;
        });
        carsPool = new ObjectPool(50, transform, () =>
        {
            return GameObject.Instantiate(carPrefabs[Random.Range(0, carPrefabs.Length)], Vector3.zero, Quaternion.identity);
        });

        StartCoroutine("StartIntro");
    }

    //start
    public void StartGame()
    {
        menuPanel.SetActive(false);

        levelIndex++;

        fadePanel.color = fadeStartColor;
        distanceTraveled = 0;
        tilesTraveled = 0;

        robot.ResetRobot();
        robot.BackToNormal();

        if (level.grid != null)
        {
            level.Clear();
            level.transform.position = Vector3.zero;
        }
        else
        {
            level.CreateGrid();
        }
        for (int y = 0; y < gridSize.y; y++)
        {
            level.GenerateRow(y, false, 0);
        }

        gameStarted = true;
        gameRunning = false;
        startTimer = 0;
    }
    void ExitGame()
    {       
        SceneManager.LoadScene(0);
    }
    IEnumerator StartIntro()
    {
        fadePanel.color = fadeStartColor;
        yield return new WaitForSeconds(0.1f);
        StartGame();
    }

    public static SoundEffect PlaySoundEffect(AudioClip audioClip, Vector3 position, float volume, float pitch, bool flat)
    {
        GameObject g = instance.sfxPool.GetObject();
        g.transform.position = position;
        SoundEffect sound = g.GetComponent<SoundEffect>();
        sound.Play(audioClip, volume, pitch, flat);
        return sound;
    }
    public static SoundEffect PlaySoundEffect(AudioClip audioClip, Vector3 position)
    {
        return PlaySoundEffect(audioClip, position, 1, 1, false);
    }

    public static float DeltaTime
    {
        get
        {
            float dt = Time.deltaTime * instance.gameSpeed;
           
            return Mathf.Min(0.017f, dt);
        }
    }
    public static float GameSpeed
    {
        get
        {
            return instance.gameSpeed;
        }
    }
    public static int LevelIndex
    {
        get
        {
            return instance.levelIndex;
        }
    }
    public static ObjectPool BulletPool
    {
        get
        {
            return instance.bulletPool;
        }
    }
    public static ObjectPool SfxPool
    {
        get
        {
            return instance.sfxPool;
        }
    }
    public static ObjectPool CarsPool
    {
        get
        {
            return instance.carsPool;
        }
    }
    public static Vector3 GridOffset
    {
        get
        {
            return instance.gridOffset;
        }
    }
    public static float TileSize
    {
        get
        {
            return instance.tileSize;
        }
    }
    public static Vector2Int GridSize
    {
        get
        {
            return instance.gridSize;
        }
    }
    public static float RunSpeed
    {
        get
        {
            return instance.runSpeed;
        }
    }
    public static int TilesTraveled
    {
        get
        {
            return instance.tilesTraveled;
        }
    }

    //buttons
    public void RestartButtonPress()
    {
        PlaySoundEffect(restartButtonPress, Vector3.zero, 1, 1, true);
        StartGame();
    }
    public void MenuButtonPressed()
    {
        if (!RobotDead())
        {
            if (menuPanel.activeSelf)
            {

                menuPanel.SetActive(false);
            }
            else
            {
                menuPanel.SetActive(true);
            }
            PlaySoundEffect(menuButtonPress, Vector3.zero, 1, 1, true);
        }
    }
    public void ExitButtonPress()
    {
        ExitGame();
    }
    public void MuteButtonPress()
    {
        float v = 0;
        audioMixer.GetFloat("MasterVolume", out v);
        if(v == -80)
        {
            audioMixer.SetFloat("MasterVolume", 0);
            muteButton.image.sprite = unmutedSprite;
        }
        else
        {
            audioMixer.SetFloat("MasterVolume", -80);
            muteButton.image.sprite = mutedSprite;
        }
    }
    public void ButtonPointerEnter(Button button)
    {
        if (button.interactable)
        {
            PlaySoundEffect(buttonHover, Vector3.zero, 0.6f, 1, true);
        }
    }

    void AdvanceGrid()
    {
        level.RemoveRow(0);
        for (int y = 1; y < gridSize.y; y++)
        {
            level.MoveRow(y, y - 1);         
        }
        level.GenerateRow(gridSize.y - 1, true, Mathf.Min(1, distanceTraveled / 1000.0f));

        float z = (tilesTraveled + 1) * tileSize;
        level.transform.position = new Vector3(0, 0, z);
    }
    void RunGame(float dt)
    {
        //get input
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        robot.Move();       
        if (Input.GetKey(KeyCode.Space))
        {
            robot.Jump();
        }
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
        {
            robot.Slide();
        }
        if (input.x != 0)
        {
            if (input.x > 0 && robot.currentLane < 2)
            {
                robot.Strafe(robot.currentLane + 1);
            }
            else if (input.x < 0 && robot.currentLane > 0)
            {
                robot.Strafe(robot.currentLane - 1);
            }
        }

        //update robot
        robot.UpdateRobot();

        if (robot.transform.position.y >= 0)
        {
            distanceTraveled = robot.transform.position.z;
        }
        //advance level
        int tile = Mathf.FloorToInt(distanceTraveled / tileSize);
        while (tilesTraveled < tile)
        {
            AdvanceGrid();
            tilesTraveled++;
        }
    }

    bool RobotDead()
    {
        return robot.CurrentState == Robot.State.dead;
    }
    void SpawnCar()
    {
        GameObject car = carsPool.GetObject();
        if (car != null)
        {
            Vector3 pos = cameraLookAt.transform.position;
            pos.x += Random.Range(-100, 100);
            bool forward = Random.Range(0, 2) == 0;
            pos.z += forward ? -200 : 200;
            pos.y = Random.Range(-15, -80);

            car.transform.position = pos;
            car.transform.forward = forward ? Vector3.forward : Vector3.back;
        }
    }
    bool OverlapsBuilding(Vector3 pos, Vector3 halfExtents)
    {
        return Physics.CheckBox(pos, halfExtents, Quaternion.identity, buildingLayerMask);
    }
    void SpawnBuilding()
    {
        Vector3 RandomBuildingPosition()
        {
            Vector3 p = new Vector3(0, -300, nextBuildingSpawnDistance) + buildingSpawnPositionOffset;
            p.x = Random.Range(0, 2) == 0 ? Random.Range(-300, -100) : Random.Range(100, 300);
            return p;
        };

        GameObject g = buildingPrefabs[Random.Range(0, buildingPrefabs.Length)];
        BoxCollider box = g.GetComponent<BoxCollider>();
        Vector3 halfExtents = box.size;
        halfExtents = g.transform.TransformVector(halfExtents * 0.5f);

        Vector3 pos = RandomBuildingPosition();

        int tries = 0;
        int maxTries = 100;
        while (OverlapsBuilding(pos + box.center, halfExtents))
        {
            pos = RandomBuildingPosition();
            tries++;
            if (tries >= maxTries)
            {
                break;
            }
        }

        if (tries < maxTries)
        {
            GameObject.Instantiate(g, pos, Quaternion.identity);
        }
    }

    Vector3 camFollowPos;
    private void FixedUpdate()
    {
        float fdt = Time.fixedDeltaTime * gameSpeed;
        if (fdt > 0)
        {
            Physics.Simulate(fdt);
        }
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            MenuButtonPressed();
        }
       

        gameSpeed = menuPanel.activeSelf ? Mathf.MoveTowards(gameSpeed, 0, Time.deltaTime * 5) : Mathf.MoveTowards(gameSpeed, 1, Time.deltaTime * 5);
        

        
        //spawn buildings
        while (distanceTraveled >= nextBuildingSpawnDistance)
        {
            SpawnBuilding();
            nextBuildingSpawnDistance += buildingSpawnDistance;
        }

        //intro
        if (gameStarted)
        {
            startTimer += Game.DeltaTime;
            fadePanel.color = Color.Lerp(fadeStartColor, 
                fadeEndColor, Mathf.Min(1, startTimer));

            if(startTimer >= 1)
            {
                gameRunning = true;
                gameStarted = false;

                audioMixer.SetFloat("MusicVolume", -7);
                musicAudioSource.time = 0;
                musicAudioSource.pitch = 1;
                musicAudioSource.Play();
            }
        }

        //battery image update
        int targetBattery = RobotDead() ? batteryImages.Length-1 : (batteryImages.Length-1) - Mathf.Min(3, Mathf.FloorToInt(robot.batteryHealth / 25f));
        if(currentBatteryImage != targetBattery)
        {
            batteryTimer += Time.deltaTime;
            if(batteryTimer % 0.25f > 0.125f)
            {
                batteryUIImage.sprite = batteryImages[targetBattery];
                batteryUIImage.color = batteryColors[targetBattery];
            }
            else
            {
                batteryUIImage.sprite = null;
                batteryUIImage.color = Color.clear;
            }
            if(batteryTimer >= 1)
            {
                currentBatteryImage = targetBattery;
            }
        }
        else
        {
            batteryTimer = 0;
            if (currentBatteryImage < 0 || currentBatteryImage > batteryImages.Length - 1)
            {
                batteryUIImage.sprite = null;
                batteryUIImage.color = Color.clear;
            }
            else
            {
                batteryUIImage.sprite = batteryImages[currentBatteryImage];
                batteryUIImage.color = batteryColors[targetBattery];
            }
        }

        if (gameRunning)
        {
            //spawn cars
            carSpawnTimer += Game.DeltaTime;
            float ct = 1.0f / carSpawnRate;
            while(carSpawnTimer >= ct)
            {
                SpawnCar();
                carSpawnTimer -= ct;
            }

            if (RobotDead())
            {
                deathTimer = Mathf.Min(2, deathTimer + Game.DeltaTime);
                float t = fadeCurve.Evaluate(deathTimer * 0.5f);               
                fadePanel.color = Color.Lerp(fadeEndColor, fadeStartColor, t);
                deathPanelFade.SetFadeValue(1.0f - t);
                deathPanelFade.EnableButtons(t >= 0.25f);
                currentBatteryImage = -1;
                menuPanel.SetActive(false);
                musicAudioSource.Pause();
            }
            else
            {
                deathTimer = 0;
                deathPanelFade.SetFadeValue(1);
                deathPanelFade.EnableButtons(false);

                if(gameSpeed > 0)
                {
                    musicAudioSource.UnPause();
                    musicAudioSource.pitch = gameSpeed;
                }
                else
                {
                    musicAudioSource.Pause();
                    
                }
            }
            RunGame(Game.DeltaTime);
            text.text = "distance traveled: " + distanceTraveled;
        }
        else
        {
            deathTimer = 0;
            deathPanelFade.SetFadeValue(1);
            deathPanelFade.EnableButtons(false);
            text.text = "";
        }

        //camera follow
        if(robot.transform.position.y >= 0)
        {
            camFollowPos = robot.transform.position;
        }
        
        Vector3 targetLookAt = new Vector3(0, Mathf.Max(0, camFollowPos.y), camFollowPos.z + cameraLookAhead);
        float dis = Vector3.Distance(cameraLookAt.transform.position, targetLookAt);
        cameraLookAt.transform.position = Vector3.MoveTowards(cameraLookAt.transform.position, targetLookAt, dis * Game.DeltaTime * runSpeed);


        
    }
    private void OnDrawGizmos()
    {
        //Gizmos.DrawWireCube(new Vector3(0, -150, nextBuildingSpawnDistance) + buildingSpawnPositionOffset, new Vector3(600, 300, buildingSpawnDistance));        
    }
}
