using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;
using BepInEx;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using CalculateScrapForQuota.Scripts;
using CalculateScrapForQuota.Utils;
using Object = UnityEngine.Object;
using P = CalculateScrapForQuota.Plugin;
using DU = CalculateScrapForQuota.Utils.DebugUtil;
// ReSharper disable PossibleNullReferenceException

namespace CalculateScrapForQuota.Patches
{
    [HarmonyPatch]
    [SuppressMessage("Method Declaration", "Harmony003:Harmony non-ref patch parameters modified")]
    public class HudManagerPatch
    {
        private static GameObject shipGO => GameObject.Find("/Environment/HangarShip");
        private static GameObject valueCounterGO => GameObject.Find("/Systems/UI/Canvas/IngamePlayerHUD/BottomMiddle/ValueCounter");
        private static int unmetQuota => Math.Max(0, TimeOfDay.Instance.profitQuota - TimeOfDay.Instance.quotaFulfilled);
        private static bool isAtTheCompany => StartOfRound.Instance.currentLevel.levelID == 3;
        private static bool isInShip => GameNetworkManager.Instance.localPlayerController.isInHangarShipRoom;
        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(HUDManager), "PingScan_performed")]
        private static void OnScan(HUDManager __instance, InputAction.CallbackContext context)
        {
            P.Log("OnScan() called.");
            var fieldInfo = typeof(HUDManager).GetField("playerPingingScan", BindingFlags.NonPublic | BindingFlags.Instance);
            var playerPingingScan = (float)fieldInfo.GetValue(__instance);
            var methodInfo = typeof(HUDManager).GetMethod("CanPlayerScan", BindingFlags.NonPublic | BindingFlags.Instance);
            var canPlayerScan = (bool)methodInfo.Invoke(__instance, null);
            
            // Guard Clause
            if (!context.performed 
                || !canPlayerScan 
                || playerPingingScan > -1.0
                || GameNetworkManager.Instance.localPlayerController == null
                ) return;
            
            P.Log("OnScan() is valid.");

            if (unmetQuota <= 0)
                return;

            List<GrabbableObject> sellableGrabbables;
            if (isInShip && !isAtTheCompany)
                sellableGrabbables = GetSellableGrabbablesInChildren(shipGO);
            else if (isAtTheCompany)
                sellableGrabbables = GetAllSellableObjects();
            else
                return;
            
            var optimalGrabbables = MathUtil.FindBestCombination(sellableGrabbables, unmetQuota, grabbable => grabbable.scrapValue);

            if (optimalGrabbables.totalValue >= unmetQuota)
            {
                HighlightGrabbables(optimalGrabbables.combination);
                SetupText(optimalGrabbables.totalValue);
            }
            
            GameNetworkManager.Instance.StartCoroutine(Display());
        }
        
        private static bool isDisplaying = false;
        private static IEnumerator Display(float duration = 5f)
        {
            if (isDisplaying)
                yield break;
            
            isDisplaying = true;
            _textGO.SetActive(true);
            Highlighter.Show();
            
            yield return new WaitForSeconds(duration);
            
            isDisplaying = false;
            _textGO.SetActive(false);
            Highlighter.Hide();
        }

        private static List<GrabbableObject> GetAllSellableObjects()
        {
            var grabbables = GameObject.FindObjectsOfType<GrabbableObject>();
            var sellableGrabbables = grabbables.Where(IsGrabbableSellable).ToList();
            return sellableGrabbables;
            
        }
        
        private static List<GrabbableObject> GetSellableGrabbablesInChildren(GameObject GO)
        {
            var grabbables = GO.GetComponentsInChildren<GrabbableObject>();
            var sellableGrabbables = grabbables.Where(IsGrabbableSellable).ToList();
            return sellableGrabbables;
        }
        
        private static bool IsGrabbableSellable(GrabbableObject grabbable)
        {
            return grabbable.itemProperties.isScrap
                   && !grabbable.isPocketed
                   && grabbable.scrapValue > 0
                   && grabbable.name != "ClipboardManual"
                   && grabbable.name != "StickyNoteItem"
                   && grabbable.name != "Gift"
                   && grabbable.name != "Shotgun"
                   && grabbable.name != "Ammo";
        }

        private static void HighlightGrabbables(List<GrabbableObject> grabbables)
        {
            Highlighter.Clear();
            foreach (var go in grabbables.Select(g => g.gameObject))
            {
                P.Log($"Adding {go.name} to Outliner");
                Highlighter.Add(go);
            }
        }
        
        private static GameObject _textGO;
        private static TextMeshProUGUI _textMesh => _textGO.GetComponentInChildren<TextMeshProUGUI>();
        
        private static void SetupText(int totalValue)
        {
            // Text GameObject instantiation and caching
            if (!_textGO)
            {
                _textGO = Object.Instantiate(valueCounterGO.gameObject, valueCounterGO.transform.parent, false);
                _textGO.transform.Translate(0f, 1f, 0f);
                var pos = _textGO.transform.localPosition;
                _textGO.transform.localPosition = new(pos.x + 50f, -100f, pos.z);
                _textMesh.fontSize = 12;
            }
            
            _textMesh.text = $"Optimal: {totalValue}";
        }
    }
}
