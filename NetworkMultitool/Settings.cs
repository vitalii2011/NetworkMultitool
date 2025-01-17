﻿using ColossalFramework;
using ColossalFramework.UI;
using ModsCommon;
using ModsCommon.Utilities;
using NetworkMultitool.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static ModsCommon.SettingsHelper;

namespace NetworkMultitool
{
    public class Settings : BaseSettings<Mod>
    {
        #region PROPERTIES

        public static SavedBool ShowToolTip { get; } = new SavedBool(nameof(ShowToolTip), SettingsFile, true, true);
        public static SavedBool AutoHideModePanel { get; } = new SavedBool(nameof(AutoHideModePanel), SettingsFile, true, true);
        public static SavedInt PanelOpenSide { get; } = new SavedInt(nameof(PanelOpenSide), SettingsFile, (int)OpenSide.Down, true);
        public static SavedInt SlopeUnite { get; } = new SavedInt(nameof(SlopeUnite), SettingsFile, 0, true);
        public static SavedBool SlopeColors { get; } = new SavedBool(nameof(SlopeColors), SettingsFile, true, true);
        public static SavedInt LengthUnite { get; } = new SavedInt(nameof(LengthUnite), SettingsFile, 0, true);
        public static SavedInt SegmentLength { get; } = new SavedInt(nameof(SegmentLength), SettingsFile, 80, true);
        public static SavedInt PanelColumns { get; } = new SavedInt(nameof(PanelColumns), SettingsFile, 2, true);
        public static SavedBool PlayEffects { get; } = new SavedBool(nameof(PlayEffects), SettingsFile, true, true);
        public static SavedInt NetworkPreview { get; } = new SavedInt(nameof(NetworkPreview), SettingsFile, (int)PreviewType.Both, true);
        public static SavedBool FollowTerrain { get; } = new SavedBool(nameof(FollowTerrain), SettingsFile, false, true);
        public static SavedBool NeedMoney { get; } = new SavedBool(nameof(NeedMoney), SettingsFile, true, true);

        public static bool ShowOverlay => NetworkPreview != (int)PreviewType.Mesh;
        public static bool ShowMesh => NetworkPreview != (int)PreviewType.Overlay;

        protected UIAdvancedHelper ShortcutsTab => GetTab(nameof(ShortcutsTab));

        #endregion

        #region BASIC

        protected override IEnumerable<KeyValuePair<string, string>> AdditionalTabs
        {
            get
            {
                yield return new KeyValuePair<string, string>(nameof(ShortcutsTab), CommonLocalize.Settings_Shortcuts);
            }
        }
        protected override void FillSettings()
        {
            base.FillSettings();

            AddGeneral();
            AddShortcuts();
#if DEBUG
            AddDebug(DebugTab);
#endif

        }

        #endregion

        #region GENERAL

        private void AddGeneral()
        {
            AddLanguage(GeneralTab);

            var interfaceGroup = GeneralTab.AddGroup(Localize.Settings_Interface);
            AddToolButton<NetworkMultitoolTool, NetworkMultitoolButton>(interfaceGroup);
            AddCheckBox(interfaceGroup, CommonLocalize.Settings_ShowTooltips, ShowToolTip);
            AddCheckBox(interfaceGroup, Localize.Settings_AutoHideModePanel, AutoHideModePanel, OnAutoHideChanged);
            if (NetworkMultitoolTool.IsUUIEnabled)
                AddCheckboxPanel(interfaceGroup, Localize.Settings_PanelOpenSide, PanelOpenSide, new string[] { Localize.Settings_PanelOpenSideDown, Localize.Settings_PanelOpenSideUp }, OnOpenSideChanged);
            AddIntField(interfaceGroup, Localize.Settings_PanelColumns, PanelColumns, 2, 1, 5, OnColumnChanged);
            AddCheckBox(interfaceGroup, Localize.Settings_PlayEffects, PlayEffects);
            AddCheckboxPanel(interfaceGroup, Localize.Settings_PreviewType, NetworkPreview, new string[] { Localize.Settings_PreviewTypeOverlay, Localize.Settings_PreviewTypeMesh, Localize.Settings_PreviewTypeBoth });
            AddCheckBox(interfaceGroup, Localize.Settings_SlopeColors, SlopeColors, OnSlopeUniteChanged);

            var gameplayGroup = GeneralTab.AddGroup(Localize.Settings_Gameplay);
            AddCheckBox(gameplayGroup, Localize.Settings_NeedMoney, NeedMoney);
            AddCheckBox(gameplayGroup, Localize.Settings_FollowTerrain, FollowTerrain);
            AddCheckboxPanel(gameplayGroup, Localize.Settings_LengthUnit, LengthUnite, new string[] { Localize.Settings_LengthUniteMeters, Localize.Settings_LengthUniteUnits }, OnSlopeUniteChanged);
            AddCheckboxPanel(gameplayGroup, Localize.Settings_SlopeUnit, SlopeUnite, new string[] { Localize.Settings_SlopeUnitPercentages, Localize.Settings_SlopeUnitDegrees }, OnSlopeUniteChanged);
            if (Utility.InGame && !Mod.NodeSpacerEnabled)
                AddIntField(gameplayGroup, Localize.Settings_SegmentLength, SegmentLength, 80, 50, 200);

            AddNotifications(GeneralTab);

            static void OnAutoHideChanged()
            {
                foreach (var panel in UIView.GetAView().GetComponentsInChildren<ModesPanel>())
                {
                    if (AutoHideModePanel)
                        panel.SetState(false, true);
                    else
                        panel.SetState(true);
                }
            }
            static void OnOpenSideChanged()
            {
                foreach (var panel in UIView.GetAView().GetComponentsInChildren<ModesPanel>())
                    panel.SetOpenSide();
            }
            static void OnColumnChanged()
            {
                foreach (var panel in UIView.GetAView().GetComponentsInChildren<ModesPanel>())
                    panel.FitChildren();
            }
            static void OnSlopeUniteChanged()
            {
                if (SingletonTool<NetworkMultitoolTool>.Instance?.Mode is SlopeNodeMode slopeNode)
                    slopeNode.RefreshLabels();
            }
        }

        #endregion

        #region SHORTCUTS

        private void AddShortcuts()
        {
            var modesGroup = ShortcutsTab.AddGroup(Localize.Settings_ActivationShortcuts);
            var modesKeymapping = AddKeyMappingPanel(modesGroup);
            modesKeymapping.AddKeymapping(NetworkMultitoolTool.ActivationShortcut);
            foreach (var shortcut in NetworkMultitoolTool.ModeShortcuts.Values)
                modesKeymapping.AddKeymapping(shortcut);

            var generalGroup = ShortcutsTab.AddGroup(Localize.Settings_CommonShortcuts);
            var generalKeymapping = AddKeyMappingPanel(generalGroup);
            generalKeymapping.AddKeymapping(NetworkMultitoolTool.SelectionStepOverShortcut);
            generalKeymapping.AddKeymapping(BaseNetworkMultitoolMode.ApplyShortcut);

            var commonGroup = ShortcutsTab.AddGroup(Localize.Settings_CommonCreateShortcuts);
            var commonKeymapping = AddKeyMappingPanel(commonGroup);
            commonKeymapping.AddKeymapping(BaseCreateMode.SwitchFollowTerrainShortcut);
            commonKeymapping.AddKeymapping(BaseCreateMode.SwitchOffsetShortcut);
            commonKeymapping.AddKeymapping(BaseCreateMode.IncreaseAngleShortcut);
            commonKeymapping.AddKeymapping(BaseCreateMode.DecreaseAngleShortcut);

            var connectionGroup = ShortcutsTab.AddGroup(Localize.Mode_CreateConnection);
            var connectionKeymapping = AddKeyMappingPanel(connectionGroup);
            connectionKeymapping.AddKeymapping(CreateConnectionMode.IncreaseRadiiShortcut);
            connectionKeymapping.AddKeymapping(CreateConnectionMode.DecreaseRadiiShortcut);
            connectionKeymapping.AddKeymapping(CreateConnectionMode.SwitchSelectShortcut);
            connectionKeymapping.AddKeymapping(CreateConnectionMode.IncreaseOneRadiusShortcut);
            connectionKeymapping.AddKeymapping(CreateConnectionMode.DecreaseOneRadiusShortcut);
            connectionKeymapping.AddKeymapping(CreateConnectionMode.IncreaseOffsetShortcut);
            connectionKeymapping.AddKeymapping(CreateConnectionMode.DecreaseOffsetShortcut);

            var loopGroup = ShortcutsTab.AddGroup(Localize.Mode_CreateLoop);
            var loopKeymapping = AddKeyMappingPanel(loopGroup);
            loopKeymapping.AddKeymapping(CreateLoopMode.IncreaseRadiusShortcut);
            loopKeymapping.AddKeymapping(CreateLoopMode.DecreaseRadiusShortcut);
            loopKeymapping.AddKeymapping(CreateLoopMode.SwitchIsLoopShortcut);

            var parallelGroup = ShortcutsTab.AddGroup(Localize.Mode_CreateParallerl);
            var parallelKeymapping = AddKeyMappingPanel(parallelGroup);
            parallelKeymapping.AddKeymapping(CreateParallelMode.IncreaseShiftShortcut);
            parallelKeymapping.AddKeymapping(CreateParallelMode.DecreaseShiftShortcut);
            parallelKeymapping.AddKeymapping(CreateParallelMode.IncreaseHeightShortcut);
            parallelKeymapping.AddKeymapping(CreateParallelMode.DecreaseHeightShortcut);
            parallelKeymapping.AddKeymapping(CreateParallelMode.ChangeSideShortcut);
            parallelKeymapping.AddKeymapping(CreateParallelMode.InvertNetworkShortcut);

            var arrangeCircleGroup = ShortcutsTab.AddGroup(Localize.Mode_ArrangeAtCircle);
            var arrangeCircleKeymapping = AddKeyMappingPanel(arrangeCircleGroup);
            arrangeCircleKeymapping.AddKeymapping(ArrangeCircleCompleteMode.ResetArrangeCircleShortcut);
            arrangeCircleKeymapping.AddKeymapping(ArrangeCircleCompleteMode.DistributeEvenlyShortcut);
            arrangeCircleKeymapping.AddKeymapping(ArrangeCircleCompleteMode.DistributeIntersectionsShortcut);
            arrangeCircleKeymapping.AddKeymapping(ArrangeCircleCompleteMode.DistributeBetweenIntersectionsShortcut);
        }

        #endregion

        #region DEBUG
#if DEBUG
        private void AddDebug(UIAdvancedHelper helper)
        {
            var overlayGroup = helper.AddGroup("Selection overlay");

            Selection.AddAlphaBlendOverlay(overlayGroup);
            Selection.AddRenderOverlayCentre(overlayGroup);
            Selection.AddRenderOverlayBorders(overlayGroup);
            Selection.AddBorderOverlayWidth(overlayGroup);
        }
#endif
        #endregion

        public enum PreviewType
        {
            Overlay = 0,
            Mesh = 1,
            Both = 2,
        }
    }
}

