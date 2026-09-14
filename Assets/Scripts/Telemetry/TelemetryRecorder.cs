using System;
using System.Globalization;
using System.IO;
using IronSand.Combat;
using UnityEngine;

namespace IronSand.Telemetry
{
    [DefaultExecutionOrder(-900)]
    public sealed class TelemetryRecorder : MonoBehaviour
    {
        private static TelemetryRecorder instance;
        private StreamWriter writer;
        private string sessionId;
        private float startedAt;
        private float flushClock;
        private int frames;
        private float frameSeconds;

        public static string CurrentSessionId => instance != null ? instance.sessionId : null;
        public static string OutputPath => instance != null ? instance.outputPath : null;
        private string outputPath;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            sessionId = Guid.NewGuid().ToString("N");
            startedAt = Time.realtimeSinceStartup;
            string directory = Path.Combine(Application.persistentDataPath, "IronSandTelemetry");
            Directory.CreateDirectory(directory);
            outputPath = Path.Combine(directory, $"combat-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{sessionId}.jsonl");
            writer = new StreamWriter(outputPath, append: false) { AutoFlush = false };
            WriteRaw("{\"record_type\":\"session_start\",\"session_id\":\"" + sessionId +
                     "\",\"timestamp\":\"" + DateTime.UtcNow.ToString("O") +
                     "\",\"commit_sha\":\"runtime-local\",\"platform\":\"" + Escape(Application.platform.ToString()) +
                     "\",\"build_kind\":\"" + (Debug.isDebugBuild ? "development" : "release") +
                     "\",\"unity_version\":\"" + Escape(Application.unityVersion) + "\"}");
            EmitBalanceSnapshot();
            Debug.Log($"Iron Sand telemetry: {outputPath}");
        }

        private void Update()
        {
            if (Time.unscaledDeltaTime > 0f)
            {
                frames++;
                frameSeconds += Time.unscaledDeltaTime;
            }
            flushClock += Time.unscaledDeltaTime;
            if (flushClock >= 1f)
            {
                flushClock = 0f;
                writer?.Flush();
            }
        }

        private void OnApplicationQuit() => EndSession("application_quit");

        private void OnDestroy()
        {
            if (instance != this) return;
            EndSession("destroyed");
            instance = null;
        }

        public static void RecordEvent(string eventType, string actor = null, string target = null,
            WeaponArchetype? weapon = null, AttackKind? attackKind = null, float? damage = null,
            float? poiseDamage = null, Vector3? position = null, string payloadJson = null)
        {
            if (instance == null || instance.writer == null || string.IsNullOrWhiteSpace(eventType)) return;
            Vector3 pos = position ?? Vector3.zero;
            string payload = string.IsNullOrWhiteSpace(payloadJson) ? "{}" : payloadJson;
            instance.WriteRaw("{\"record_type\":\"combat_event\",\"session_id\":\"" + instance.sessionId +
                              "\",\"ts_seconds\":" + F(Time.realtimeSinceStartup - instance.startedAt) +
                              ",\"event_type\":\"" + Escape(eventType) + "\"" +
                              Optional("actor", actor) + Optional("target", target) +
                              (weapon.HasValue ? ",\"weapon\":\"" + weapon.Value + "\"" : "") +
                              (attackKind.HasValue ? ",\"attack_kind\":\"" + attackKind.Value + "\"" : "") +
                              (damage.HasValue ? ",\"damage\":" + F(damage.Value) : "") +
                              (poiseDamage.HasValue ? ",\"poise_damage\":" + F(poiseDamage.Value) : "") +
                              ",\"x\":" + F(pos.x) + ",\"y\":" + F(pos.y) + ",\"z\":" + F(pos.z) +
                              ",\"payload\":" + payload + "}");
        }

        public static void RecordParameter(string system, string entity, string parameter, float value, string unit = null)
        {
            if (instance == null) return;
            instance.WriteRaw("{\"record_type\":\"balance_parameter\",\"commit_sha\":\"runtime-local\",\"timestamp\":\"" +
                              DateTime.UtcNow.ToString("O") + "\",\"system\":\"" + Escape(system) +
                              "\",\"entity\":\"" + Escape(entity) + "\",\"parameter\":\"" + Escape(parameter) +
                              "\",\"value\":" + F(value) + (unit == null ? "" : ",\"unit\":\"" + Escape(unit) + "\"") + "}");
        }

        private void EmitBalanceSnapshot()
        {
            foreach (WeaponArchetype weapon in Enum.GetValues(typeof(WeaponArchetype)))
            {
                WeaponStats w = WeaponCatalog.Get(weapon);
                string e = weapon.ToString();
                RecordParameter("weapon", e, "light_damage_multiplier", w.LightDamageMultiplier, "ratio");
                RecordParameter("weapon", e, "heavy_damage_multiplier", w.HeavyDamageMultiplier, "ratio");
                RecordParameter("weapon", e, "reach_multiplier", w.ReachMultiplier, "ratio");
                RecordParameter("weapon", e, "cooldown_multiplier", w.CooldownMultiplier, "ratio");
                RecordParameter("weapon", e, "stun_multiplier", w.StunMultiplier, "ratio");
                RecordParameter("weapon", e, "max_durability", w.MaxDurability, "hits");
                RecordParameter("weapon", e, "favor_bonus", w.FavorBonus, "points");
                foreach (AttackKind kind in new[] { AttackKind.Light, AttackKind.Heavy })
                {
                    for (int chain = 0; chain < 3; chain++)
                    {
                        AttackProfile a = AttackLibrary.Get(weapon, kind, chain);
                        string attack = $"{weapon}.{kind}.chain{chain}";
                        RecordParameter("attack", attack, "startup", a.Startup, "seconds");
                        RecordParameter("attack", attack, "active", a.Active, "seconds");
                        RecordParameter("attack", attack, "recovery", a.Recovery, "seconds");
                        RecordParameter("attack", attack, "damage_multiplier", a.DamageMultiplier, "ratio");
                        RecordParameter("attack", attack, "poise_damage", a.PoiseDamage, "points");
                        RecordParameter("attack", attack, "sweep_radius", a.SweepRadius, "meters");
                        RecordParameter("attack", attack, "root_motion_distance", a.RootMotionDistance, "meters");
                        RecordParameter("attack", attack, "hit_stop", a.HitStopSeconds, "seconds");
                        RecordParameter("attack", attack, "camera_shake", a.CameraShake, "intensity");
                    }
                }
            }
        }

        private void EndSession(string outcome)
        {
            if (writer == null) return;
            float duration = Mathf.Max(0f, Time.realtimeSinceStartup - startedAt);
            float fps = frameSeconds > 0f ? frames / frameSeconds : 0f;
            WriteRaw("{\"record_type\":\"session_end\",\"session_id\":\"" + sessionId +
                     "\",\"timestamp\":\"" + DateTime.UtcNow.ToString("O") +
                     "\",\"outcome\":\"" + Escape(outcome) + "\",\"duration_seconds\":" + F(duration) +
                     ",\"fps_avg\":" + F(fps) + "}");
            writer.Flush();
            writer.Dispose();
            writer = null;
        }

        private void WriteRaw(string json) => writer?.WriteLine(json);
        private static string F(float value) => value.ToString("0.######", CultureInfo.InvariantCulture);
        private static string Escape(string value) => (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n");
        private static string Optional(string key, string value) => value == null ? "" : ",\"" + key + "\":\"" + Escape(value) + "\"";
    }
}
