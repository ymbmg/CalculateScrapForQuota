using System.Reflection;
using UnityEngine;

namespace CalculateScrapForQuota.Utils;

public static class DebugUtil
{
    private const BindingFlags FLAGS = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    
    public static string GetGameObjectDetails(GameObject gameObject, bool ignoreProperties = false, bool ignoreFields = false)
    {
        var info = "--- GameObject Info";
        
        info += $"\nName: {gameObject.name}";
        foreach (var component in gameObject.GetComponents<Component>())
        {
            info += $"\n  Component: {component.GetType().Name}";
            
            if (!ignoreProperties)
            {
                var properties = component.GetType().GetProperties(FLAGS);
                foreach (var property in properties)
                {
                    info += $"\n    Property: {property.Name}, Value: {property.GetValue(component, null)}";
                }
            }

            if (!ignoreFields)
            {
                var fields = component.GetType().GetFields(FLAGS);
                foreach (var field in fields)
                {
                    info += $"\n    Field: {field.Name}, Value: {field.GetValue(component)}";
                }
            }
        }
        
        return info;
    }
}