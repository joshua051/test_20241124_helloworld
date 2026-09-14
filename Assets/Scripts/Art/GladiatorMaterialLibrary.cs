using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace IronSand.Art
{
    public static class GladiatorMaterialLibrary
    {
        public const string ResourcePath = "Gladiators/RuntimeMaterial";
        public static string ExpectedShaderName()
        {
            RenderPipelineAsset pipeline = GraphicsSettings.currentRenderPipeline;
            if (pipeline == null) return "Standard";
            if (pipeline.GetType().FullName == "UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset")
                return "Universal Render Pipeline/Lit";
            throw new InvalidOperationException("Gladiator supports Built-in or URP; this render pipeline is not supported.");
        }
        public static Material RequireTemplate()
        {
            Material template = Resources.Load<Material>(ResourcePath);
            if (template == null || template.shader == null || template.shader.name != ExpectedShaderName())
                throw new InvalidOperationException("Missing or incompatible gladiator RuntimeMaterial. Rebuild the prototype arena before Play/Build.");
            return template;
        }
    }
}
