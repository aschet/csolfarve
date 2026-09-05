// SPDX-FileCopyrightText: 2026 Thomas Ascher <thomas.ascher@gmx.at>
//
// SPDX-License-Identifier: MIT

using System.Drawing;

namespace Olfarve.Tests;

public class SRGBColorTests
{
    [Fact]
    public void SupportsValueEqualityAndDeconstruction()
    {
        SRGBColor color = BeerColor.SrmToSrgb(10);
        Assert.Equal(color, new SRGBColor(color.R, color.G, color.B));
        (double r, double g, double b) = color;
        Assert.Equal((color.R, color.G, color.B), (r, g, b));
    }

    [Fact]
    public void ToHexEncodesFullPrecisionColors()
    {
        Assert.Equal("#ff8000", new SRGBColor(1.0, 0.5, 0.0).ToHex());
        Assert.Equal("#000000", new SRGBColor(0.0, 0.0, 0.0).ToHex());
    }

    [Fact]
    public void ToRgb8QuantizesToByteComponents() =>
        Assert.Equal(((byte)255, (byte)128, (byte)0), new SRGBColor(1.0, 0.5, 0.0).ToRgb8());

    [Theory]
    [InlineData(1.0, "#ffffff")]
    [InlineData(0.0, "#000000")]
    public void HexEndpoints(double component, string expected) =>
        Assert.Equal(expected, new SRGBColor(component, component, component).ToHex());

    [Fact]
    public void HexOutputIsLowercaseAndPadded()
    {
        string text = new SRGBColor(0.04, 0.04, 0.04).ToHex();
        Assert.Equal(text, text.ToLowerInvariant());
        Assert.Equal(7, text.Length);
    }

    [Fact]
    public void HexAgreesWithRgb8()
    {
        for (int srm = 0; srm <= 60; srm++)
        {
            SRGBColor color = BeerColor.SrmToSrgb(srm);
            (byte r, byte g, byte b) = color.ToRgb8();
            string expected = $"#{r:x2}{g:x2}{b:x2}";
            Assert.Equal(expected, color.ToHex());
        }
    }

    [Theory]
    [InlineData(2.0, "#ff0000")]
    [InlineData(-1.0, "#000000")]
    [InlineData(1.5, "#ff0000")]
    public void OutOfGamutComponentsAreClamped(double component, string expected)
    {
        SRGBColor color = component > 0
            ? new SRGBColor(component, 0.0, 0.0)
            : new SRGBColor(component, component, component);
        Assert.Equal(7, color.ToHex().Length);
        Assert.Equal(expected, color.ToHex());
    }

    [Fact]
    public void ToColorMatchesToRgb8()
    {
        SRGBColor color = BeerColor.SrmToSrgb(10);
        (byte r, byte g, byte b) = color.ToRgb8();
        Assert.Equal(Color.FromArgb(r, g, b), color.ToColor());
    }

    [Fact]
    public void ToColorIsOpaque() =>
        Assert.Equal(255, new SRGBColor(1.0, 0.5, 0.0).ToColor().A);

    [Fact]
    public void ToColorClampsOutOfGamutComponents() =>
        Assert.Equal(Color.FromArgb(255, 0, 0), new SRGBColor(2.0, -1.0, 0.0).ToColor());
}
