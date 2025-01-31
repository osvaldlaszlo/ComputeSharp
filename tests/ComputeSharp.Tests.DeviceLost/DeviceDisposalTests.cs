﻿using System;
using ComputeSharp.Interop;
using ComputeSharp.Tests.Attributes;
using ComputeSharp.Tests.DeviceLost.Helpers;
using ComputeSharp.Tests.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace ComputeSharp.Tests.DeviceLost;

[TestClass]
[TestCategory("DeviceDisposal")]
public partial class DeviceDisposalTests
{
    [TestMethod]
    public void DeviceDisposal_GetDefault_ReferenceCounting()
    {
        using ComPtr<ID3D12Device> d3D12Device = default;

        using (GraphicsDevice graphicsDevice = GraphicsDevice.GetDefault())
        {
            GraphicsDeviceHelper.GetD3D12Device(graphicsDevice, in d3D12Device);
        }

        Assert.AreEqual(1u, GraphicsDeviceHelper.GetD3D12DeviceRefCount(d3D12Device));
    }

    [CombinatorialTestMethod]
    [AllDevices]
    public void DeviceDisposal_ReferenceCounting(Device device)
    {
        using ComPtr<ID3D12Device> d3D12Device = default;

        using (GraphicsDevice graphicsDevice = device.Get())
        {
            GraphicsDeviceHelper.GetD3D12Device(graphicsDevice, in d3D12Device);
        }

        Assert.AreEqual(1u, GraphicsDeviceHelper.GetD3D12DeviceRefCount(d3D12Device));
    }

    [CombinatorialTestMethod]
    [AllDevices]
    [ExpectedException(typeof(ObjectDisposedException))]
    public unsafe void DeviceDisposal_InteropServicesThrowsObjectDisposedException(Device device)
    {
        using ComPtr<ID3D12Device> d3D12Device = default;

        GraphicsDevice graphicsDevice = device.Get();

        graphicsDevice.Dispose();

        InteropServices.GetID3D12Device(graphicsDevice, Windows.__uuidof<ID3D12Device>(), (void**)d3D12Device.GetAddressOf());
    }

    [CombinatorialTestMethod]
    [AllDevices]
    public void DeviceDisposal_DisposedResources_ReferenceCounting(Device device)
    {
        using ComPtr<ID3D12Device> d3D12Device = default;

        using (GraphicsDevice graphicsDevice = device.Get())
        {
            using var r1 = graphicsDevice.AllocateConstantBuffer<float>(128);
            using var r2 = graphicsDevice.AllocateReadOnlyBuffer<float>(128);
            using var r3 = graphicsDevice.AllocateReadWriteBuffer<float>(128);
            using var r4 = graphicsDevice.AllocateReadOnlyTexture2D<float>(128, 128);
            using var r5 = graphicsDevice.AllocateReadWriteTexture2D<float>(128, 128);
            using var r6 = graphicsDevice.AllocateUploadBuffer<float>(128);
            using var r7 = graphicsDevice.AllocateReadBackBuffer<float>(128);
            using var r8 = graphicsDevice.AllocateUploadTexture2D<float>(128, 128);
            using var r9 = graphicsDevice.AllocateReadBackTexture2D<float>(128, 128);

            GraphicsDeviceHelper.GetD3D12Device(graphicsDevice, in d3D12Device);
        }

        Assert.AreEqual(1u, GraphicsDeviceHelper.GetD3D12DeviceRefCount(d3D12Device));
    }

    [CombinatorialTestMethod]
    [AllDevices]
    public void DeviceDisposal_DisposedResourcesAfterDevice_ReferenceCounting(Device device)
    {
        using ComPtr<ID3D12Device> d3D12Device = default;

        ConstantBuffer<float> r1;
        ReadOnlyBuffer<float> r2;
        ReadWriteBuffer<float> r3;
        ReadOnlyTexture2D<float> r4;
        ReadWriteTexture2D<float> r5;

        using (GraphicsDevice graphicsDevice = device.Get())
        {
            r1 = graphicsDevice.AllocateConstantBuffer<float>(128);
            r2 = graphicsDevice.AllocateReadOnlyBuffer<float>(128);
            r3 = graphicsDevice.AllocateReadWriteBuffer<float>(128);
            r4 = graphicsDevice.AllocateReadOnlyTexture2D<float>(128, 128);
            r5 = graphicsDevice.AllocateReadWriteTexture2D<float>(128, 128);

            GraphicsDeviceHelper.GetD3D12Device(graphicsDevice, in d3D12Device);
        }

        r1.Dispose();
        r2.Dispose();
        r3.Dispose();
        r4.Dispose();
        r5.Dispose();

        Assert.AreEqual(1u, GraphicsDeviceHelper.GetD3D12DeviceRefCount(d3D12Device));
    }

    [CombinatorialTestMethod]
    [AllDevices]
    public void DeviceDisposal_WithComputeShader_ReferenceCounting(Device device)
    {
        using ComPtr<ID3D12Device> d3D12Device = default;

        using (GraphicsDevice graphicsDevice = device.Get())
        {
            using var buffer = graphicsDevice.AllocateReadWriteBuffer<float>(128);

            using (ComputeContext context = graphicsDevice.CreateComputeContext())
            {
                context.For(buffer.Length, new InitializeShader(buffer));
            }

            GraphicsDeviceHelper.GetD3D12Device(graphicsDevice, in d3D12Device);
        }

        Assert.AreEqual(1u, GraphicsDeviceHelper.GetD3D12DeviceRefCount(d3D12Device));
    }

    [EmbeddedBytecode(DispatchAxis.X)]
    [AutoConstructor]
    private partial struct InitializeShader : IComputeShader
    {
        private readonly ReadWriteBuffer<float> buffer;

        public void Execute()
        {
            buffer[ThreadIds.X] = ThreadIds.X;
        }
    }

    [CombinatorialTestMethod]
    [AllDevices]
    public void DeviceDisposal_WithPixelShader_ReferenceCounting(Device device)
    {
        using ComPtr<ID3D12Device> d3D12Device = default;

        using (GraphicsDevice graphicsDevice = device.Get())
        {
            using var texture = graphicsDevice.AllocateReadWriteTexture2D<Rgba32, float4>(128, 128);

            using (ComputeContext context = graphicsDevice.CreateComputeContext())
            {
                context.ForEach(texture, default(HelloWorldShader));
            }

            GraphicsDeviceHelper.GetD3D12Device(graphicsDevice, in d3D12Device);
        }

        Assert.AreEqual(1u, GraphicsDeviceHelper.GetD3D12DeviceRefCount(d3D12Device));
    }

    [EmbeddedBytecode(DispatchAxis.XY)]
    private partial struct HelloWorldShader : IPixelShader<float4>
    {
        public float4 Execute()
        {
            float2 uv = ThreadIds.Normalized.XY;
            float3 col = 0.5f + 0.5f * Hlsl.Cos(new float3(uv, uv.X) + new float3(0, 2, 4));

            return new(col, 1f);
        }
    }
}
