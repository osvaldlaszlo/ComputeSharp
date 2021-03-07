using System;
using System.Numerics;
using System.Reflection;
using ComputeSharp.__Internals;
using ComputeSharp.Resources;
using ComputeSharp.Tests.Attributes;
using ComputeSharp.Tests.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#pragma warning disable CS0618

namespace ComputeSharp.Tests
{
    public partial class Texture2DTests
    {
        [CombinatorialTestMethod]
        [AllDevices]
        [Resource(typeof(ReadOnlyTexture2D<,>))]
        [Resource(typeof(ReadWriteTexture2D<,>))]
        [Data(typeof(Bgra32), typeof(Vector4))]
        [Data(typeof(Bgra32), typeof(Float4))]
        [Data(typeof(R16), typeof(float))]
        [Data(typeof(R8), typeof(float))]
        [Data(typeof(Rg16), typeof(Vector2))]
        [Data(typeof(Rg16), typeof(Float2))]
        [Data(typeof(Rg32), typeof(Vector2))]
        [Data(typeof(Rg32), typeof(Float2))]
        [Data(typeof(Rgba32), typeof(Vector4))]
        [Data(typeof(Rgba32), typeof(Float4))]
        [Data(typeof(Rgba64), typeof(Vector4))]
        [Data(typeof(Rgba64), typeof(Float4))]
        public void Allocate_Uninitialized_Pixel_Ok(Device device, Type textureType, Type t, Type tPixel)
        {
            static void Test<T, TPixel>(Device device, Type textureType)
                where T : unmanaged, IUnorm<TPixel>
                where TPixel : unmanaged
            {
                using Texture2D<T> texture = device.Get().AllocateTexture2D<T, TPixel>(textureType, 128, 128);

                Assert.IsNotNull(texture);
                Assert.AreEqual(texture.Width, 128);
                Assert.AreEqual(texture.Height, 128);
                Assert.AreSame(texture.GraphicsDevice, device.Get());

                if (textureType == typeof(ReadOnlyTexture2D<,>))
                {
                    Assert.IsTrue(texture is ReadOnlyTexture2D<T, TPixel>);
                }
                else
                {
                    Assert.IsTrue(texture is ReadWriteTexture2D<T, TPixel>);
                }
            }

            try
            {
                new Action<Device, Type>(Test<Rgba32, Vector4>).Method.GetGenericMethodDefinition().MakeGenericMethod(t, tPixel).Invoke(null, new object[] { device, textureType });
            }
            catch (TargetInvocationException e) when (e.InnerException is not null)
            {
                throw e.InnerException;
            }
        }
    }
}
