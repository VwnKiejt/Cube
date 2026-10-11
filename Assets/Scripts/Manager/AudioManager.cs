using System;
using System.IO;
using UnityEngine;
using UnityEngine.Serialization;

[DefaultExecutionOrder(-100)]
public class AudioManager : MonoBehaviour
{
    public static AudioManager instance { get; private set; }
    [Header("Audio")]
    [FormerlySerializedAs("audio")]
    [SerializeField] public AudioClip sound;
    [Tooltip("Baked from the original music. Defaults to Resources/<clip name>Beat.")]
    [SerializeField] private TextAsset beatAnalysis;
    [Header("Beat detection")]
    [SerializeField, Range(0f, 1f)] private float sensitivity = 0.12f;
    [SerializeField] private float minBeatInterval = 0.25f;
    [Header("Bass response (baked 40–180 Hz)")]
    [SerializeField, Min(0f)] private float bassSensitivity = 30f;
    public event Action OnBeat;
    private AudioSource musicSource;
    private float[] analysis;
    private float analysisRate;
    private int analysisFrameCount;
    private float previousLeftBassWave;
    private float previousRightBassWave;
    private float lastBeatTime;
    public float RmsLeft { get; private set; }
    public float RmsRight { get; private set; }
    public float BassLevelLeft { get; private set; }
    public float BassLevelRight { get; private set; }
    public float BassWaveLeft { get; private set; }
    public float BassWaveRight { get; private set; }
    public float StereoBassLevel => Mathf.Max(BassLevelLeft, BassLevelRight);
    public AudioSource MusicSource => musicSource;

    private void Awake()
    {
        instance = this;
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.spatialBlend = 0f;
        musicSource.clip = sound;
        if (beatAnalysis == null && sound != null)
            beatAnalysis = Resources.Load<TextAsset>(sound.name + "Beat");
        if (beatAnalysis == null)
        {
            Debug.LogWarning("No beat data. Select the music clip and use Tools/Audio/Bake Selected Music Beat Data.", this);
            return;
        }
        using (var reader = new BinaryReader(new MemoryStream(beatAnalysis.bytes)))
        {
            if (reader.ReadInt32() != 0x42505431) throw new InvalidDataException("Invalid beat analysis format");
            analysisRate = reader.ReadSingle();
            analysisFrameCount = reader.ReadInt32();
            if (analysisRate <= 0 || analysisFrameCount <= 0 ||
                reader.BaseStream.Length != 12L + analysisFrameCount * 24L)
                throw new InvalidDataException("Invalid beat analysis length");
            analysis = new float[analysisFrameCount * 6];
            for (int i = 0; i < analysis.Length; i++) analysis[i] = reader.ReadSingle();
        }
    }

    private void Update()
    {
        float seconds;
        var clip = musicSource != null ? musicSource.clip : null;
        if (musicSource != null && clip != null && clip.frequency > 0 && musicSource.isPlaying)
            seconds = musicSource.timeSamples / (float)clip.frequency;
        else
        {
            var emitter = GameManager.Instance != null ? GameManager.Instance.LevelMusic : null;
            if (emitter == null || !emitter.IsPlaying() ||
                emitter.EventInstance.getTimelinePosition(out int milliseconds) != FMOD.RESULT.OK)
            {
                ResetSignal();
                return;
            }
            seconds = milliseconds / 1000f;
        }
        SampleAnalysis(seconds);
        DetectBeat();
    }

    // This path reads only the baked original signal; playback volume and mute never enter it.
    private void SampleAnalysis(float seconds)
    {
        if (analysis == null) { ResetSignal(); return; }
        float frame = Mathf.Clamp(seconds * analysisRate, 0, analysisFrameCount - 1);
        int first = Mathf.FloorToInt(frame);
        int next = Mathf.Min(first + 1, analysisFrameCount - 1);
        float blend = frame - first;
        RmsLeft = ReadChannel(first, next, blend, 0);
        RmsRight = ReadChannel(first, next, blend, 1);
        BassLevelLeft = CompressBass(ReadChannel(first, next, blend, 2));
        BassLevelRight = CompressBass(ReadChannel(first, next, blend, 3));
        BassWaveLeft = CompressBass(ReadChannel(first, next, blend, 4));
        BassWaveRight = CompressBass(ReadChannel(first, next, blend, 5));
    }

    private float ReadChannel(int first, int next, float blend, int channel) =>
        Mathf.Lerp(analysis[first * 6 + channel], analysis[next * 6 + channel], blend);

    private void DetectBeat()
    {
        float leftIncrease = BassWaveLeft - previousLeftBassWave;
        float rightIncrease = BassWaveRight - previousRightBassWave;
        float leftThreshold = sensitivity * Mathf.Max(previousLeftBassWave, 0.1f);
        float rightThreshold = sensitivity * Mathf.Max(previousRightBassWave, 0.1f);
        bool leftBeat = leftIncrease > leftThreshold && BassWaveLeft > 0.05f;
        bool rightBeat = rightIncrease > rightThreshold && BassWaveRight > 0.05f;
        previousLeftBassWave = BassWaveLeft;
        previousRightBassWave = BassWaveRight;
        if ((leftBeat || rightBeat) && Time.time - lastBeatTime >= minBeatInterval)
        {
            lastBeatTime = Time.time;
            OnBeat?.Invoke();
        }
    }

    private float CompressBass(float rawLevel)
    {
        float scaled = Mathf.Max(0f, rawLevel) * bassSensitivity;
        return Mathf.Clamp01(Mathf.Log(1f + scaled) / Mathf.Log(1f + Mathf.Max(1f, bassSensitivity * 4f)));
    }

    private void ResetSignal()
    {
        RmsLeft = RmsRight = 0f;
        BassLevelLeft = BassLevelRight = BassWaveLeft = BassWaveRight = 0f;
        previousLeftBassWave = previousRightBassWave = 0f;
    }

    public void PlayMusic()
    {
        if (musicSource != null && musicSource.clip != null && !musicSource.isPlaying)
            musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource != null) musicSource.Stop();
        ResetSignal();
        lastBeatTime = 0f;
    }

    private void OnDestroy()
    {
        StopMusic();
        if (instance == this) instance = null;
    }
}
