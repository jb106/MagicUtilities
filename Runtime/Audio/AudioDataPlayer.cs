using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioDataPlayer
{
    public AudioData audioData;
    public Vector3 basePosition;

    public AudioDataPlayer(AudioData a, Vector3 b)
    {
        audioData = a;
        basePosition = b;
    }
}
