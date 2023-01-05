using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundEffect : MonoBehaviour
{
    AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }
    public void Play(AudioClip clip, float volume, float pitch, bool flat)
    {
        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.pitch = pitch;
        audioSource.spatialBlend = flat ? 0 : 1;
        audioSource.Play();
    }
    public bool Playing
    {
        get
        {
            return gameObject.activeSelf;
        }
    }
    void Update()
    {
        if(!audioSource.isPlaying)
        {
            Game.SfxPool.ReturnObject(gameObject);
        }
    }
}
