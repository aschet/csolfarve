// SPDX-FileCopyrightText: 2026 Thomas Ascher <thomas.ascher@gmx.at>
//
// SPDX-License-Identifier: MIT

using System.Drawing;
using System.Globalization;

namespace Olfarve;

/// <summary>
/// An sRGB color, gamma encoded, with components in <c>[0, 1]</c>.
/// </summary>
/// <param name="R">The gamma encoded red component.</param>
/// <param name="G">The gamma encoded green component.</param>
/// <param name="B">The gamma encoded blue component.</param>
/// <remarks>
/// An immutable value type, so it compares and deconstructs like a plain triplet:
/// <code>
/// var (r, g, b) = BeerColor.SrmToSrgb(10);
/// </code>
/// </remarks>
public readonly record struct SRGBColor(double R, double G, double B)
{
    /// <summary>
    /// Returns the color quantized to 8 bits per channel.
    /// </summary>
    /// <remarks>
    /// Components are clamped into gamut first, so the result is a valid 8 bit triplet even for
    /// an instance built by hand out of range.
    /// <code>
    /// new SRGBColor(1.0, 0.5, 0.0).ToRgb8(); // (255, 128, 0)
    /// new SRGBColor(2.0, -1.0, 0.0).ToRgb8(); // (255, 0, 0)
    /// </code>
    /// </remarks>
    public (byte R, byte G, byte B) ToRgb8() => (To8Bit(R), To8Bit(G), To8Bit(B));

    /// <summary>
    /// Returns the color as a <c>#rrggbb</c> string.
    /// </summary>
    /// <remarks>
    /// <code>
    /// new SRGBColor(1.0, 0.5, 0.0).ToHex(); // "#ff8000"
    /// new SRGBColor(2.0, -1.0, 0.0).ToHex(); // "#ff0000"
    /// </code>
    /// </remarks>
    public string ToHex()
    {
        (byte r, byte g, byte b) = ToRgb8();
        return string.Create(CultureInfo.InvariantCulture, $"#{r:x2}{g:x2}{b:x2}");
    }

    /// <summary>
    /// Converts the color to a <see cref="Color"/>, quantized to 8 bits per channel.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Components are clamped into gamut first, via <see cref="ToRgb8"/>, so the result is a
    /// valid opaque color even for an instance built by hand out of range.
    /// <code>
    /// BeerColor.SrmToSrgb(10).ToColor(); // Color [A=255, R=186, G=91, B=0]
    /// </code>
    /// </para>
    /// <para>
    /// <see cref="Color"/> and the other primitive types in the <see cref="System.Drawing"/>
    /// namespace ship as part of the shared framework on every platform .NET supports (Windows,
    /// Linux and macOS): unlike the GDI+-backed types in that namespace such as
    /// <c>Bitmap</c> or <c>Graphics</c>, they are plain value types with no native dependency, so
    /// this conversion needs no extra package and no platform-specific handling.
    /// </para>
    /// </remarks>
    /// <returns>An opaque <see cref="Color"/> with the same, quantized, components.</returns>
    public Color ToColor()
    {
        (byte r, byte g, byte b) = ToRgb8();
        return Color.FromArgb(r, g, b);
    }

    /// <summary>Quantizes one gamma encoded component to a byte, clamping it to <c>[0, 255]</c>.</summary>
    private static byte To8Bit(double component) =>
        (byte)Math.Clamp(Math.Round(component * 255.0), 0.0, 255.0);
}
