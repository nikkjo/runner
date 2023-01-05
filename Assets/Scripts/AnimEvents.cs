using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimEvents : MonoBehaviour
{
    public void PlaySound(string name)
    {
        transform.root.gameObject.BroadcastMessage("PlaySoundEffect", name);
    }
}
