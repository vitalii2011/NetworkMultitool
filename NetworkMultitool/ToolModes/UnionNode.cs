﻿using ModsCommon;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NetworkMultitool
{
    public class UnionNodeMode : BaseNetworkMultitoolMode
    {
        public override ToolModeType Type => ToolModeType.UnionNode;
        protected override bool IsReseted => !IsSource;

        protected override bool IsValidNode(ushort nodeId) => !IsSource || nodeId != Source.Id;

        private NodeSelection Source { get; set; }
        private bool IsSource => Source != null;
        private NodeSelection Target => HoverNode;
        private bool IsTarget => Target != null;

        private int Count
        {
            get
            {
                var count = 0;
                if (IsSource)
                    count += Source.Id.GetNode().CountSegments();
                if(IsTarget)
                    count += Target.Id.GetNode().CountSegments();
                return count;
            }
        }
        private bool IsCorrectCount => Count <= 8;
        private bool IsConnected
        {
            get
            {
                if (!IsTarget || !IsSource)
                    return false;
                else
                    return NetExtension.GetCommon(Source.Id, Target.Id, out _);
            }
        }

        protected override string GetInfo()
        {
            if (!IsSource)
            {
                if (IsHoverNode)
                    return Localize.Mode_UnionNode_Info_ClickSource + GetStepOverInfo();
                else
                    return Localize.Mode_UnionNode_Info_SelectSource;
            }
            else if (!IsTarget)
                return Localize.Mode_UnionNode_Info_SelectTarget;
            else if (IsConnected)
                return Localize.Mode_UnionNode_Info_NoCommon;
            else if (!IsCorrectCount)
                return Localize.Mode_UnionNode_Info_Overflow + GetStepOverInfo();
            else
                return Localize.Mode_UnionNode_Info_ClickUnion + GetStepOverInfo();
        }
        protected override void Reset(IToolMode prevMode)
        {
            base.Reset(prevMode);
            Source = null;
        }
        public override void OnPrimaryMouseClicked(Event e)
        {
            if (!IsHoverNode)
                return;
            else if (!IsSource)
                Source = HoverNode;
            else if(IsCorrectCount)
            {
                Union(Source.Id, Target.Id);
                Reset(this);
            }
        }
        public override void OnSecondaryMouseClicked()
        {
            if (IsSource)
                Source = null;
        }
        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            if (!IsSource)
            {
                if (IsHoverNode)
                    HoverNode.Render(new OverlayData(cameraInfo) { Color = Colors.Green });
                else
                    RenderSegmentNodes(cameraInfo, IsValidNode);
            }
            else if (!IsTarget)
            {
                Source.Render(new OverlayData(cameraInfo));
                RenderSegmentNodes(cameraInfo, IsValidNode);
            }
            else if (!IsCorrectCount || IsConnected)
            {
                Source.Render(new OverlayData(cameraInfo) { Color = Colors.Red });
                Target.Render(new OverlayData(cameraInfo) { Color = Colors.Red });
            }
            else
            {
                Source.Render(new OverlayData(cameraInfo) { Color = Colors.Green });
                Target.Render(new OverlayData(cameraInfo) { Color = Colors.Green });
            }
        }

        private bool Union(ushort sourceId, ushort targetId)
        {
            var sourceNode = sourceId.GetNode();
            var targetNode = targetId.GetNode();
            var segmentIds = sourceNode.SegmentIds().ToArray();

            foreach (var segmentId in segmentIds)
            {
                var segment = segmentId.GetSegment();
                var otherNodeId = segment.GetOtherNode(sourceId);
                var info = segment.Info;
                var otherDir = segment.IsStartNode(sourceId) ? segment.m_endDirection : segment.m_startDirection;
                var sourceDir = segment.IsStartNode(sourceId) ? segment.m_startDirection : segment.m_endDirection;
                var invert = segment.IsStartNode(sourceId) ^ segment.IsInvert();

                var otherNode = otherNodeId.GetNode();

                var oldDir = new StraightTrajectory(otherNode.m_position.MakeFlat(), sourceNode.m_position.MakeFlat());
                var newDir = new StraightTrajectory(otherNode.m_position.MakeFlat(), targetNode.m_position.MakeFlat());
                var angle = MathExtention.GetAngle(oldDir.Direction, newDir.Direction);

                otherDir = otherDir.TurnRad(angle, false);
                sourceDir = sourceDir.TurnRad(angle, false);

                RemoveSegment(segmentId);
                CreateSegment(out _, info, otherNodeId, targetId, otherDir, sourceDir, invert);
            }

            RemoveNode(sourceId);
            return true;
        }
    }
}