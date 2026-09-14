using IronSand.Player;
using UnityEngine;

namespace IronSand.Combat
{
    public sealed class CombatFeedbackSystem : MonoBehaviour
    {
        private static CombatFeedbackSystem instance;
        private AudioSource audioSource;
        private AudioClip lightImpact;
        private AudioClip heavyImpact;
        private AudioClip guardImpact;
        private AudioClip executionImpact;
        private void Awake()
        {
            instance = this;
            audioSource = gameObject.AddComponent<AudioSource>(); audioSource.playOnAwake = false; audioSource.spatialBlend = 0f;
            lightImpact = BuildClip("LightImpact", 0.065f, 170f, 0.18f, 17);
            heavyImpact = BuildClip("HeavyImpact", 0.10f, 88f, 0.28f, 23);
            guardImpact = BuildClip("GuardImpact", 0.09f, 420f, 0.20f, 31);
            executionImpact = BuildClip("ExecutionImpact", 0.14f, 64f, 0.34f, 47);
        }
        private void OnDestroy() { if (instance == this) instance = null; }
        public static void EmitImpact(Vector3 position, float hitStopSeconds, float cameraShake, bool heavy)
        {
            CombatFreezeSystem.Request(hitStopSeconds);
            if (Camera.main != null) Camera.main.GetComponent<ThirdPersonArenaCamera>()?.AddImpulse(cameraShake, heavy ? 0.16f : 0.10f);
            instance?.SpawnFlash(position, heavy ? 0.18f : 0.12f, heavy ? 0.14f : 0.10f);
            instance?.Play(heavy ? instance.heavyImpact : instance.lightImpact, heavy ? 0.95f : 0.72f);
        }
        public static void EmitPerfectGuard(Vector3 position)
        {
            CombatFreezeSystem.Request(0.085f);
            if (Camera.main != null) Camera.main.GetComponent<ThirdPersonArenaCamera>()?.AddImpulse(0.18f, 0.12f);
            instance?.SpawnFlash(position, 0.22f, 0.13f); instance?.Play(instance.guardImpact, 0.9f);
        }
        public static void EmitExecution(Vector3 position)
        {
            CombatFreezeSystem.Request(0.12f);
            if (Camera.main != null) Camera.main.GetComponent<ThirdPersonArenaCamera>()?.AddImpulse(0.34f, 0.24f);
            instance?.SpawnFlash(position, 0.30f, 0.18f); instance?.Play(instance.executionImpact, 1f);
        }
        private void SpawnFlash(Vector3 position, float size, float seconds)
        {
            GameObject flash = GameObject.CreatePrimitive(PrimitiveType.Sphere); flash.name = "ImpactFlash"; flash.transform.position = position;
            Collider collider = flash.GetComponent<Collider>(); if (collider != null) Destroy(collider);
            flash.AddComponent<TransientImpactVfx>().Configure(seconds, size);
        }
        private void Play(AudioClip clip, float volume) { if (audioSource != null && clip != null) audioSource.PlayOneShot(clip, Mathf.Clamp01(volume)); }
        private static AudioClip BuildClip(string name, float seconds, float frequency, float noiseLevel, int seed)
        {
            const int sampleRate = 44100; int samples = Mathf.Max(64, Mathf.CeilToInt(sampleRate * seconds)); float[] data = new float[samples]; var random = new System.Random(seed);
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)sampleRate; float envelope = 1f - i / (float)samples;
                float tone = Mathf.Sin(Mathf.PI * 2f * frequency * t) * 0.35f; float noise = ((float)random.NextDouble() * 2f - 1f) * noiseLevel;
                data[i] = (tone + noise) * envelope;
            }
            AudioClip clip = AudioClip.Create(name, samples, 1, sampleRate, false); clip.SetData(data, 0); return clip;
        }
    }
}
