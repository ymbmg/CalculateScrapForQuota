using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;
using P = CalculateScrapForQuota.Plugin;

namespace CalculateScrapForQuota.Scripts
{
public static class Outliner
    {
        private static Shader HDRP_LIT_SHADER = Shader.Find("HDRP/Lit");
        private static Shader STANDARD_SHADER = Shader.Find("Standard");
        
        public static Color Color = Color.green;
        public static float Transparency = 0.5f;
        public static float Scale = 1.05f;
        
        public static bool IsShowingOutline = false;

        private static Dictionary<GameObject, GameObject> pool = new();

        public static void Add(GameObject gameObject)
        {
            if (pool.ContainsKey(gameObject))
                return;
            var visualsGameObject = CloneVisualsOnly(gameObject);
            pool.Add(gameObject, visualsGameObject);
            ConvertVisualsToOutlineVisuals(visualsGameObject);
            visualsGameObject.SetActive(IsShowingOutline);
        }

        public static void Show()
        {
            IsShowingOutline = true;
            foreach (var go in pool.Values) 
                go.SetActive(true);
        }

        public static void Hide()
        {
            IsShowingOutline = false;
            foreach (var go in pool.Values)
                go.SetActive(false);
        }

        public static void Clear()
        {
            IsShowingOutline = false;
            foreach (var go in pool)
            {
                try
                {
                    Object.Destroy(go.Value);
                }
                catch
                {
                    // ignored
                }
            }
            pool.Clear();
        }
        
        private static void ConvertVisualsToOutlineVisuals(GameObject obj)
        {
            // Scale the duplicated object create the outline effect
            var scale = obj.transform.localScale;
            scale.x *= Scale;
            scale.y *= Scale;
            scale.z *= Scale;
            obj.transform.localScale = scale;
            
            var renderers = obj.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                for (var i = 0; i < renderer.materials.Length; i++)
                {
                    var material = renderer.materials[i];
                    if (HDRP_LIT_SHADER)
                    {
                        material.shader = HDRP_LIT_SHADER;
                        // Set material to transparent
                        material.SetFloat("_SurfaceType", 1); // 0 is Opaque, 1 is Transparent
                        
                        // Blend Mode for HDRP - Alpha blending
                        // Note: HDRP uses different numeric values. You may need to adjust these based on your specific needs.
                        material.SetFloat("_BlendMode", 0); // 0 is Alpha, 1 is Additive, etc.

                        // Additional transparency settings
                        material.SetFloat("_AlphaCutoffEnable", 0); // Disable alpha cutoff
                        material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        material.SetFloat("_ZWrite", 0); // Disable ZWrite for transparency
                        material.SetFloat("_TransparentZWrite", 0);
                        material.SetFloat("_TransparentCullMode", (float)CullMode.Off);
                        material.SetFloat("_TransparentSortPriority", 0); // Adjust if needed
                        material.SetFloat("_CullModeForward", (float)CullMode.Off);
                    }
                    else if (STANDARD_SHADER)
                    {
                        material.shader = STANDARD_SHADER;
                        material.SetFloat("_Mode", 3);
                        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        material.SetInt("_ZWrite", 0);
                        material.DisableKeyword("_ALPHATEST_ON");
                        material.EnableKeyword("_ALPHABLEND_ON");
                        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        material.renderQueue = 3000;
                    }

                    material.color = new(Color.r, Color.g, Color.b, Transparency);
                    P.Log($"Using {material.shader.name}");
                }
            }
        }

        private static GameObject CloneVisualsOnly(GameObject original)
        {
            var clonedVisualsParent = new GameObject($"{original.name} (ClonedVisuals)");
            clonedVisualsParent.transform.SetPositionAndRotation(original.transform.position, original.transform.rotation);
            clonedVisualsParent.transform.localScale = original.transform.lossyScale;
            clonedVisualsParent.transform.SetParent(original.transform);
        
            var renderers = original.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                var newVisualClone = new GameObject($"{renderer.gameObject.name} ClonedVisual");
                newVisualClone.transform.SetPositionAndRotation(renderer.transform.position, renderer.transform.rotation);
                newVisualClone.transform.localScale = renderer.transform.lossyScale;
                newVisualClone.transform.SetParent(clonedVisualsParent.transform);

                var originalMeshFilter = renderer.GetComponent<MeshFilter>();
                var copyMeshFilter = newVisualClone.AddComponent<MeshFilter>();
                copyMeshFilter.sharedMesh = originalMeshFilter.sharedMesh;
                copyMeshFilter.mesh = originalMeshFilter.mesh;

                var originalMeshRenderer = renderer.GetComponent<MeshRenderer>();
                if (originalMeshRenderer != null)
                {
                    var copyRenderer = newVisualClone.AddComponent<MeshRenderer>();
                    copyRenderer.materials = renderer.materials;
                }
                else
                {
                    var copyRenderer = newVisualClone.AddComponent<SkinnedMeshRenderer>();
                    copyRenderer.materials = renderer.materials;
                }
            }
            
            P.Log($"Made {clonedVisualsParent.name}");
            return clonedVisualsParent;
        }
    }
}
