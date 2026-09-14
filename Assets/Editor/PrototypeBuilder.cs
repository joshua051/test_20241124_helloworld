using System;
using System.Collections.Generic;
using IronSand.Arena;
using IronSand.Combat;
using IronSand.Player;
using IronSand.Scoring;
using IronSand.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace IronSand.Editor
{
    public static class PrototypeBuilder
    {
        public const string ScenePath = "Assets/Scenes/ArenaPrototype.unity";
        [MenuItem("Tools/Iron Sand Arena/Rebuild Prototype Arena")]
        public static void RebuildPrototypeArena()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("Exit Play Mode before rebuilding the arena."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null && !EditorUtility.DisplayDialog("Rebuild generated arena?", "This replaces ArenaPrototype.unity. Save a copy first if you edited this generated scene.", "Rebuild", "Cancel")) return;
            BuildScene();
        }
        public static void BuildForValidation() { if (!Application.isBatchMode) throw new InvalidOperationException("BuildForValidation is for Unity -batchmode only."); BuildScene(); }
        private static void BuildScene()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); CreateLighting(); ArenaGeometry.Create();
            WeaponPickup.Spawn(new Vector3(-4f, 0.35f, 0f), WeaponArchetype.Axe); WeaponPickup.Spawn(new Vector3(4f, 0.35f, 0f), WeaponArchetype.Spear); WeaponPickup.Spawn(new Vector3(0f, 0.35f, 4f), WeaponArchetype.Mace);
            PlayerGladiator player = CreatePlayer(); CreateCamera(player.transform); GameObject systems = new("ArenaSystems");
            systems.AddComponent<CombatFreezeSystem>(); systems.AddComponent<CombatFeedbackSystem>(); systems.AddComponent<CrowdFavorSystem>(); systems.AddComponent<CombatStyleSystem>(); systems.AddComponent<ArenaDirector>(); systems.AddComponent<CrowdRewardDirector>(); systems.AddComponent<ArenaSession>(); systems.AddComponent<PrototypeHUD>();
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new InvalidOperationException("Could not save generated arena scene.");
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes); int index = scenes.FindIndex(entry => entry.path == ScenePath); if (index >= 0) scenes[index] = new EditorBuildSettingsScene(ScenePath, true); else scenes.Add(new EditorBuildSettingsScene(ScenePath, true)); EditorBuildSettings.scenes = scenes.ToArray(); AssetDatabase.SaveAssets();
        }
        private static void CreateLighting() { GameObject lightObject = new("Sun"); Light light = lightObject.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.25f; lightObject.transform.rotation = Quaternion.Euler(45f, -35f, 0f); }
        private static PlayerGladiator CreatePlayer()
        {
            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Capsule); root.name = "PlayerGladiator"; root.transform.position = new Vector3(0f, 1.1f, -3f); Object.DestroyImmediate(root.GetComponent<Collider>());
            CharacterController controller = root.AddComponent<CharacterController>(); controller.height = 2f; controller.radius = 0.45f; controller.center = Vector3.zero; return root.AddComponent<PlayerGladiator>();
        }
        private static void CreateCamera(Transform target)
        {
            GameObject cameraObject = new("Main Camera"); Camera camera = cameraObject.AddComponent<Camera>(); camera.tag = "MainCamera"; camera.nearClipPlane = 0.05f; cameraObject.AddComponent<AudioListener>(); cameraObject.transform.position = new Vector3(0f, 4f, -9f); cameraObject.AddComponent<ThirdPersonArenaCamera>().SetTarget(target);
        }
    }
}
