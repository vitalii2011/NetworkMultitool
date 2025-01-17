﻿using ColossalFramework.UI;
using ModsCommon;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NetworkMultitool
{
    public class SplitNodeMode : BaseNetworkMultitoolMode
    {
        public override ToolModeType Type => ToolModeType.SplitNode;

        protected override bool IsReseted => !IsSource;
        protected override bool SelectNodes => !IsSource;
        protected override bool IsValidSegment(ushort segmentId) => base.IsValidSegment(segmentId) && (!IsSource || segmentId.GetSegment().Contains(Source.Id));
        protected override bool CanSwitchUnderground => !IsSource;

        private NodeSelection Source { get; set; }
        private bool IsSource => Source != null;
        private HashSet<Selection> Segments { get; set; } = new HashSet<Selection>(Selection.Comparer);
        private bool IsSegments => Segments.Count != 0;
        private bool CanAddSegment => Segments.Count < Source.Id.GetNode().CountSegments() - 1;
        private bool IsFar => (Source.Id.GetNode().m_position - Tool.MouseWorldPosition).sqrMagnitude > 40000f;
        private bool IsReady => IsSource && IsSegments;
        private bool IsCorrect => IsReady && !IsFar;

        protected override string GetInfo()
        {
            if (!IsSource)
            {
                if (!IsHoverNode)
                    return Localize.Mode_UnionNode_Info_SelectSource + UndergroundInfo;
                else if (HoverNode.Id.GetNode().CountSegments() < 2)
                    return Localize.Mode_SplitNode_Info_NotAllowedSplit.AddErrorColor() + StepOverInfo;
                else
                    return Localize.Mode_UnionNode_Info_ClickSource.AddActionColor() + StepOverInfo;
            }
            else if (!IsSegments)
            {
                if (!IsHoverSegment)
                    return Localize.Mode_SplitNode_Info_SelectToSplit;
                else
                    return Localize.Mode_SplitNode_Info_ClickToOrder.AddActionColor() + StepOverInfo;
            }
            else
            {
                if (!IsHoverSegment)
                {
                    if (IsFar)
                        return Localize.Mode_SplitNode_Info_TooFar.AddErrorColor();
                    else
                        return string.Format(Localize.Mode_SplitNode_Info_ClickToSplit, LocalizeExtension.Shift.AddInfoColor());
                }
                else if (Segments.Contains(HoverSegment))
                    return Localize.Mode_SplitNode_Info_ClickFromOrder.AddActionColor();
                else if (!CanAddSegment)
                    return Localize.Mode_SplitNode_Info_OrderIsFull.AddErrorColor();
                else
                    return Localize.Mode_SplitNode_Info_ClickToOrder.AddActionColor();
            }
        }
        protected override void Reset(IToolMode prevMode)
        {
            base.Reset(prevMode);

            Source = null;
            Segments.Clear();
        }
        public override void OnPrimaryMouseClicked(Event e)
        {
            if (IsHoverNode)
            {
                if (HoverNode.Id.GetNode().CountSegments() >= 2)
                    Source = HoverNode;
            }
            else if (IsHoverSegment)
            {
                if (!IsSource)
                    return;
                else if (Segments.Contains(HoverSegment))
                    Segments.Remove(HoverSegment);
                else if (CanAddSegment)
                    Segments.Add(HoverSegment);
            }
            else if (IsCorrect)
            {
                var sourceId = Source.Id;
                var segments = Segments.ToArray();
                var newPosition = Tool.MouseWorldPosition;
                if (Utility.OnlyShiftIsPressed)
                    newPosition.y = sourceId.GetNode().m_position.y;
                SimulationManager.instance.AddAction(() =>
                {
                    Split(sourceId, newPosition, segments);
                    PlayNodeEffect(sourceId, true);
                    ClearSelectionBuffer();
                });

                Reset(this);
            }
        }
        public override void OnSecondaryMouseClicked()
        {
            if (IsSegments)
                Segments.Clear();
            else if (IsSource)
                Source = null;
            else
                base.OnSecondaryMouseClicked();
        }

        private static bool Split(ushort nodeId, Vector3 newPosition, IEnumerable<Selection> segments)
        {
            var sourceNode = nodeId.GetNode();
            var terrainRect = GetTerrainRect(segments.Select(s => s.Id).ToArray());

            CreateNode(out var newNodeId, sourceNode.Info, newPosition);

            foreach (var segment in segments)
                RelinkSegment(segment.Id, nodeId, newNodeId);

            UpdateTerrain(terrainRect);
            return true;
        }

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            if (IsSource)
            {
                Source.Render(new OverlayData(cameraInfo) { RenderLimit = Underground });

                if (IsHoverSegment)
                {
                    var color = Segments.Contains(HoverSegment) ? Colors.Yellow : (CanAddSegment ? Colors.Green : Colors.Red);
                    HoverSegment.Render(new OverlayData(cameraInfo) { Color = color, RenderLimit = Underground });
                }
                else if (IsReady)
                {
                    var width = Segments.Max(s => s.Id.GetSegment().Info.m_halfWidth) * 2f;
                    var color = IsFar ? Colors.Red : Colors.Green;
                    Tool.MouseWorldPosition.RenderCircle(new OverlayData(cameraInfo) { Width = width, Color = color, RenderLimit = Underground });
                }

                if (IsSegments)
                {
                    foreach (var segment in Segments)
                    {
                        if (!segment.Equals(HoverSegment))
                            segment.Render(new OverlayData(cameraInfo) { RenderLimit = Underground });
                    }
                }
            }
            else
            {
                RenderNearNodes(cameraInfo);

                if (IsHoverNode)
                {
                    var color = HoverNode.Id.GetNode().CountSegments() >= 2 ? Colors.Green : Colors.Red;
                    HoverNode.Render(new OverlayData(cameraInfo) { Color = color, RenderLimit = Underground });
                }
                else
                    RenderSegmentNodes(cameraInfo, IsValidNode);
            }
        }
    }
}
