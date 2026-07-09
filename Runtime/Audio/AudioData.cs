using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(menuName = "ScriptableObjects/Audio/AudioData")]
public class AudioData : ScriptableObject
{
    [System.Serializable]
    public class AudioClipEntry
    {
        [HorizontalGroup("row", 20f), HideLabel, ToggleLeft]
        [Tooltip("Enable / disable this clip")]
        public bool enabled = true;

        [HorizontalGroup("row"), HideLabel, EnableIf("enabled")]
        public AudioClip clip;

        [HorizontalGroup("row", 120f), HideLabel, EnableIf("enabled")]
        [PropertyRange(0f, 1f), Tooltip("Relative weight (chance to be picked)")]
        public float weight = 1f;
    }

    #if UNITY_EDITOR
        private AudioSource _previewSource;
        private float _previewPeakDb = -80f;

        private bool  _previewHasData;
        private float _previewProgress;
        private float _previewPos, _previewLen, _previewPitch, _previewDb;

        [BoxGroup("Preview")]
        [HorizontalGroup("Preview/Buttons")]
        [PropertyOrder(-10)]
        [Button("▶  Play", ButtonSizes.Large), GUIColor(0.4f, 0.85f, 1f)]
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
        [PropertyOrder(-10)]
        [Button("■  Stop", ButtonSizes.Large), GUIColor(1f, 0.5f, 0.5f)]
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

        [BoxGroup("Preview")]
        [PropertyOrder(-9)]
        [OnInspectorGUI]
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

            if (!_previewHasData)
            {
                UnityEditor.EditorGUILayout.LabelField("Press  ▶ Play  to preview the sound.",
                    UnityEditor.EditorStyles.centeredGreyMiniLabel);
                return;
            }

            Rect r1 = UnityEditor.EditorGUILayout.GetControlRect(false, 20f);
            DrawBar(r1, _previewProgress, new Color(0.30f, 0.65f, 1f),
                $"{_previewPos:0.00}s / {_previewLen:0.00}s      pitch ×{_previewPitch:0.00}");

            UnityEditor.EditorGUILayout.Space(3f);

            float norm     = Mathf.InverseLerp(-60f, 0f, _previewDb);
            float peakNorm = Mathf.InverseLerp(-60f, 0f, _previewPeakDb);

            Rect r2 = UnityEditor.EditorGUILayout.GetControlRect(false, 18f);
            DrawBar(r2, norm, DbColor(norm),
                $"{_previewDb:0.0} dB       peak {_previewPeakDb:0.0} dB");

            Rect peakMark = new Rect(r2.x + r2.width * peakNorm - 1f, r2.y, 2f, r2.height);
            UnityEditor.EditorGUI.DrawRect(peakMark, Color.white);
        }

        private static void DrawBar(Rect rect, float value01, Color fill, string label)
        {
            value01 = Mathf.Clamp01(value01);
            UnityEditor.EditorGUI.DrawRect(rect, new Color(0f, 0f, 0f, 0.35f));
            Rect fillRect = new Rect(rect.x, rect.y, rect.width * value01, rect.height);
            UnityEditor.EditorGUI.DrawRect(fillRect, fill);

            GUIStyle style = new GUIStyle(UnityEditor.EditorStyles.miniBoldLabel)
            { alignment = TextAnchor.MiddleCenter };
            style.normal.textColor = Color.white;
            UnityEditor.EditorGUI.LabelField(rect, label, style);
        }

        private static Color DbColor(float norm)
        {
            if (norm < 0.60f) return new Color(0.30f, 0.80f, 0.35f);
            if (norm < 0.85f) return new Color(0.95f, 0.75f, 0.20f);
            return new Color(0.95f, 0.30f, 0.25f);
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

    // ─────────────────────────────  Clips  ─────────────────────────────
    [PropertyOrder(0)]
    [Title("Clips", "A clip is picked at random on each play", TitleAlignments.Left)]
    [InfoBox("$_description", InfoMessageType.None, "@!string.IsNullOrEmpty(_description)")]
    [SerializeField, HideLabel]
    [ListDrawerSettings(ShowFoldout = false, DraggableItems = true)]
    private List<AudioClipEntry> _clips = new List<AudioClipEntry>();

    [PropertyOrder(1)]
    [SerializeField, Min(0)]
    [LabelText("Avoid Last N Played"), SuffixLabel("clips", true)]
    private int _avoidRepeatingLast = 1;

    // ─────────────────────────────  Volume  ────────────────────────────
    [PropertyOrder(10)]
    [Title("Volume")]
    [SerializeField, ToggleLeft, LabelText("Randomize (range)")]
    private bool _useVolumeRange;

    [PropertyOrder(11)]
    [SerializeField, HideIf("_useVolumeRange"), HideLabel, Range(0.0f, 1.0f)]
    float _volume = 1.0f;

    [PropertyOrder(11)]
    [SerializeField, ShowIf("_useVolumeRange"), HideLabel, MinMaxSlider(0.0f, 1.0f, true)]
    private Vector2 _volumeRange;

    // ─────────────────────────────  Pitch  ─────────────────────────────
    [PropertyOrder(12)]
    [Title("Pitch")]
    [SerializeField, ToggleLeft, LabelText("Randomize (range)")]
    private bool _usePitchRange;

    [PropertyOrder(13)]
    [SerializeField, HideIf("_usePitchRange"), HideLabel, Range(0.75f, 1.25f)]
    private float _pitch = 1.0f;

    [PropertyOrder(13)]
    [SerializeField, ShowIf("_usePitchRange"), HideLabel, MinMaxSlider(0.75f, 1.25f, true)]
    Vector2 _pitchRange = new Vector2(1.0f, 1.0f);

    // ───────────────────────────  Playback  ────────────────────────────
    [PropertyOrder(20)]
    [Title("Playback")]
    [SerializeField, Range(0f, 2.5f), SuffixLabel("s", true)]
    float _delay;

    [PropertyOrder(21)]
    [SerializeField, LabelText("Audio Group")]
    string _audioGroup = string.Empty;

    [PropertyOrder(22)]
    [SerializeField, ToggleLeft, LabelText("Only Play If Visible")]
    private bool _onlyPlayIfVisible;

    [PropertyOrder(23)]
    [SerializeField, ToggleLeft, LabelText("Cooldown")]
    private bool _hasCooldown;

    [PropertyOrder(24)]
    [SerializeField, ShowIf("_hasCooldown"), Indent, HideLabel, SuffixLabel("s", true)]
    float _cooldown = 0f;

    // ───────────────────────────  Advanced  ────────────────────────────
    [PropertyOrder(30)]
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
            foreach (AudioClipEntry e in _clips)
                if (e != null && e.enabled && e.clip != null)
                    return true;

            return false;
        }
    }

    public AudioClip audioClip
    {
        get
        {
            List<AudioClipEntry> playable = new List<AudioClipEntry>(_clips.Count);
            foreach (AudioClipEntry e in _clips)
                if (e != null && e.enabled && e.clip != null && e.weight > 0f)
                    playable.Add(e);

            if (playable.Count == 0) return null;
            if (playable.Count == 1) return playable[0].clip;

            int avoid = Mathf.Clamp(_avoidRepeatingLast, 0, playable.Count - 1);

            List<AudioClipEntry> candidates = new List<AudioClipEntry>(playable.Count);
            float totalWeight = 0f;
            foreach (AudioClipEntry e in playable)
            {
                if (avoid > 0 && _history.Contains(e.clip)) continue;
                candidates.Add(e);
                totalWeight += e.weight;
            }

            if (candidates.Count == 0 || totalWeight <= 0f)
            {
                candidates = playable;
                totalWeight = 0f;
                foreach (AudioClipEntry e in candidates) totalWeight += e.weight;
            }

            float r = Random.value * totalWeight;
            AudioClip chosen = candidates[candidates.Count - 1].clip;
            foreach (AudioClipEntry e in candidates)
            {
                r -= e.weight;
                if (r <= 0f) { chosen = e.clip; break; }
            }

            _history.Add(chosen);
            while (_history.Count > avoid) _history.RemoveAt(0);

            return chosen;
        }
    }
}
