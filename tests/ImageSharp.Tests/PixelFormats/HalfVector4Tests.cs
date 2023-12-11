// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;
using System.Runtime.CompilerServices;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;

namespace SixLabors.ImageSharp.Tests.PixelFormats;

[Trait("Category", "PixelFormats")]
public class HalfVector4Tests
{
    [Fact]
    public void HalfVector4_PackedValue()
    {
        Assert.Equal(0uL, new HalfVector4(Vector4.Zero).PackedValue);
        Assert.Equal(4323521613979991040uL, new HalfVector4(Vector4.One).PackedValue);
        Assert.Equal(13547034390470638592uL, new HalfVector4(-Vector4.One).PackedValue);
        Assert.Equal(15360uL, new HalfVector4(Vector4.UnitX).PackedValue);
        Assert.Equal(1006632960uL, new HalfVector4(Vector4.UnitY).PackedValue);
        Assert.Equal(65970697666560uL, new HalfVector4(Vector4.UnitZ).PackedValue);
        Assert.Equal(4323455642275676160uL, new HalfVector4(Vector4.UnitW).PackedValue);
        Assert.Equal(4035285078724390502uL, new HalfVector4(0.1f, 0.3f, 0.4f, 0.5f).PackedValue);
    }

    [Fact]
    public void HalfVector4_ToVector4()
    {
        Assert.Equal(Vector4.Zero, new HalfVector4(Vector4.Zero).ToVector4());
        Assert.Equal(Vector4.One, new HalfVector4(Vector4.One).ToVector4());
        Assert.Equal(-Vector4.One, new HalfVector4(-Vector4.One).ToVector4());
        Assert.Equal(Vector4.UnitX, new HalfVector4(Vector4.UnitX).ToVector4());
        Assert.Equal(Vector4.UnitY, new HalfVector4(Vector4.UnitY).ToVector4());
        Assert.Equal(Vector4.UnitZ, new HalfVector4(Vector4.UnitZ).ToVector4());
        Assert.Equal(Vector4.UnitW, new HalfVector4(Vector4.UnitW).ToVector4());
    }

    [Fact]
    public void HalfVector4_ToScaledVector4()
    {
        // arrange
        HalfVector4 halfVector4 = new(-Vector4.One);

        // act
        Vector4 actual = halfVector4.ToScaledVector4();

        // assert
        Assert.Equal(0, actual.X);
        Assert.Equal(0, actual.Y);
        Assert.Equal(0, actual.Z);
        Assert.Equal(0, actual.W);
    }

    [Fact]
    public void HalfVector4_FromScaledVector4()
    {
        // arrange
        HalfVector4 halfVector4 = default;
        Vector4 scaled = new HalfVector4(-Vector4.One).ToScaledVector4();
        ulong expected = 13547034390470638592uL;

        // act
        halfVector4.FromScaledVector4(scaled);
        ulong actual = halfVector4.PackedValue;

        // assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void HalfVector4_FromBgra5551()
    {
        // arrange
        HalfVector4 halfVector4 = default;
        Vector4 expected = Vector4.One;

        // act
        halfVector4.FromBgra5551(new Bgra5551(1.0f, 1.0f, 1.0f, 1.0f));

        // assert
        Assert.Equal(expected, halfVector4.ToScaledVector4());
    }

    [Fact]
    public void HalfVector4_PixelInformation()
    {
        PixelTypeInfo info = HalfVector4.GetPixelTypeInfo();
        Assert.Equal(Unsafe.SizeOf<HalfVector4>() * 8, info.BitsPerPixel);
        Assert.Equal(4, info.ComponentCount);
        Assert.Equal(PixelAlphaRepresentation.Unassociated, info.AlphaRepresentation);
        Assert.Equal(PixelComponentPrecision.Half, info.MaxComponentPrecision);
    }
}
