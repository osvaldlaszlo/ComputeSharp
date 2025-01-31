﻿using System.IO;
using System.Reflection;
using ComputeSharp.BokehBlur.Processors;
using ComputeSharp.D2D1.Tests.Effects;
using ComputeSharp.D2D1.Tests.Helpers;
using ComputeSharp.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace ComputeSharp.D2D1.Tests;

[TestClass]
[TestCategory("EndToEnd")]
public class EndToEndTests
{
    [AssemblyInitialize]
    public static void ConfigureImageSharp(TestContext _)
    {
        Configuration.Default.PreferContiguousImageBuffers = true;
    }

    [TestMethod]
    public unsafe void Invert()
    {
        D2D1TestRunner.RunAndCompareShader(new InvertEffect(), null, "Landscape.png", "Landscape_Inverted.png");
    }

    [TestMethod]
    public unsafe void InvertWithThreshold()
    {
        D2D1TestRunner.RunAndCompareShader(new InvertWithThresholdEffect(1), null, "Landscape.png", "Landscape_Inverted.png");
    }

    [TestMethod]
    public unsafe void Pixelate()
    {
        D2D1TestRunner.RunAndCompareShader(
            new PixelateEffect.Shader(new PixelateEffect.Shader.Constants(1280, 840, 16)),
            static () => new PixelateEffect(),
            "Landscape.png",
            "Landscape_Pixelate.png");
    }

    [TestMethod]
    public unsafe void ZonePlate()
    {
        D2D1TestRunner.RunAndCompareShader(new ZonePlateEffect(1280, 720, 800), null, 1280, 720, "ZonePlate.png");
    }

    [TestMethod]
    public unsafe void CheckerboardClip()
    {
        D2D1TestRunner.RunAndCompareShader(new CheckerboardClipEffect(1280, 840, 32), null, "Landscape.png", "Landscape_CheckerboardClip.png");
    }

    [TestMethod]
    [DataRow(8, 1, "Landscape")]
    [DataRow(32, 3, "Landscape")]
    [DataRow(80, 1, "City")]
    [DataRow(80, 2, "City")]
    public void BokehBlur(int radius, int numberOfComponents, string filename)
    {
        string assetsPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "Assets");
        string temporaryPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "temp");
        string sourcePath = Path.Combine(assetsPath, Path.ChangeExtension(filename, "png"));
        string destinationPathForCpu = Path.Combine(temporaryPath, Path.ChangeExtension($"{filename}_{radius}_{numberOfComponents}_cpu", "png"));
        string destinationPathForGpu = Path.Combine(temporaryPath, Path.ChangeExtension($"{filename}_{radius}_{numberOfComponents}_gpu", "png"));

        _ = Directory.CreateDirectory(temporaryPath);

        using Image<Rgba32> original = Image.Load<Rgba32>(sourcePath);
        using Image<Rgba32> cpu = original.Clone(c => c.BokehBlur(radius, numberOfComponents, 3));

        cpu.SaveAsPng(destinationPathForCpu);

        new BokehBlurEffect(radius, numberOfComponents).ApplyEffect(sourcePath, destinationPathForGpu);

        TolerantImageComparer.AssertEqual(destinationPathForCpu, destinationPathForGpu, 0.00002f);
    }
}