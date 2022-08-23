﻿// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from um/d2d1_1.h in the Windows SDK for Windows 10.0.22000.0
// Original source is Copyright © Microsoft. All rights reserved.

namespace TerraFX.Interop.DirectX
{
    internal unsafe partial struct D2D1_BITMAP_PROPERTIES1
    {
        public D2D1_PIXEL_FORMAT pixelFormat;
        public float dpiX;
        public float dpiY;
        public D2D1_BITMAP_OPTIONS bitmapOptions;
        public ID2D1ColorContext* colorContext;
    }
}