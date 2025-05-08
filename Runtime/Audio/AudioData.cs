using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(menuName = "ScriptableObjects/Audio/AudioData")]
public class AudioData : ScriptableObject
{
    [InfoBox("$_description")]

    [SerializeField] private List<AudioClip> _clips = new List<AudioClip>();

    [SerializeField, Range(0f, 2.5f)] float _delay;
    [SerializeField] string _audioGroup = string.Empty;

    [SerializeField, BoxGroup("Options")] private bool _onlyPlayIfVisible;
    [SerializeField, BoxGroup("Options")] private bool _hasCooldown;
    [SerializeField, ShowIf("_hasCooldown"), BoxGroup("Options")] float _cooldown = 0f;

    [SerializeField, BoxGroup("Options")] private bool _useVolumeRange;
    [SerializeField, HideIf("_useVolumeRange"), Range(0.0f, 1.0f), BoxGroup("Options")] float _volume = 1.0f;
    [SerializeField, ShowIf("_useVolumeRange"), MinMaxSlider(0.0f, 1.0f), BoxGroup("Options")] private Vector2 _volumeRange;

    [SerializeField, BoxGroup("Options")] private bool _usePitchRange;
    [SerializeField, HideIf("_usePitchRange"), BoxGroup("Options"), Range(0.75f, 1.25f)] private float _pitch = 1.0f;
    [SerializeField, MinMaxSlider(0.75f, 1.25f), ShowIf("_usePitchRange"), BoxGroup("Options")] Vector2 _pitchRange = new Vector2(1.0f, 1.0f);


    [SerializeField, FoldoutGroup("Advanced")][Range(0.0f, 1.0f)] float _spatialBlend = 0f;
    [SerializeField, FoldoutGroup("Advanced")][Range(0, 256)] int _priority = 128;
    [SerializeField, FoldoutGroup("Advanced"), TextArea] string _description;

    public float delay => _delay;
    public string audioGroup { get { return _audioGroup; } }
    public float volume { get { return _useVolumeRange ? Random.Range(_volumeRange.x, _volumeRange.y) : _volume; } }
    public bool onlyPlayIfVisible => _onlyPlayIfVisible;
    public bool hasCooldown => _hasCooldown;
    public float cooldown => _cooldown;
    public float spatialBlend { get { return _spatialBlend; } }

    public float pitch { get { return _usePitchRange ? Random.Range(_pitchRange.x, _pitchRange.y) : _pitch; } }
    public int priority { get { return _priority; } }

    [HideInInspector] public float volumeMultiplier = -1f;

    private List<AudioClip> _clipsLeftToPlay;
    private AudioClip _lastClipPlayed;

    public AudioData DeepCopyWithCustomVolume(float customVolume)
    {
        AudioData dc = new AudioData();

        dc._audioGroup = _audioGroup;
        dc._useVolumeRange = _useVolumeRange;
        dc._volume = customVolume;
        dc._volumeRange = _volumeRange;
        dc._spatialBlend = _spatialBlend;
        dc._usePitchRange = _usePitchRange;
        dc._pitch = _pitch;
        dc._pitchRange = _pitchRange;
        dc._priority = _priority;

        return dc;
    }

    public bool checkIfAudioClipExist
    {
        get
        {
            if (_clips.Count == 0) return false;

            foreach (AudioClip clip in _clips)
                if (clip == null)
                    return false;

            return true;
        }
    }

    public AudioClip audioClip
    {
        get
        {
            if (_clips.Count == 0) return null;

            bool reconstructLeftToPlay = false;

            if (_clipsLeftToPlay != null)
            {
                foreach (AudioClip c in _clipsLeftToPlay)
                {
                    if (_clips.Contains(c) == false)
                    {
                        reconstructLeftToPlay = true;
                        break;
                    }
                }
            }

            if (_clipsLeftToPlay == null || reconstructLeftToPlay || _clipsLeftToPlay.Count == 0)
            {
                _clipsLeftToPlay = new List<AudioClip>();
                foreach (AudioClip cl in _clips)
                {
                    if (_lastClipPlayed != cl)
                        _clipsLeftToPlay.Add(cl);
                }
            }

            AudioClip clip;

            if (_clips.Count == 1)
            {
                clip = _clips[0];
            }
            else
            {
                int randomIndex = Random.Range(0, _clipsLeftToPlay.Count);
                clip = _clipsLeftToPlay[randomIndex];
                _lastClipPlayed = clip;

                _clipsLeftToPlay.RemoveAt(randomIndex);
            }

            return clip;
        }
    }
}