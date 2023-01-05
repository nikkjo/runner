using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIFade : MonoBehaviour
{
    Image[] images;
    Text[] texts;
    Button[] buttons;

    void Start()
    {
        images = gameObject.GetComponentsInChildren<Image>();
        texts = gameObject.GetComponentsInChildren<Text>();
        buttons = gameObject.GetComponentsInChildren<Button>();
    }
    public void SetFadeValue(float value)
    {
        foreach(Image i in images)
        {
            i.color = Color.Lerp(Color.white, new Color(1, 1, 1, 0), value);
        }
        foreach(Text t in texts)
        {
            t.color = Color.Lerp(Color.white, new Color(1, 1, 1, 0), value);
        }       
    }
    public void EnableButtons(bool enable)
    {
        foreach(Button b in buttons)
        {
            b.interactable = enable;
        }
    }
}
