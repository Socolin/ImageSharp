// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

#nullable disable

using System.Buffers;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using SixLabors.ImageSharp.Formats.Tiff.Utils;
using SixLabors.ImageSharp.Memory;
using SixLabors.ImageSharp.PixelFormats;

namespace SixLabors.ImageSharp.Formats.Tiff.PhotometricInterpretation;

/// <summary>
/// Implements the 'RGB' photometric interpretation with an alpha channel and with 16 bits for each channel.
/// </summary>
/// <typeparam name="TPixel">The type of pixel format.</typeparam>
internal class Rgba16161616TiffColorSimd<TPixel> : TiffBaseColorDecoder<TPixel>
    where TPixel : unmanaged, IPixel<TPixel>
{
    private static readonly Vector128<byte> Be16ToLe16ShuffleMask128 = Vector128.Create((byte)1, 0, 3, 2, 5, 4, 7, 6, 9, 8, 11, 10, 13, 12, 15, 14);
    private static readonly Vector256<byte> Be16ToLe16ShuffleMask256 = Vector256.Create((byte)1, 0, 3, 2, 5, 4, 7, 6, 9, 8, 11, 10, 13, 12, 15, 14, 17, 16, 19, 18, 21, 20, 23, 22, 25, 24, 27, 26, 29, 28, 31, 30);
    private readonly bool isBigEndian;

    private readonly Configuration configuration;

    private readonly MemoryAllocator memoryAllocator;

    private readonly TiffExtraSampleType? extraSamplesType;

    /// <summary>
    /// Initializes a new instance of the <see cref="Rgba16161616TiffColorSimd{TPixel}" /> class.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <param name="memoryAllocator">The memory allocator.</param>
    /// <param name="extraSamplesType">The type of the extra samples.</param>
    /// <param name="isBigEndian">if set to <c>true</c> decodes the pixel data as big endian, otherwise as little endian.</param>
    public Rgba16161616TiffColorSimd(Configuration configuration, MemoryAllocator memoryAllocator, TiffExtraSampleType? extraSamplesType, bool isBigEndian)
    {
        this.configuration = configuration;
        this.isBigEndian = isBigEndian;
        this.memoryAllocator = memoryAllocator;
        this.extraSamplesType = extraSamplesType;
    }

    /// <inheritdoc/>
    public override void Decode(ReadOnlySpan<byte> data, Buffer2D<TPixel> pixels, int left, int top, int width, int height)
    {
        bool hasAssociatedAlpha = this.extraSamplesType.HasValue && this.extraSamplesType == TiffExtraSampleType.AssociatedAlphaData;
        int offset = 0;

        using IMemoryOwner<Vector4> vectors = hasAssociatedAlpha ? this.memoryAllocator.Allocate<Vector4>(width) : null;
        Span<Vector4> vectorsSpan = hasAssociatedAlpha ? vectors.GetSpan() : [];

        if (this.isBigEndian)
        {
            using IMemoryOwner<ushort> swappedData = this.memoryAllocator.Allocate<ushort>(width * 4);
            Span<ushort> swappedSourceBytes = swappedData!.GetSpan();

            for (int y = top; y < top + height; y++)
            {
                Span<TPixel> pixelRow = pixels.DangerousGetRowSpan(y).Slice(left, width);
                int byteCount = pixelRow.Length * 8;
                ReadOnlySpan<byte> sourceBytes = data.Slice(offset, byteCount);

                if (Avx2.IsSupported)
                {
                    int simdCount = byteCount / 32 * 32;
                    for (int i = 0; i < simdCount; i += 32)
                    {
                        Vector256<byte> sourceBytesVector = Vector256.LoadUnsafe(ref MemoryMarshal.GetReference(sourceBytes.Slice(i, 32)));
                        Vector256<byte> swapped = Avx2.Shuffle(sourceBytesVector, Be16ToLe16ShuffleMask256);
                        swapped.AsUInt16().StoreUnsafe(ref MemoryMarshal.GetReference(swappedSourceBytes!.Slice(i / 2, 16)));
                    }

                    for (int i = simdCount; i < byteCount; i += 2)
                    {
                        swappedSourceBytes[i / 2] = TiffUtilities.ConvertToUShortBigEndian(sourceBytes.Slice(i, 2));
                    }
                }
                else if (Ssse3.IsSupported)
                {
                    int simdCount = byteCount / 16 * 16;
                    for (int i = 0; i < simdCount; i += 16)
                    {
                        Vector128<byte> sourceBytesVector = Vector128.LoadUnsafe(ref MemoryMarshal.GetReference(sourceBytes.Slice(i, 16)));
                        Vector128<byte> swapped = Ssse3.Shuffle(sourceBytesVector, Be16ToLe16ShuffleMask128);
                        swapped.AsUInt16().StoreUnsafe(ref MemoryMarshal.GetReference(swappedSourceBytes!.Slice(i / 2, 8)));
                    }

                    for (int i = simdCount; i < byteCount; i += 2)
                    {
                        swappedSourceBytes[i / 2] = TiffUtilities.ConvertToUShortBigEndian(sourceBytes.Slice(i, 2));
                    }
                }
                else
                {
                    for (int i = 0; i < byteCount - 1; i += 2)
                    {
                        swappedSourceBytes[i / 2] = TiffUtilities.ConvertToUShortBigEndian(sourceBytes.Slice(i, 2));
                    }
                }

                PixelOperations<TPixel>.Instance.FromRgba64Bytes(
                    this.configuration,
                    MemoryMarshal.Cast<ushort, byte>(swappedSourceBytes),
                    pixelRow,
                    pixelRow.Length);

                if (hasAssociatedAlpha)
                {
                    PixelOperations<TPixel>.Instance.ToVector4(this.configuration, pixelRow, vectorsSpan);
                    PixelOperations<TPixel>.Instance.FromVector4Destructive(this.configuration, vectorsSpan, pixelRow, PixelConversionModifiers.Premultiply | PixelConversionModifiers.Scale);
                }

                offset += byteCount;
            }
        }
        else
        {
            for (int y = top; y < top + height; y++)
            {
                Span<TPixel> pixelRow = pixels.DangerousGetRowSpan(y).Slice(left, width);
                int byteCount = pixelRow.Length * 8;

                PixelOperations<TPixel>.Instance.FromRgba64Bytes(
                    this.configuration,
                    data.Slice(offset, byteCount),
                    pixelRow,
                    pixelRow.Length);

                if (hasAssociatedAlpha)
                {
                    PixelOperations<TPixel>.Instance.ToVector4(this.configuration, pixelRow, vectorsSpan);
                    PixelOperations<TPixel>.Instance.FromVector4Destructive(this.configuration, vectorsSpan, pixelRow, PixelConversionModifiers.Premultiply | PixelConversionModifiers.Scale);
                }

                offset += byteCount;
            }
        }
    }
}
