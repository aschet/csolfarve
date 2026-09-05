// SPDX-FileCopyrightText: 2026 Thomas Ascher <thomas.ascher@gmx.at>
//
// SPDX-License-Identifier: MIT

namespace Olfarve;

/// <summary>
/// One wavelength sample of the CIE 1931 observer and the D65 illuminant.
/// </summary>
/// <remarks>
/// Field names follow CIE notation: <paramref name="XBar"/>, <paramref name="YBar"/> and
/// <paramref name="ZBar"/> are the color matching functions x(lambda), y(lambda) and z(lambda);
/// <paramref name="SD65"/> is the relative spectral power distribution S(lambda) of illuminant D65.
/// </remarks>
internal readonly record struct CieSample(double XBar, double YBar, double ZBar, double SD65);
