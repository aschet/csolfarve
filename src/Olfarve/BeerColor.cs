// SPDX-FileCopyrightText: 2026 Thomas Ascher <thomas.ascher@gmx.at>
//
// SPDX-License-Identifier: MIT

namespace Olfarve;

/// <summary>
/// sRGB rendering of SRM and EBC beer color values.
/// </summary>
/// <remarks>
/// <para>
/// The spectral model is A. J. de Lange, "Color," in <i>Brewing Materials and Processes</i>,
/// Elsevier, 2016, pp. 199-249: beer's transmittance across the visible range is approximated
/// from its absorption at 430 nm. Integrating that against the CIE 1931 color matching functions
/// under illuminant D65 gives XYZ tristimulus values, which are then transformed to sRGB.
/// </para>
/// <para>
/// The sRGB primaries, white point and gamma encoding follow
/// https://www.w3.org/Graphics/Color/srgb. The colorimetric data is documented in
/// <see cref="Cie"/>.
/// </para>
/// </remarks>
public static class BeerColor
{
    /// <summary>
    /// Default optical path length in cm, set to the typical sample glass width specified by the
    /// BJCP color guide.
    /// </summary>
    /// <remarks>https://www.bjcp.org/education-training/education-resources/color-guide</remarks>
    public const double DefaultPathLengthCm = 5.0;

    // Both scales are defined as a multiple of the absorbance at 430 nm measured over a 1 cm
    // path: SRM = 12.7 * A430 and EBC = 25.0 * A430.
    private const double SrmPerAbsorbance = 12.7;
    private const double EbcPerAbsorbance = 25.0;

    // The de Lange approximation sums two exponentials decaying away from 430 nm, giving
    // absorption at any wavelength relative to the absorption there.
    private const double ReferenceWavelengthNm = 430.0;
    private const double ShortDecayWeight = 0.02465;
    private const double ShortDecayNm = 17.591;
    private const double LongDecayWeight = 0.97535;
    private const double LongDecayNm = 82.122;

    // Piecewise sRGB gamma encoding: linear below the threshold, a power law above it.
    private const double GammaThreshold = 0.0031308;
    private const double GammaSlope = 12.92;
    private const double GammaScale = 1.055;
    private const double GammaOffset = 0.055;
    private const double GammaExponent = 1.0 / 2.4;

    private static readonly double K = CalculateK();
    private static readonly SpectrumEntry[] Spectrum = BuildSpectrum();

    /// <summary>Precomputed wavelength dependent terms of the integration.</summary>
    /// <remarks>
    /// Only the absorbance varies between conversions. The absorption ratios and the colorimetric
    /// weights depend solely on wavelength, so they are evaluated once, statically, rather than on
    /// every call.
    /// </remarks>
    private readonly record struct SpectrumEntry(
        double AbsorptionRatio, double SD65, double XBar, double YBar, double ZBar);

    /// <summary>
    /// Converts a beer's absorption at 430 nm into an sRGB color.
    /// </summary>
    /// <remarks>
    /// Prefer <see cref="SrmToSrgb"/> or <see cref="EbcToSrgb"/> when you have a color value,
    /// which is what brewing software reports. This method is for a photometer reading taken
    /// directly, where the absorbance is the measurement and the SRM or EBC value is derived from
    /// it.
    /// <code>
    /// BeerColor.AbsorptionToSrgb(10.0 / 12.7).ToHex(); // "#ba5b00"
    /// </code>
    /// </remarks>
    /// <param name="absorption430">
    /// Linear decadic absorption coefficient at 430 nm, in cm^-1. Numerically this is the
    /// ASBC/EBC absorbance A430, which is defined for a 1 cm path length.
    /// </param>
    /// <param name="pathLengthCm">Optical path length in cm, e.g. the glass width.</param>
    /// <returns>The gamma encoded color, with components in <c>[0, 1]</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Either argument is negative.</exception>
    public static SRGBColor AbsorptionToSrgb(
        double absorption430, double pathLengthCm = DefaultPathLengthCm)
    {
        if (absorption430 < 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(absorption430), absorption430, "Value must not be negative.");
        }

        if (pathLengthCm < 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pathLengthCm), pathLengthCm, "Value must not be negative.");
        }

        // Beer-Lambert law: absorbance A = a * l, and transmittance T = 10 ^ -A.
        double absorbance430 = absorption430 * pathLengthCm;

        double tristimulusX = 0.0;
        double tristimulusY = 0.0;
        double tristimulusZ = 0.0;
        foreach (SpectrumEntry entry in Spectrum)
        {
            double transmittedPower =
                entry.SD65 * Math.Pow(10.0, -absorbance430 * entry.AbsorptionRatio);
            tristimulusX += transmittedPower * entry.XBar;
            tristimulusY += transmittedPower * entry.YBar;
            tristimulusZ += transmittedPower * entry.ZBar;
        }

        tristimulusX *= K;
        tristimulusY *= K;
        tristimulusZ *= K;

        // XYZ to linear sRGB, D65 white point.
        return new SRGBColor(
            EncodeGamma(
                (tristimulusX * 3.2406255) + (tristimulusY * -1.537208) + (tristimulusZ * -0.4986286)),
            EncodeGamma(
                (tristimulusX * -0.9689307) + (tristimulusY * 1.8757561) + (tristimulusZ * 0.0415175)),
            EncodeGamma(
                (tristimulusX * 0.0557101) + (tristimulusY * -0.2040211) + (tristimulusZ * 1.0569959)));
    }

    /// <summary>
    /// Converts a Standard Reference Method color value into an sRGB color.
    /// </summary>
    /// <remarks>
    /// <code>
    /// BeerColor.SrmToSrgb(10).ToHex(); // "#ba5b00"
    /// </code>
    /// </remarks>
    /// <param name="srm">The SRM color value.</param>
    /// <param name="pathLengthCm">Optical path length in cm, e.g. the glass width.</param>
    /// <returns>The gamma encoded color, with components in <c>[0, 1]</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Either argument is negative.</exception>
    public static SRGBColor SrmToSrgb(double srm, double pathLengthCm = DefaultPathLengthCm) =>
        AbsorptionToSrgb(srm / SrmPerAbsorbance, pathLengthCm);

    /// <summary>
    /// Converts a European Brewery Convention color value into an sRGB color.
    /// </summary>
    /// <remarks>
    /// <code>
    /// BeerColor.EbcToSrgb(20).ToHex(); // "#b95900"
    /// </code>
    /// </remarks>
    /// <param name="ebc">The EBC color value.</param>
    /// <param name="pathLengthCm">Optical path length in cm, e.g. the glass width.</param>
    /// <returns>The gamma encoded color, with components in <c>[0, 1]</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Either argument is negative.</exception>
    public static SRGBColor EbcToSrgb(double ebc, double pathLengthCm = DefaultPathLengthCm) =>
        AbsorptionToSrgb(ebc / EbcPerAbsorbance, pathLengthCm);

    /// <summary>Returns the normalizing constant for illuminant D65.</summary>
    /// <remarks>
    /// CIE defines <c>k = 100 / sum(S(lambda) * y_bar(lambda))</c>, putting the luminance of a
    /// perfectly transmitting sample at 100. Dropping the factor of 100 puts it at 1.0 instead,
    /// which is the range sRGB expects.
    /// </remarks>
    private static double CalculateK()
    {
        double luminance = 0.0;
        foreach (CieSample sample in Cie.Samples)
        {
            luminance += sample.SD65 * sample.YBar;
        }

        return 1.0 / luminance;
    }

    /// <summary>Returns absorption at <paramref name="wavelengthNm"/> relative to that at 430 nm.</summary>
    private static double AbsorptionRatio(double wavelengthNm)
    {
        double offsetNm = wavelengthNm - ReferenceWavelengthNm;
        return (ShortDecayWeight * Math.Exp(-offsetNm / ShortDecayNm))
            + (LongDecayWeight * Math.Exp(-offsetNm / LongDecayNm));
    }

    private static SpectrumEntry[] BuildSpectrum()
    {
        var spectrum = new SpectrumEntry[Cie.Samples.Length];
        double wavelengthNm = Cie.FirstWavelengthNm;
        for (int i = 0; i < Cie.Samples.Length; i++)
        {
            CieSample sample = Cie.Samples[i];
            spectrum[i] = new SpectrumEntry(
                AbsorptionRatio(wavelengthNm), sample.SD65, sample.XBar, sample.YBar, sample.ZBar);
            wavelengthNm += Cie.WavelengthStepNm;
        }

        return spectrum;
    }

    /// <summary>Gamma encodes one linear component, clamping it to <c>[0, 1]</c> first.</summary>
    /// <remarks>
    /// This is the inverse of the sRGB EOTF: it maps a linear tristimulus component to the
    /// non-linear signal a display decodes.
    /// </remarks>
    private static double EncodeGamma(double linear)
    {
        linear = Math.Clamp(linear, 0.0, 1.0);
        return linear <= GammaThreshold
            ? linear * GammaSlope
            : (GammaScale * Math.Pow(linear, GammaExponent)) - GammaOffset;
    }
}
