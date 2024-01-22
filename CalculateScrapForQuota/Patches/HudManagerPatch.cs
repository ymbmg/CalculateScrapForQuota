using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;
using BepInEx;
using CalculateScrapForQuota.Scripts;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
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
        private static int unmetQuota => TimeOfDay.Instance.profitQuota - TimeOfDay.Instance.quotaFulfilled;
        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(HUDManager), "PingScan_performed")]
        private static void OnScan(HUDManager __instance, InputAction.CallbackContext context)
        {
            P.Log("OnScan() called.");
            var fieldInfo = typeof(HUDManager).GetField("playerPingingScan", BindingFlags.NonPublic | BindingFlags.Instance);
            var playerPingingScan = (float)fieldInfo.GetValue(__instance);
            var methodInfo = typeof(HUDManager).GetMethod("CanPlayerScan", BindingFlags.NonPublic | BindingFlags.Instance);
            var canPlayerScan = (bool)methodInfo.Invoke(__instance, null);
            
            if (!context.performed 
                || !canPlayerScan 
                || playerPingingScan > -1.0
                || GameNetworkManager.Instance.localPlayerController == null
                || (!StartOfRound.Instance.inShipPhase && !GameNetworkManager.Instance.localPlayerController.isInHangarShipRoom)
                ) return;
            
            var sellableGrabbables = GetSellableGrabbablesInChildren(shipGO);
            var optimalGrabbables = GetOptimalGrabbablesForQuota(sellableGrabbables, unmetQuota);
            
            OutlineGrabbables(sellableGrabbables);
            
            SetupText();
            
            GameNetworkManager.Instance.StartCoroutine(Display());
        }
        
        private static bool isDisplaying = false;
        private static IEnumerator Display(float duration = 5f)
        {
            if (isDisplaying)
                yield break;
            
            isDisplaying = true;
            _textGO.SetActive(true);
            Outliner.Show();
            
            yield return new WaitForSeconds(duration);
            
            isDisplaying = false;
            _textGO.SetActive(false);
            Outliner.Hide();
        }
        
        private static List<GrabbableObject> GetOptimalGrabbablesForQuota(List<GrabbableObject> grabbables, int quota)
        {
            var sortedGrabbables = grabbables.OrderByDescending(g => g.scrapValue).ToList();
            //var bestIndividualGrabbable = MeetQuota(sortedGrabbables, quota);
            return sortedGrabbables;
            // TODO: the FindCombinations
            //List<List<int>> bestCombinations = FindCombinations(itemsValues, quota);
        }

        private static GrabbableObject MeetQuota(List<GrabbableObject> grabbables, int quota)
        {
            // Filter out items that are smaller than the quota
            grabbables = grabbables.Where(g => g.scrapValue >= quota).ToList();

            // Subtract each item from the quota and return the item that results in the smallest non-negative difference
            return grabbables.Aggregate((a, b) => Math.Abs(quota - a.scrapValue) < Math.Abs(quota - b.scrapValue) ? a : b);
        }

        private static List<List<int>> FindCombinations(List<int> items, int quota)
        {
            var results = new List<List<int>>();
            int n = items.Count;
            int bestSumDifference = int.MaxValue;

            // Generate all subsets of the items
            for (int i = 0; i < (1 << n); i++)
            {
                var subset = new List<int>();
                for (int j = 0; j < n; j++)
                {
                    if ((i & (1 << j)) > 0)
                    {
                        subset.Add(items[j]);
                    }
                }

                // Calculate the sum of the subset
                int subsetSum = subset.Sum();

                // If the sum of the subset is close to the quota, update the best result
                int sumDifference = Math.Abs(quota - subsetSum);
                if (sumDifference < bestSumDifference)
                {
                    bestSumDifference = sumDifference;
                    results.Clear();
                    results.Add(subset);
                }
                else if (sumDifference == bestSumDifference)
                {
                    results.Add(subset);
                }
            }

            // Sort the results by the size of the combination, ascending
            results.Sort((a, b) => a.Count.CompareTo(b.Count));

            return results;
        }

        private static List<GrabbableObject> GetSellableGrabbablesInChildren(GameObject GO)
        {
            var shipGrabbables = GO.GetComponentsInChildren<GrabbableObject>();
            var sellableGrabbables = shipGrabbables.Where(g => 
                g.itemProperties.isScrap 
                && g.scrapValue > 0 
                && g.name != "ClipboardManual" 
                && g.name != "StickyNoteItem"
                && g.name != "Gift"
                && g.name != "Shotgun"
                && g.name != "Ammo"
                ).ToList();
            return sellableGrabbables;
        }

        private static void OutlineGrabbables(List<GrabbableObject> grabbables)
        {
            foreach (var g in grabbables)
            {
                var go = g.gameObject;
                P.Log($"Adding {go.name} to Outliner");
                Outliner.Add(go);
            }
        }
        
        private static GameObject _textGO;
        private static TextMeshProUGUI _textMesh => _textGO.GetComponentInChildren<TextMeshProUGUI>();
        
        private static void SetupText()
        {
            // Text GameObject instantiation and caching
            if (!_textGO)
            {
                _textGO = Object.Instantiate(valueCounterGO.gameObject, valueCounterGO.transform.parent, false);
                _textGO.transform.Translate(0f, 1f, 0f);
                var pos = _textGO.transform.localPosition;
                _textGO.transform.localPosition = new(pos.x + 50f, -100f, pos.z);
            }
            
            _textMesh.text = $"[insert if canMakeQuota]";
        }
    }
}
