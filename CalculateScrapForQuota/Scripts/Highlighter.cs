using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;
using P = CalculateScrapForQuota.Plugin;

namespace CalculateScrapForQuota.Scripts;

public static class Highlighter // This class is mediocre, fair warning.
{
    public static Color Color = Color.green;
    public static float Scale = 1.05f;
    public static bool IsShowing => isShowing;
    
    #region MATERIALS
    private static Shader HDRP_LIT_SHADER = Shader.Find("HDRP/Lit");
    private static Material _highlightMaterialHDRP;
    private static Material HighlightMaterialHDRP
    {
        get
        {
            if (_highlightMaterialHDRP != null) return _highlightMaterialHDRP;
            _highlightMaterialHDRP = new(HDRP_LIT_SHADER);
            _highlightMaterialHDRP.shader = HDRP_LIT_SHADER;
            // Set material to transparent
            _highlightMaterialHDRP.SetFloat("_SurfaceType", 1); // 0 is Opaque, 1 is Transparent
            // Blend Mode for HDRP - Alpha blending
            // Note: HDRP uses different numeric values. You may need to adjust these based on your specific needs.
            _highlightMaterialHDRP.SetFloat("_BlendMode", 0); // 0 is Alpha, 1 is Additive, etc.
            // Additional transparency settings
            _highlightMaterialHDRP.SetFloat("_AlphaCutoffEnable", 0); // Disable alpha cutoff
            _highlightMaterialHDRP.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            _highlightMaterialHDRP.SetFloat("_ZWrite", 0); // Disable ZWrite for transparency
            _highlightMaterialHDRP.SetFloat("_TransparentZWrite", 0);
            _highlightMaterialHDRP.SetFloat("_TransparentCullMode", (float)CullMode.Off);
            _highlightMaterialHDRP.SetFloat("_TransparentSortPriority", 0); // Adjust if needed
            _highlightMaterialHDRP.SetFloat("_CullModeForward", (float)CullMode.Off);
            return _highlightMaterialHDRP;
        }
    }
    private static Shader SRP_SHADER = Shader.Find("Standard");
    private static Material _highlightMaterialSRP;
    private static Material HighlightMaterialSRP
    {
        get
        {
            if (_highlightMaterialSRP != null) return _highlightMaterialSRP;
            _highlightMaterialSRP = new(SRP_SHADER);
            _highlightMaterialSRP.shader = SRP_SHADER;
            _highlightMaterialSRP.SetFloat("_Mode", 3);
            _highlightMaterialSRP.SetInt("_SrcBlend", (int)BlendMode.One);
            _highlightMaterialSRP.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            _highlightMaterialSRP.SetInt("_ZWrite", 0);
            _highlightMaterialSRP.DisableKeyword("_ALPHATEST_ON");
            _highlightMaterialSRP.EnableKeyword("_ALPHABLEND_ON");
            _highlightMaterialSRP.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            _highlightMaterialSRP.renderQueue = 3000;
            return _highlightMaterialSRP;
        }
    }
    #endregion
    
    private static Dictionary<GameObject, GameObject> pool = new();
    private static bool isShowing = false;

    public static void Add(GameObject gameObject)
    {
        if (pool.ContainsKey(gameObject))
            return;
        var visualsGameObject = CloneVisualsOnly(gameObject);
        pool.Add(gameObject, visualsGameObject);
        ConvertVisuals(visualsGameObject);
        visualsGameObject.SetActive(isShowing);
    }

    public static void Show()
    {
        isShowing = true;
        foreach (var go in pool.Values)
        {
            P.Log($"Showing {go.name}");
            go.SetActive(true);
        }
    }

    public static void Hide()
    {
        isShowing = false;
        foreach (var go in pool.Values)
        {
            P.Log($"Hiding {go.name}");
            go.SetActive(false);
        }
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
    
    private static void ConvertVisuals(GameObject obj)
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
                    renderer.materials[i] = HighlightMaterialHDRP;
                else if (SRP_SHADER) 
                    renderer.materials[i] = HighlightMaterialSRP;

                renderer.materials[i].color = new(Color.r, Color.g, Color.b, 1);
            }
        }
    }

    private static GameObject CloneVisualsOnly(GameObject gameObject)
    {
        var clone = Object.Instantiate(gameObject, gameObject.transform.position, gameObject.transform.rotation);
        clone.transform.localScale = gameObject.transform.localScale;
        clone.transform.SetParent(gameObject.transform);
        clone.name = $"{gameObject.name} (ClonedVisuals)";
        P.Log($"Made {clone.name}");
        
        var allComponents = clone.GetComponentsInChildren<Component>(false);
        foreach (var component in allComponents)
        {
            if (component is not (Transform or MeshFilter or MeshRenderer or SkinnedMeshRenderer))
                Object.Destroy(component);
            else if (component is MeshFilter mf)
                P.Log($"Preserving mesh: {mf.mesh.name}");
        }
        
        return clone;
    }
}