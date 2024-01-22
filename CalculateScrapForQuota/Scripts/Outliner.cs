using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;
using P = CalculateScrapForQuota.Plugin;

namespace CalculateScrapForQuota.Scripts;

public static class Outliner
{
    public static Color Color = Color.green;
    public static float Transparency = 0.5f;
    public static float Scale = 1.05f;
    public static bool IsShowing => isShowing;
    
    private static Shader HDRP_LIT_SHADER = Shader.Find("HDRP/Lit");
    private static Material _glowMaterialHDRP;
    private static Material GlowMaterialHDRP
    {
        get
        {
            if (_glowMaterialHDRP != null) return _glowMaterialHDRP;
            _glowMaterialHDRP = new(HDRP_LIT_SHADER);
            _glowMaterialHDRP.shader = HDRP_LIT_SHADER;
            // Set material to transparent
            _glowMaterialHDRP.SetFloat("_SurfaceType", 1); // 0 is Opaque, 1 is Transparent
            // Blend Mode for HDRP - Alpha blending
            // Note: HDRP uses different numeric values. You may need to adjust these based on your specific needs.
            _glowMaterialHDRP.SetFloat("_BlendMode", 0); // 0 is Alpha, 1 is Additive, etc.
            // Additional transparency settings
            _glowMaterialHDRP.SetFloat("_AlphaCutoffEnable", 0); // Disable alpha cutoff
            _glowMaterialHDRP.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            _glowMaterialHDRP.SetFloat("_ZWrite", 0); // Disable ZWrite for transparency
            _glowMaterialHDRP.SetFloat("_TransparentZWrite", 0);
            _glowMaterialHDRP.SetFloat("_TransparentCullMode", (float)CullMode.Off);
            _glowMaterialHDRP.SetFloat("_TransparentSortPriority", 0); // Adjust if needed
            _glowMaterialHDRP.SetFloat("_CullModeForward", (float)CullMode.Off);
            return _glowMaterialHDRP;
        }
    }
    private static Shader SRP_SHADER = Shader.Find("Standard");
    private static Material _glowMaterialSRP;
    private static Material GlowMaterialSRP
    {
        get
        {
            if (_glowMaterialSRP != null) return _glowMaterialSRP;
            _glowMaterialSRP = new(SRP_SHADER);
            _glowMaterialSRP.shader = SRP_SHADER;
            _glowMaterialSRP.SetFloat("_Mode", 3);
            _glowMaterialSRP.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            _glowMaterialSRP.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            _glowMaterialSRP.SetInt("_ZWrite", 0);
            _glowMaterialSRP.DisableKeyword("_ALPHATEST_ON");
            _glowMaterialSRP.EnableKeyword("_ALPHABLEND_ON");
            _glowMaterialSRP.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            _glowMaterialSRP.renderQueue = 3000;
            return _glowMaterialSRP;
        }
    }
    private static Dictionary<GameObject, GameObject> pool = new();
    private static bool isShowing = false;

    public static void Add(GameObject gameObject)
    {
        if (pool.ContainsKey(gameObject))
            return;
        var visualsGameObject = CloneVisualsOnly(gameObject);
        pool.Add(gameObject, visualsGameObject);
        ConvertVisualsToOutlineVisuals(visualsGameObject);
        visualsGameObject.SetActive(isShowing);
    }

    public static void Show()
    {
        isShowing = true;
        foreach (var go in pool.Values) 
            go.SetActive(true);
    }

    public static void Hide()
    {
        isShowing = false;
        foreach (var go in pool.Values)
            go.SetActive(false);
    }

    public static void Clear()
    {
        isShowing = false;
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
                if (HDRP_LIT_SHADER)
                    renderer.materials[i] = GlowMaterialHDRP;
                else if (SRP_SHADER) 
                    renderer.materials[i] = GlowMaterialSRP;

                renderer.materials[i].color = new(Color.r, Color.g, Color.b, Transparency);
                P.Log($"Using {renderer.materials[i].shader.name}");
            }
        }
    }

    private static GameObject CloneVisualsOnly(GameObject gameObject)
    {
        var clone = Object.Instantiate(gameObject);
        clone.name = $"{gameObject.name} (ClonedVisuals)";
        
        var allComponents = clone.GetComponentsInChildren<Component>();
        foreach (var component in allComponents)
        {
            if (component is not (Transform or MeshFilter or MeshRenderer or SkinnedMeshRenderer))
                Object.Destroy(component);
        }
        
        P.Log($"Made {clone.name}");
        return clone;
    }
}