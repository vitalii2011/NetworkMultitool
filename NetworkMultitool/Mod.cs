﻿using ColossalFramework.UI;
using HarmonyLib;
using ICities;
using ModsCommon;
using ModsCommon.Utilities;
using NetworkMultitool.UI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection.Emit;
using System.Resources;
using System.Text;
using static ColossalFramework.Plugins.PluginManager;

namespace NetworkMultitool
{
    public class Mod : BasePatcherMod<Mod>
    {
        #region PROPERTIES

        protected override string IdRaw => nameof(NetworkMultitool);
        public override List<Version> Versions { get; } = new List<Version>
        {
            new Version("1.2"),
            new Version("1.1"),
            new Version("1.0")
        };

        public override string NameRaw => "Network Multitool";
        public override string Description => !IsBeta ? Localize.Mod_Description : CommonLocalize.Mod_DescriptionBeta;
        protected override ulong StableWorkshopId => 2560782729ul;
        protected override ulong BetaWorkshopId => 2556133736ul;

#if BETA
        public override bool IsBeta => true;
#else
        public override bool IsBeta => false;
#endif
        #endregion

        protected override ResourceManager LocalizeManager => Localize.ResourceManager;
        private static PluginSearcher FRTSearcher { get; } = PluginUtilities.GetSearcher("Fine Road Tool", 1844442251ul);
        private static PluginSearcher NodeSpacerSearcher { get; } = PluginUtilities.GetSearcher("Node Spacer", 2085018096ul);

        public static bool IsFRT => FRTSearcher.GetPlugin() != null;
        public static bool IsNodeSpacer => NodeSpacerSearcher.GetPlugin() != null;
        public static bool FRTEnabled => FRTSearcher.GetPlugin() is PluginInfo plugin && plugin.isEnabled;
        public static bool NodeSpacerEnabled => NodeSpacerSearcher.GetPlugin() is PluginInfo plugin && plugin.isEnabled;

        #region BASIC

        protected override void GetSettings(UIHelperBase helper)
        {
            var settings = new Settings();
            settings.OnSettingsUI(helper);
        }
        protected override void SetCulture(CultureInfo culture) => Localize.Culture = culture;

        #endregion

        #region PATCHES

        protected override bool PatchProcess()
        {
            var success = true;

            success &= AddTool();
            success &= AddNetToolButton();
            success &= ToolOnEscape();
            if (IsFRT)
            {
                success &= FineRoadToolUpdate();
                success &= FineRoadToolOnGUI();
            }
            if (IsNodeSpacer)
                success &= NodeSpacerStart();
            return success;
        }

        private bool AddTool()
        {
            return AddTranspiler(typeof(Patcher), nameof(Patcher.ToolControllerAwakeTranspiler), typeof(ToolController), "Awake");
        }
        private bool AddNetToolButton()
        {
            return AddPostfix(typeof(Patcher), nameof(Patcher.GeneratedScrollPanelCreateOptionPanelPostfix), typeof(GeneratedScrollPanel), "CreateOptionPanel");
        }
        private bool ToolOnEscape()
        {
            return AddTranspiler(typeof(Patcher), nameof(Patcher.GameKeyShortcutsEscapeTranspiler), typeof(GameKeyShortcuts), "Escape");
        }
        private bool FineRoadToolUpdate()
        {
            if (!AddTranspiler(typeof(Patcher), nameof(Patcher.FineRoadToolUpdateTranspiler), Type.GetType("FineRoadTool.FineRoadTool"), "Update"))
                return AddTranspiler(typeof(Patcher), nameof(Patcher.FineRoadToolUpdateTranspiler), Type.GetType("FineRoadTool.FineRoadTool"), "FpsBoosterUpdate");
            else
                return true;
        }
        private bool FineRoadToolOnGUI()
        {
            return AddTranspiler(typeof(Patcher), nameof(Patcher.FineRoadToolOnGUITranspiler), Type.GetType("FineRoadTool.FineRoadTool"), "OnGUI");
        }
        private bool NodeSpacerStart()
        {
            return AddPostfix(typeof(Patcher), nameof(Patcher.NodeSpacerPostfix), Type.GetType("NodeSpacer.ModUI"), "Start");
        }

        #endregion
    }
    public static class Patcher
    {
        public static IEnumerable<CodeInstruction> ToolControllerAwakeTranspiler(ILGenerator generator, IEnumerable<CodeInstruction> instructions) => ModsCommon.Patcher.ToolControllerAwakeTranspiler<Mod, NetworkMultitoolTool>(generator, instructions);

        public static void GeneratedScrollPanelCreateOptionPanelPostfix(string templateName, ref OptionPanelBase __result) => ModsCommon.Patcher.GeneratedScrollPanelCreateOptionPanelPostfix<Mod, NetworkMultitoolButton>(templateName, ref __result, ModsCommon.Patcher.RoadsOptionPanel, ModsCommon.Patcher.TracksOptionPanel, ModsCommon.Patcher.PathsOptionPanel, ModsCommon.Patcher.CanalsOptionPanel, ModsCommon.Patcher.QuaysOptionPanel, ModsCommon.Patcher.FloodWallsOptionPanel);

        public static IEnumerable<CodeInstruction> GameKeyShortcutsEscapeTranspiler(ILGenerator generator, IEnumerable<CodeInstruction> instructions) => ModsCommon.Patcher.GameKeyShortcutsEscapeTranspiler<Mod, NetworkMultitoolTool>(generator, instructions);

        public static IEnumerable<CodeInstruction> FineRoadToolUpdateTranspiler(ILGenerator generator, IEnumerable<CodeInstruction> instructions)
        {
            var enabledProperty = AccessTools.PropertyGetter(typeof(UnityEngine.Behaviour), nameof(UnityEngine.Behaviour.enabled));
            var netToolField = AccessTools.Field(Type.GetType("FineRoadTool.FineRoadTool"), "m_netTool");
            var prefabField = AccessTools.Field(typeof(NetTool), nameof(NetTool.m_prefab));
            var prev = default(CodeInstruction);
            foreach (var instruction in instructions)
            {
                yield return instruction;
                if (prev != null && prev.opcode == OpCodes.Ldfld && prev.operand == netToolField && instruction.opcode == OpCodes.Callvirt && instruction.operand == enabledProperty)
                {
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Patcher), nameof(Patcher.Enabled), new Type[] { typeof(bool)}));
                }
                else if (instruction.opcode == OpCodes.Ldfld && instruction.operand == prefabField)
                {
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Patcher), nameof(Patcher.GetPrefab)));
                }
                prev = instruction;
            }
        }
        private static bool Enabled(bool netToolEnabled) => netToolEnabled || SingletonTool<NetworkMultitoolTool>.Instance.enabled;
        private static bool Enabled() => SingletonTool<NetworkMultitoolTool>.Instance.enabled;
        private static NetInfo GetPrefab(NetInfo info) => info ?? PrefabCollection<NetInfo>.FindLoaded("Basic Road");

        public static IEnumerable<CodeInstruction> FineRoadToolOnGUITranspiler(ILGenerator generator, IEnumerable<CodeInstruction> instructions)
        {
            var modeField = AccessTools.Field(Type.GetType("FineRoadTool.FineRoadTool"), "m_mode");
            var prev = default(CodeInstruction);
            var label = generator.DefineLabel();
            var count = 0;
            var labelAdded = false;
            foreach (var inst in instructions)
            {
                var instruction = inst;
                if (prev != null && prev.opcode == OpCodes.Ldarg_0 && instruction.opcode == OpCodes.Ldfld && instruction.operand == modeField)
                {
                    count += 1;
                    if (count == 3)
                    {
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Patcher), nameof(Patcher.Enabled), new Type[0]))
                        {
                            labels = prev.labels,
                        };
                        yield return new CodeInstruction(OpCodes.Brtrue_S, label);
                        prev.labels = new List<Label>();
                    }
                }
                else if (instruction.opcode == OpCodes.Leave_S && !labelAdded)
                {
                    labelAdded = true;
                    instruction.labels.Add(label);
                }
                else if (instruction.opcode == OpCodes.Ldc_I4_S && instruction.operand is sbyte value && value == (sbyte)InfoManager.InfoMode.Traffic)
                    instruction = new CodeInstruction(OpCodes.Ldc_I4_S, (sbyte)InfoManager.InfoMode.Underground);

                if (prev != null)
                    yield return prev;

                prev = instruction;
            }
            yield return prev;
        }

        public static void NodeSpacerPostfix(UISlider ___m_maxLengthSlider)
        {
            ___m_maxLengthSlider.eventValueChanged += (_, _) =>
            {
                if (SingletonTool<NetworkMultitoolTool>.Instance.Mode is BaseCreateMode mode)
                    mode.Recalculate();
            };
        }
    }
    public class LoadingExtension : BaseLoadingExtension<Mod> { }
}
