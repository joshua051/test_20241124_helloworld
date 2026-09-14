using System;
using IronSand.Art;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace IronSand.Editor
{
    // Only explicit builders create the asset; imports/domain reloads never mutate a scene.
    public sealed class GladiatorBuildPreflight : IPreprocessBuildWithReport
    {
        public const string MaterialPath = "Assets/Resources/Gladiators/RuntimeMaterial.mat";
        public int callbackOrder => 0;
        public static void Prepare()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Gladiators") ||
                AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Resources/Gladiators/Gladiator.json") == null)
                throw new InvalidOperationException("The committed Gladiators Resources data is missing.");
            string shaderName = GladiatorMaterialLibrary.ExpectedShaderName();
            Shader shader = Shader.Find(shaderName); // Editor only; runtime keeps an asset reference.
            if (shader == null) throw new InvalidOperationException("Cannot find editor shader: " + shaderName);
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (existing == null)
            {
                if (AssetDatabase.LoadMainAssetAtPath(MaterialPath) != null)
                    throw new InvalidOperationException("RuntimeMaterial path is occupied by a non-material asset.");
                AssetDatabase.CreateAsset(new Material(shader) { name = "IronSand_RuntimeMaterial" }, MaterialPath);
            }
            else if (existing.shader != shader)
            {
                existing.shader = shader;
                EditorUtility.SetDirty(existing);
            }
            AssetDatabase.SaveAssets();
        }
        public void OnPreprocessBuild(BuildReport report)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null || material.shader == null ||
                material.shader.name != GladiatorMaterialLibrary.ExpectedShaderName())
                throw new BuildFailedException("Rebuild the prototype arena to prepare its referenced runtime material.");
        }
    }
}
