using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;

public class MainMenu : MonoBehaviour
{
    public float wobbleDistance = 0.1f;

    public Image fadeImage;
    public Color fadeStartColor;
    public Color fadeEndColor;

    public Button howToButton;
    public UIFade menuButtons;

    public GameObject tutorialPanel;

    public AudioMixer audioMixer;

    public AudioClip buttonPress;
    public AudioClip buttonHover;

    AudioSource audioSource;

    AsyncOperation startOperation;
    bool starting = false;
    float startTimer = 0;

    Camera cam;
    Vector3 lookAtPos;

    Vector3 startPos;
    Vector3 nextPos;

    Vector3 GetRandomPos()
    {
        return startPos + Random.insideUnitSphere * wobbleDistance;
    }
    void Start()
    {
        audioMixer.SetFloat("MasterVolume", 0);
        audioMixer.SetFloat("MusicVolume", -7);

        audioSource = GetComponent<AudioSource>();

        fadeImage.color = fadeStartColor;

        cam = Camera.main;
        lookAtPos = cam.transform.position + cam.transform.forward * 10;
        startPos = lookAtPos;
        nextPos = GetRandomPos();
    }
    public void StartGame()
    {
        if (!starting)
        {
            menuButtons.EnableButtons(false);
            howToButton.gameObject.SetActive(false);

            audioSource.PlayOneShot(buttonPress);
            startOperation = SceneManager.LoadSceneAsync(1);
            startOperation.allowSceneActivation = false;
            starting = true;
        }
    }
    public void ExitGame()
    {
        audioSource.PlayOneShot(buttonPress);
        Application.Quit();
    }
    public void HowToPressed()
    {
        if(tutorialPanel.activeSelf)
        {
            tutorialPanel.SetActive(false);
        }
        else
        {
            tutorialPanel.SetActive(true);
        }
        audioSource.PlayOneShot(buttonPress);
    }
    public void ButtonPointerEnter(Button button)
    {
        if (button.interactable)
        {
            audioSource.PlayOneShot(buttonHover);
        }
    }
    void Update()
    {
        if(starting)
        {
            startTimer += Time.deltaTime;
            fadeImage.color = Color.Lerp(fadeStartColor, fadeEndColor, Mathf.Min(1, startTimer));
            audioMixer.SetFloat("MusicVolume", Mathf.Lerp(-7, -80, Mathf.Min(1, startTimer)));
            menuButtons.SetFadeValue(Mathf.Min(1, startTimer));

            if (startTimer >= 1)
            {
                if (startOperation.progress >= 0.9f)
                {
                    startOperation.allowSceneActivation = true;
                }
            }
        }

        lookAtPos = Vector3.MoveTowards(lookAtPos, nextPos, Time.deltaTime * wobbleDistance);
        if(lookAtPos == nextPos)
        {
            nextPos = GetRandomPos();
        }

        Vector3 dir = (lookAtPos - cam.transform.position).normalized;
        float dis = Vector3.Distance(cam.transform.forward, dir);
        cam.transform.forward = Vector3.MoveTowards(cam.transform.forward, dir, Time.deltaTime * dis);
    }
}
