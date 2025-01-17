﻿using ColossalFramework.UI;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NetworkMultitool.Utilities
{
    public static class NetworkMultitoolTextures
    {
        public static UITextureAtlas Atlas;
        public static Texture2D Texture => Atlas.texture;

        public static string ButtonNormal => nameof(ButtonNormal);
        public static string ButtonActive => nameof(ButtonActive);
        public static string ButtonHover => nameof(ButtonHover);
        public static string Icon => nameof(Icon);
        public static string IconActive => nameof(IconActive);
        public static string IconHover => nameof(IconHover);

        public static string UUINormal => nameof(UUINormal);
        public static string UUIHovered => nameof(UUIHovered);
        public static string UUIPressed => nameof(UUIPressed);
        //public static string UUIDisabled => nameof(UUIDisabled);

        private static Dictionary<string, TextureHelper.SpriteParamsGetter> Files { get; } = new Dictionary<string, TextureHelper.SpriteParamsGetter>
        {
            {nameof(Button), Button},
            {nameof(UUIButton), UUIButton},
            {nameof(ModeButtons), ModeButtons},
        };

        static NetworkMultitoolTextures()
        {
            Atlas = TextureHelper.CreateAtlas(nameof(NetworkMultitool), Files);
        }

        private static UITextureAtlas.SpriteInfo[] Button(int texWidth, int texHeight, Rect rect) => TextureHelper.GetSpritesInfo(texWidth, texHeight, rect, 31, 31, ButtonNormal, ButtonActive, ButtonHover, Icon, IconActive, IconHover).ToArray();

        private static UITextureAtlas.SpriteInfo[] UUIButton(int texWidth, int texHeight, Rect rect) => TextureHelper.GetSpritesInfo(texWidth, texHeight, rect, 40, 40, UUINormal, UUIHovered, UUIPressed/*, UUIDisabled*/).ToArray();

        private static UITextureAtlas.SpriteInfo[] ModeButtons(int texWidth, int texHeight, Rect rect)
        {
            var sprites = NetworkMultitoolTool.ModeTypes.Select(m => m.ToString()).ToArray();
            return TextureHelper.GetSpritesInfo(texWidth, texHeight, rect, 25, 25, new RectOffset(4, 4, 4, 4), 2, sprites).ToArray();
        }
    }
}
