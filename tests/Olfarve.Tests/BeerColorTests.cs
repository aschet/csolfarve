// SPDX-FileCopyrightText: 2026 Thomas Ascher <thomas.ascher@gmx.at>
//
// SPDX-License-Identifier: MIT

namespace Olfarve.Tests;

public class BeerColorTests
{
    public static readonly TheoryData<double, string> SrmReferenceColors = new()
    {
        { 1, "#fae8b6" },
        { 2, "#f4d180" },
        { 4, "#e7aa31" },
        { 10, "#ba5b00" },
        { 20, "#7d1900" },
        { 30, "#540000" },
        { 40, "#390000" },
        { 50, "#270000" },
    };

    [Fact]
    public void UnabsorbingSampleRendersAsWhite()
    {
        const double tolerance = 1e-4;
        SRGBColor color = BeerColor.AbsorptionToSrgb(0.0);
        Assert.True(Math.Abs(color.R - 1.0) <= tolerance, $"R was {color.R}");
        Assert.True(Math.Abs(color.G - 1.0) <= tolerance, $"G was {color.G}");
        Assert.True(Math.Abs(color.B - 1.0) <= tolerance, $"B was {color.B}");
        Assert.Equal("#ffffff", color.ToHex());
    }

    [Theory]
    [MemberData(nameof(SrmReferenceColors))]
    public void SrmToSrgbMatchesReferenceColors(double srm, string expected) =>
        Assert.Equal(expected, BeerColor.SrmToSrgb(srm).ToHex());

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(25)]
    [InlineData(40)]
    public void EbcMatchesEquivalentSrm(double srm)
    {
        double ebc = srm * 25.0 / 12.7;
        SRGBColor expected = BeerColor.SrmToSrgb(srm);
        SRGBColor actual = BeerColor.EbcToSrgb(ebc);
        Assert.Equal(expected.R, actual.R, 9);
        Assert.Equal(expected.G, actual.G, 9);
        Assert.Equal(expected.B, actual.B, 9);
    }

    [Fact]
    public void ComponentsAreWithinUnitRange()
    {
        for (int srm = 0; srm <= 60; srm++)
        {
            SRGBColor color = BeerColor.SrmToSrgb(srm);
            Assert.InRange(color.R, 0.0, 1.0);
            Assert.InRange(color.G, 0.0, 1.0);
            Assert.InRange(color.B, 0.0, 1.0);
        }
    }

    [Fact]
    public void ColorDarkensMonotonicallyWithColorValue()
    {
        double previous = double.PositiveInfinity;
        for (int srm = 0; srm <= 40; srm++)
        {
            SRGBColor color = BeerColor.SrmToSrgb(srm);
            double luminance = color.R + color.G + color.B;
            Assert.True(luminance < previous);
            previous = luminance;
        }
    }

    [Fact]
    public void LongerPathLengthDarkensColor()
    {
        SRGBColor shortPath = BeerColor.SrmToSrgb(10, pathLengthCm: 1.0);
        SRGBColor longPath = BeerColor.SrmToSrgb(10, pathLengthCm: 10.0);
        Assert.True(longPath.R + longPath.G + longPath.B < shortPath.R + shortPath.G + shortPath.B);
    }

    [Fact]
    public void ZeroPathLengthIsWhite() =>
        Assert.Equal("#ffffff", BeerColor.SrmToSrgb(20, pathLengthCm: 0.0).ToHex());

    [Fact]
    public void DefaultPathLengthMatchesBjcpGlassWidth()
    {
        Assert.Equal(5.0, BeerColor.DefaultPathLengthCm);
        Assert.Equal(BeerColor.SrmToSrgb(10), BeerColor.SrmToSrgb(10, BeerColor.DefaultPathLengthCm));
    }

    [Theory]
    [InlineData(-0.1, 5.0)]
    [InlineData(1.0, -1.0)]
    public void AbsorptionToSrgbRejectsNegativeInput(double absorption430, double pathLengthCm) =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => BeerColor.AbsorptionToSrgb(absorption430, pathLengthCm));

    [Theory]
    [InlineData(-1, 5.0)]
    [InlineData(1, -1.0)]
    public void SrmToSrgbRejectsNegativeInput(double srm, double pathLengthCm) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => BeerColor.SrmToSrgb(srm, pathLengthCm));

    [Theory]
    [InlineData(-1, 5.0)]
    [InlineData(1, -1.0)]
    public void EbcToSrgbRejectsNegativeInput(double ebc, double pathLengthCm) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => BeerColor.EbcToSrgb(ebc, pathLengthCm));
}
