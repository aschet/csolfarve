# csolfarve

[![NuGet version](https://img.shields.io/nuget/v/Olfarve)](https://www.nuget.org/packages/Olfarve/)

*Øl farve* ("beer color") renders SRM and EBC beer color values as sRGB
colors, following the spectral model described by A. J. de Lange, "Color," in
*Brewing Materials and Processes*, Elsevier, 2016, pp. 199-249.

Given a color value and an optical path length (the width of the glass the
beer is viewed through), the sample's spectral transmittance is derived from
its absorption coefficient at 430 nm via the Beer-Lambert law, integrated
against the CIE 1931 color matching functions of the 2 degree standard
colorimetric observer under illuminant D65, and the resulting XYZ tristimulus
values are transformed to sRGB.

## Installation

```bash
dotnet add package Olfarve
```

The package targets .NET 8 and .NET 10 and has no runtime dependencies. It is
fully cross-platform (Windows, Linux, macOS).

## Usage

```csharp
using Olfarve;
using System.Drawing;

BeerColor.SrmToSrgb(10).ToHex();
BeerColor.EbcToSrgb(20).ToHex();

// The default path length is 5 cm, the width of a typical sample glass
BeerColor.SrmToSrgb(10, pathLengthCm: 1.0).ToHex();

// Results are SRGBColor records of gamma encoded components in [0, 1]
SRGBColor color = BeerColor.SrmToSrgb(10);
var (r, g, b) = color;
color.ToRgb8();
Color drawingColor = color.ToColor();

// Or start from an absorbance measured at 430 nm
BeerColor.AbsorptionToSrgb(0.7874);
```

## Development

Requires the [.NET 8 and .NET 10 SDKs](https://dotnet.microsoft.com/download)
(both, to build and test every target framework).

```bash
dotnet build
dotnet test
dotnet format --verify-no-changes
```
