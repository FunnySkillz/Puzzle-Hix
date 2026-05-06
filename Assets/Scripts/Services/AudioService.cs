using System.Collections.Generic;
using UnityEngine;

namespace PuzzleDungeon.Services
{
    [DisallowMultipleComponent]
    public class AudioService : MonoBehaviour
    {
        [SerializeField] private bool useProceduralFallback = true;
        [SerializeField] private float masterVolume = 0.55f;
        [SerializeField] private AudioClip buttonClickClip;
        [SerializeField] private AudioClip swapClip;
        [SerializeField] private AudioClip invalidSwapClip;
        [SerializeField] private AudioClip matchClearClip;
        [SerializeField] private AudioClip cascadeClip;
        [SerializeField] private AudioClip specialCreateClip;
        [SerializeField] private AudioClip specialActivateClip;
        [SerializeField] private AudioClip winClip;
        [SerializeField] private AudioClip failClip;

        private readonly Dictionary<AudioCue, AudioClip> proceduralClips = new Dictionary<AudioCue, AudioClip>();
        private AudioSource audioSource;

        public static AudioService GetOrCreate()
        {
            AudioService service = FindObjectOfType<AudioService>();

            if (service != null)
            {
                return service;
            }

            GameObject serviceObject = new GameObject("AudioService", typeof(AudioService));
            return serviceObject.GetComponent<AudioService>();
        }

        public static void PlayGlobal(AudioCue cue)
        {
            GetOrCreate().Play(cue);
        }

        public void Play(AudioCue cue)
        {
            EnsureAudioSource();
            AudioClip clip = GetAssignedClip(cue);

            if (clip == null && useProceduralFallback)
            {
                clip = GetProceduralClip(cue);
            }

            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip, Mathf.Clamp01(masterVolume));
            }
        }

        private void Awake()
        {
            EnsureAudioSource();
        }

        private void EnsureAudioSource()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();

                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }

                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f;
            }
        }

        private AudioClip GetAssignedClip(AudioCue cue)
        {
            switch (cue)
            {
                case AudioCue.ButtonClick:
                    return buttonClickClip;
                case AudioCue.Swap:
                    return swapClip;
                case AudioCue.InvalidSwap:
                    return invalidSwapClip;
                case AudioCue.MatchClear:
                    return matchClearClip;
                case AudioCue.Cascade:
                    return cascadeClip;
                case AudioCue.SpecialCreate:
                    return specialCreateClip;
                case AudioCue.SpecialActivate:
                    return specialActivateClip;
                case AudioCue.Win:
                    return winClip;
                case AudioCue.Fail:
                    return failClip;
                default:
                    return null;
            }
        }

        private AudioClip GetProceduralClip(AudioCue cue)
        {
            if (proceduralClips.TryGetValue(cue, out AudioClip clip))
            {
                return clip;
            }

            float frequency;
            float duration;

            switch (cue)
            {
                case AudioCue.ButtonClick:
                    frequency = 560f;
                    duration = 0.055f;
                    break;
                case AudioCue.Swap:
                    frequency = 440f;
                    duration = 0.075f;
                    break;
                case AudioCue.InvalidSwap:
                    frequency = 180f;
                    duration = 0.11f;
                    break;
                case AudioCue.MatchClear:
                    frequency = 720f;
                    duration = 0.12f;
                    break;
                case AudioCue.Cascade:
                    frequency = 880f;
                    duration = 0.13f;
                    break;
                case AudioCue.SpecialCreate:
                    frequency = 1040f;
                    duration = 0.16f;
                    break;
                case AudioCue.SpecialActivate:
                    frequency = 620f;
                    duration = 0.20f;
                    break;
                case AudioCue.Win:
                    frequency = 980f;
                    duration = 0.24f;
                    break;
                case AudioCue.Fail:
                    frequency = 150f;
                    duration = 0.22f;
                    break;
                default:
                    frequency = 440f;
                    duration = 0.08f;
                    break;
            }

            clip = CreateTone(cue.ToString(), frequency, duration);
            proceduralClips[cue] = clip;
            return clip;
        }

        private static AudioClip CreateTone(string name, float frequency, float duration)
        {
            const int sampleRate = 44100;
            int sampleCount = Mathf.Max(1, Mathf.CeilToInt(sampleRate * Mathf.Max(0.01f, duration)));
            float[] data = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)sampleRate;
                float envelope = 1f - (i / (float)sampleCount);
                data[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * 0.18f;
            }

            AudioClip clip = AudioClip.Create($"Procedural_{name}", sampleCount, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
