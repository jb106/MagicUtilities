using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(menuName = "ScriptableObjects/Audio/AudioData")]
public class AudioData : ScriptableObject
{
    #if UNITY_EDITOR
        private AudioSource _previewSource;
        private float _previewPeakDb = -80f;

        private bool  _previewHasData;
        private float _previewProgress;
        private float _previewPos, _previewLen, _previewPitch, _previewDb;

        [BoxGroup("Preview")]
        [HorizontalGroup("Preview/Buttons")]
        [Button("▶ Play", ButtonSizes.Large), GUIColor(0.4f, 0.85f, 1f)]
        private void EditorPreviewPlay()
        {
            EditorPreviewStop();

            AudioClip clip = audioClip;
            if (clip == null) return;

            GameObject go = new GameObject("AudioData Preview")
            { hideFlags = HideFlags.HideAndDontSave };

            _previewSource = go.AddComponent<AudioSource>();
            _previewSource.clip = clip;
            _previewSource.volume = volume;
            _previewSource.pitch = pitch;
            _previewSource.spatialBlend = 0f;
            _previewSource.Play();

            _previewPeakDb   = -80f;
            _previewHasData  = true;
            _previewProgress = 0f;
            _previewPos      = 0f;
            _previewLen      = clip.length;
            _previewPitch    = _previewSource.pitch;
            _previewDb       = -80f;

            UnityEditor.EditorApplication.update += EditorPreviewTick;
        }

        [HorizontalGroup("Preview/Buttons")]
        [Button("■ Stop", ButtonSizes.Large), GUIColor(1f, 0.5f, 0.5f)]
        private void EditorPreviewStop()
        {
            UnityEditor.EditorApplication.update -= EditorPreviewTick;
            if (_previewSource != null)
            {
                DestroyImmediate(_previewSource.gameObject);
                _previewSource = null;
            }
        }

        private void EditorPreviewTick()
        {
            if (_previewSource == null || !_previewSource.isPlaying)
            {
                if (_previewHasData)
                {
                    _previewProgress = 1f;
                    _previewPos      = _previewLen;
                }
                EditorPreviewStop();
                RepaintInspectors();
                return;
            }
            RepaintInspectors();
        }

        private static void RepaintInspectors()
        {
            foreach (UnityEditor.EditorWindow w in Resources.FindObjectsOfTypeAll<UnityEditor.EditorWindow>())
                if (w.GetType().Name == "InspectorWindow")
                    w.Repaint();
        }

        [OnInspectorGUI, PropertyOrder(100)]
        private void EditorPreviewMeters()
        {
            if (_previewSource != null && _previewSource.clip != null && _previewSource.isPlaying)
            {
                _previewLen      = _previewSource.clip.length;
                _previewPos      = _previewSource.time;
                _previewPitch    = _previewSource.pitch;
                _previewProgress = _previewLen > 0f ? Mathf.Clamp01(_previewPos / _previewLen) : 0f;

                _previewDb     = GetPreviewDb();
                _previewPeakDb = Mathf.Max(_previewPeakDb, _previewDb);
            }

            if (!_previewHasData) return;

            Rect r1 = UnityEditor.EditorGUILayout.GetControlRect(false, 18f);
            UnityEditor.EditorGUI.ProgressBar(r1, _previewProgress,
                $"{_previewPos:0.00}s / {_previewLen:0.00}s   (pitch {_previewPitch:0.00})");

            float norm     = Mathf.InverseLerp(-60f, 0f, _previewDb);
            float peakNorm = Mathf.InverseLerp(-60f, 0f, _previewPeakDb);

            Rect r2 = UnityEditor.EditorGUILayout.GetControlRect(false, 16f);
            UnityEditor.EditorGUI.ProgressBar(r2, norm, $"{_previewDb:0.0} dB   (peak {_previewPeakDb:0.0})");

            Rect peakMark = new Rect(r2.x + r2.width * peakNorm - 1f, r2.y, 2f, r2.height);
            UnityEditor.EditorGUI.DrawRect(peakMark, Color.red);
        }

        private float GetPreviewDb()
        {
            float[] buf = new float[256];
            _previewSource.GetOutputData(buf, 0);
            float sum = 0f;
            for (int i = 0; i < buf.Length; i++) sum += buf[i] * buf[i];
            float rms = Mathf.Sqrt(sum / buf.Length);
            return rms > 0f ? 20f * Mathf.Log10(rms) : -80f;
        }
    #endif
        
    [InfoBox("$_description")]

    [SerializeField] private List<AudioClip> _clips = new List<AudioClip>();

    [SerializeField, Range(0f, 2.5f)] float _delay;
    [SerializeField] string _audioGroup = string.Empty;

    [SerializeField, BoxGroup("Options")] private bool _onlyPlayIfVisible;
    [SerializeField, BoxGroup("Options")] private bool _hasCooldown;
    [SerializeField, ShowIf("_hasCooldown"), BoxGroup("Options")] float _cooldown = 0f;
    [SerializeField, BoxGroup("Options"), Min(0)] private int _avoidRepeatingLast = 1;

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

    private readonly List<AudioClip> _history = new();

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
            if (_clips.Count == 1) return _clips[0];

            int avoid = Mathf.Clamp(_avoidRepeatingLast, 0, _clips.Count - 1);

            List<AudioClip> candidates = new List<AudioClip>(_clips.Count);
            foreach (AudioClip c in _clips)
            {
                if (c == null) continue;
                if (avoid > 0 && _history.Contains(c)) continue;
                candidates.Add(c);
            }

            if (candidates.Count == 0)
            {
                candidates = _clips.FindAll(c => c != null);
            }
            if (candidates.Count == 0) return null;

            AudioClip chosen = candidates[Random.Range(0, candidates.Count)];

            _history.Add(chosen);
            while (_history.Count > avoid) _history.RemoveAt(0);

            return chosen;
        }
    }
}