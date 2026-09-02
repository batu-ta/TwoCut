using UnityEngine;

namespace TwoCutGame
{
    /// <summary>
    /// TwoCut Audio Manager.
    /// Provides procedural sound effects (Scissors Snip, Cash Cha-Ching, Customer Bell, Sweep, Dash)
    /// without requiring external audio asset files. Auto-initializes if not present in scene.
    /// </summary>
    public class TwoCutAudioManager : MonoBehaviour
    {
        private static TwoCutAudioManager _instance;
        public static TwoCutAudioManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<TwoCutAudioManager>();
                    if (_instance == null)
                    {
                        GameObject audioObj = new GameObject("TwoCutAudioManager");
                        _instance = audioObj.AddComponent<TwoCutAudioManager>();
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        private AudioSource sfxSource;

        private AudioClip scissorsClip;
        private AudioClip cashClip;
        private AudioClip bellClip;
        private AudioClip sweepClip;
        private AudioClip dashClip;
        private AudioClip cheerClip;
        private AudioClip popClip;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeAudio();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void InitializeAudio()
        {
            sfxSource = gameObject.GetComponent<AudioSource>();
            if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f; // 2D Sound

            // Generate procedural audio clips
            scissorsClip = GenerateScissorsClip();
            cashClip = GenerateCashClip();
            bellClip = GenerateBellClip();
            sweepClip = GenerateSweepClip();
            dashClip = GenerateDashClip();
            cheerClip = GenerateCheerClip();
            popClip = GeneratePopClip();
        }

        public void PlayScissors() => PlaySound(scissorsClip, 0.9f, Random.Range(0.95f, 1.15f));
        public void PlayCash() => PlaySound(cashClip, 1.0f, 1.0f);
        public void PlayCustomerBell() => PlaySound(bellClip, 0.8f, 1.0f);
        public void PlaySweep() => PlaySound(sweepClip, 0.7f, Random.Range(0.9f, 1.1f));
        public void PlayDash() => PlaySound(dashClip, 0.8f, 1.2f);
        public void PlayCheer() => PlaySound(cheerClip, 0.9f, 1.0f);
        public void PlayPop() => PlaySound(popClip, 0.8f, Random.Range(1.0f, 1.3f));

        private void PlaySound(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            if (clip == null) return;
            if (sfxSource == null) InitializeAudio();
            if (sfxSource == null) return;
            sfxSource.pitch = pitch;
            sfxSource.PlayOneShot(clip, volume);
        }

        #region Procedural Audio Generators

        private AudioClip GenerateScissorsClip()
        {
            int sampleRate = 44100;
            float duration = 0.12f;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Exp(-t * 35f);
                float noise = (Random.value * 2f - 1f) * 0.7f;
                float highTone = Mathf.Sin(2f * Mathf.PI * 2800f * t) * 0.3f;
                samples[i] = (noise + highTone) * envelope;
            }

            AudioClip clip = AudioClip.Create("ScissorsSFX", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GenerateCashClip()
        {
            int sampleRate = 44100;
            float duration = 0.35f;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Exp(-t * 10f);
                float tone1 = Mathf.Sin(2f * Mathf.PI * 1975f * t) * 0.5f;
                float tone2 = Mathf.Sin(2f * Mathf.PI * 3136f * t) * 0.5f;
                samples[i] = (tone1 + tone2) * envelope;
            }

            AudioClip clip = AudioClip.Create("CashSFX", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GenerateBellClip()
        {
            int sampleRate = 44100;
            float duration = 0.4f;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Exp(-t * 7f);
                float tone = Mathf.Sin(2f * Mathf.PI * 1320f * t) * 0.8f;
                samples[i] = tone * envelope;
            }

            AudioClip clip = AudioClip.Create("BellSFX", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GenerateSweepClip()
        {
            int sampleRate = 44100;
            float duration = 0.2f;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Sin(Mathf.PI * (t / duration));
                float noise = (Random.value * 2f - 1f) * 0.8f;
                samples[i] = noise * envelope;
            }

            AudioClip clip = AudioClip.Create("SweepSFX", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GenerateDashClip()
        {
            int sampleRate = 44100;
            float duration = 0.25f;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Sin(Mathf.PI * (t / duration));
                float freq = Mathf.Lerp(600f, 200f, t / duration);
                float tone = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.5f;
                float noise = (Random.value * 2f - 1f) * 0.5f;
                samples[i] = (tone + noise) * envelope;
            }

            AudioClip clip = AudioClip.Create("DashSFX", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GenerateCheerClip()
        {
            int sampleRate = 44100;
            float duration = 0.3f;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Exp(-t * 8f);
                float tone = Mathf.Sin(2f * Mathf.PI * 880f * t) * 0.6f + Mathf.Sin(2f * Mathf.PI * 1174f * t) * 0.4f;
                samples[i] = tone * envelope;
            }

            AudioClip clip = AudioClip.Create("CheerSFX", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GeneratePopClip()
        {
            int sampleRate = 44100;
            float duration = 0.08f;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Exp(-t * 50f);
                float freq = Mathf.Lerp(400f, 900f, t / duration);
                samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope;
            }

            AudioClip clip = AudioClip.Create("PopSFX", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        #endregion
    }
}
