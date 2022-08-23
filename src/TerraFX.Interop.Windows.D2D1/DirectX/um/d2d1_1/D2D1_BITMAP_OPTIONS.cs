﻿// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from um/d2d1_1.h in the Windows SDK for Windows 10.0.22000.0
// Original source is Copyright © Microsoft. All rights reserved.

using System;

namespace TerraFX.Interop.DirectX
{
    [Flags]
    internal enum D2D1_BITMAP_OPTIONS : uint
    {
        D2D1_BITMAP_OPTIONS_NONE = 0x00000000,
        D2D1_BITMAP_OPTIONS_TARGET = 0x00000001,
        D2D1_BITMAP_OPTIONS_CANNOT_DRAW = 0x00000002,
        D2D1_BITMAP_OPTIONS_CPU_READ = 0x00000004,
        D2D1_BITMAP_OPTIONS_GDI_COMPATIBLE = 0x00000008,
        D2D1_BITMAP_OPTIONS_FORCE_DWORD = 0xffffffff,
    }
}