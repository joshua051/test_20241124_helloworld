using IronSand.Arena;
using IronSand.Player;
using IronSand.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IronSand.Editor
{
    public static class PrototypeBuilder
    {
        private const string ScenePath = "Assets/Scenes/ArenaPrototype.unity";

        [MenuItem("Tools/Iron Sand Arena/Rebuild Prototype Arena")]
        public static void RebuildPrototypeArena()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            BuildScene();
        }

        private static void BuildScene()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateLighting();
            CreateArenaGeometry();
            PlayerGladiator player = CreatePlayer();
            CreateCamera(player.transform);

            GameObject systems = new("ArenaSystems");
            systems.AddComponent<CrowdFavorSystem>();
            systems.AddComponent<ArenaDirector>();
            systems.AddComponent<PrototypeHUD>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        private static void CreateLighting()
        {
            GameObject lightObject = new("Sun");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.25f;
            lightObject.transform.rotation = Quaternion.Euler(45f, -35f, 0f);
        }

        private static void CreateArenaGeometry()
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            floor.name = "ArenaFloor";
            floor.transform.position = new Vector3(0f, -0.5f, 0f);
            floor.transform.localScale = new Vector3(14f, 0.5f, 14f);

            const int wallCount = 24;
            const float radius = 14.5f;
            for (int i = 0; i < wallCount; i++)
            {
                float angle = Mathf.PI * 2f * i / wallCount;
                GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.name = $"ArenaWall_{i:00}";
                wall.transform.position = new Vector3(Mathf.Cos(angle) * radius, 1.4f, Mathf.Sin(angle) * radius);
                wall.transform.rotation = Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 0f);
                wall.transform.localScale = new Vector3(3.9f, 3f, 0.8f);
            }

            for (int i = 0; i < 8; i++)
            {
                float angle = Mathf.PI * 2f * i / 8f;
                GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pillar.name = $"Pillar_{i:00}";
                pillar.transform.position = new Vector3(Mathf.Cos(angle) * 12.2f, 1.3f, Mathf.Sin(angle) * 12.2f);
                pillar.transform.localScale = new Vector3(0.55f, 1.8f, 0.55f);
            }
        }

        private static PlayerGladiator CreatePlayer()
        {
            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            root.name = "PlayerGladiator";
            root.transform.position = new Vector3(0f, 1f, -3f);

            Object.DestroyImmediate(root.GetComponent<Collider>());
            CharacterController controller = root.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.radius = 0.45f;
            controller.center = Vector3.zero;

            return root.AddComponent<PlayerGladiator>();
        }

        private static void CreateCamera(Transform target)
        {
            GameObject cameraObject = new("Main Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.nearClipPlane = 0.05f;
            cameraObject.transform.position = new Vector3(0f, 4f, -9f);

            ThirdPersonArenaCamera follow = cameraObject.AddComponent<ThirdPersonArenaCamera>();
            follow.SetTarget(target);
        }
    }
}
