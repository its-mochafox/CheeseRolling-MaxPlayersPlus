using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace MaxPlayersPlus
{
    [BepInPlugin(Guid, Name, Version)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "mochafox.cheeserolling.maxplayers";
        public const string Name = "Max Players Plus";
        public const string Version = "1.0.0";

        internal static ManualLogSource Log;
        internal static ConfigEntry<int> MaxPlayers;
        internal static ConfigEntry<float> MaxSpawnLineWidth;

        private void Awake()
        {
            Log = Logger;
            MaxPlayers = Config.Bind("General", "MaxPlayers", 64,
                new ConfigDescription("Maximum value of the Host Settings max-players slider (vanilla: 12, Steam limit: 250).",
                    new AcceptableValueRange<int>(2, 250)));
            MaxSpawnLineWidth = Config.Bind("General", "MaxSpawnLineWidth", 132f,
                "Host-only. Players spawn in a line 12 units apart. If > 0, spacing is reduced so the line never exceeds this width " +
                "(vanilla 12 players = 132). 0 = vanilla spacing (64 players would span ~756 units).");

            new Harmony(Guid).PatchAll(typeof(Plugin).Assembly);
            Log.LogInfo($"{Name} {Version} loaded. MaxPlayers={MaxPlayers.Value}, MaxSpawnLineWidth={MaxSpawnLineWidth.Value}");
        }
    }

    [HarmonyPatch(typeof(HostSettingsUI), "OnEnable")]
    internal static class HostSettingsUI_OnEnable_Patch
    {
        private static readonly AccessTools.FieldRef<HostSettingsUI, Slider> SliderRef =
            AccessTools.FieldRefAccess<HostSettingsUI, Slider>("maxPlayersSlider");

        private static void Prefix(HostSettingsUI __instance)
        {
            var slider = SliderRef(__instance);
            if (slider == null)
            {
                Plugin.Log.LogWarning("maxPlayersSlider not found; cannot raise player cap.");
                return;
            }
            if (slider.maxValue != Plugin.MaxPlayers.Value)
            {
                Plugin.Log.LogInfo($"Max players slider: {slider.maxValue} -> {Plugin.MaxPlayers.Value}");
                slider.maxValue = Plugin.MaxPlayers.Value;
            }
        }
    }

    [HarmonyPatch(typeof(EntityManager), "GeneratePlayerRagdolls")]
    internal static class EntityManager_GeneratePlayerRagdolls_Patch
    {
        private const float VanillaSpacing = 12f;
        private static readonly MethodInfo GetMemberIDs = AccessTools.Method(typeof(LobbyManager), "GetMemberIDs");
        private static readonly AccessTools.FieldRef<EntityManager, int> NextID =
            AccessTools.FieldRefAccess<EntityManager, int>("nextID");

        // Replaces the first `ldc.r4 12.0` (the per-player spawn spacing) with a call to GetSpacing().
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            bool replaced = false;
            foreach (var ins in instructions)
            {
                if (!replaced && ins.opcode == OpCodes.Ldc_R4 && ins.operand is float f && f == VanillaSpacing)
                {
                    replaced = true;
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(EntityManager_GeneratePlayerRagdolls_Patch), nameof(GetSpacing))).MoveLabelsFrom(ins);
                    continue;
                }
                yield return ins;
            }
            if (!replaced) Plugin.Log.LogWarning("Spawn spacing constant not found; MaxSpawnLineWidth will have no effect.");
        }

        private static float GetSpacing()
        {
            float maxWidth = Plugin.MaxSpawnLineWidth.Value;
            if (maxWidth <= 0f) return VanillaSpacing;
            int count = 0;
            try
            {
                if (GetMemberIDs?.Invoke(LobbyManager.Instance, null) is System.Array ids) count = ids.Length;
            }
            catch (System.Exception e) { Plugin.Log.LogWarning($"Could not get member count: {e.Message}"); }
            if (count <= 1) return VanillaSpacing;
            return Mathf.Min(VanillaSpacing, maxWidth / (count - 1));
        }

        private static void Postfix(EntityManager __instance)
        {
            int next = NextID(__instance);
            if (next >= 200)
                Plugin.Log.LogWarning($"Entity IDs used: {next}/255. Too many players/entities may break this map.");
            else
                Plugin.Log.LogDebug($"Entity IDs used: {next}/255");
        }
    }
}
