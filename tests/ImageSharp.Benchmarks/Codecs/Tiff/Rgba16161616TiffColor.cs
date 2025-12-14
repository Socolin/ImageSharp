// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using BenchmarkDotNet.Attributes;
using SixLabors.ImageSharp.Formats.Tiff;
using SixLabors.ImageSharp.Formats.Tiff.PhotometricInterpretation;
using SixLabors.ImageSharp.Memory;
using SixLabors.ImageSharp.PixelFormats;

namespace SixLabors.ImageSharp.Benchmarks.Codecs.Tiff;

[MarkdownExporter]
[HtmlExporter]
[Config(typeof(Config.Standard))]
public class Rgba16161616TiffColor
{
    private Memory<byte> buffer;
    private const int Width = 200;
    private const int Height = 200;

    [Params(true, false)]
    public bool AssociatedAlpha { get; set; }

    [Params(true, false)]
    public bool IsBigEndian { get; set; }

    private TiffExtraSampleType ExtraSampleType => this.AssociatedAlpha ? TiffExtraSampleType.AssociatedAlphaData : TiffExtraSampleType.UnassociatedAlphaData;

    [IterationSetup]
    public void SetupImages()
    {
        this.buffer = new byte[Width * Height * 8];
        Random.Shared.NextBytes(this.buffer.Span);
    }

    [Benchmark(Description = "Rgba16161616TiffColorSplit")]
    public void Rgba16161616TiffColorSplit()
    {
        Buffer2D<Rgba32> pixels = MemoryAllocator.Default.Allocate2D<Rgba32>(
            Width,
            Height,
            true,
            AllocationOptions.Clean
        );

        Rgba16161616TiffColor<Rgba32> colorDecoder = new(Configuration.Default, MemoryAllocator.Default, this.ExtraSampleType, this.IsBigEndian);
        colorDecoder.Decode(this.buffer.Span, pixels, 0, 0, Width, Height);
    }

    [Benchmark(Description = "Rgba16161616TiffColorOriginal")]
    public void Rgba16161616TiffColorOriginal()
    {
        Buffer2D<Rgba32> pixels = MemoryAllocator.Default.Allocate2D<Rgba32>(
            Width,
            Height,
            true,
            AllocationOptions.Clean
        );

        Rgba16161616TiffColorOriginal<Rgba32> colorDecoder = new(Configuration.Default, MemoryAllocator.Default, this.ExtraSampleType, this.IsBigEndian);
        colorDecoder.Decode(this.buffer.Span, pixels, 0, 0, Width, Height);
    }

    [Benchmark(Description = "Rgba16161616TiffColorSimd")]
    public void Rgba16161616TiffColorSimd()
    {
        Buffer2D<Rgba32> pixels = MemoryAllocator.Default.Allocate2D<Rgba32>(
            Width,
            Height,
            true,
            AllocationOptions.Clean
        );

        Rgba16161616TiffColorSimd<Rgba32> colorDecoder = new(Configuration.Default, MemoryAllocator.Default, this.ExtraSampleType, this.IsBigEndian);
        colorDecoder.Decode(this.buffer.Span, pixels, 0, 0, Width, Height);
    }
}
