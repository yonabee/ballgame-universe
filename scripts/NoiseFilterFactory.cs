using System.Collections;
using System.Collections.Generic;
using Godot;

public static class NoiseFilterFactory
{
    public static INoiseFilter CreateNoiseFilter(NoiseSettings settings)
    {
        return settings.filterType switch
        {
            NoiseSettings.FilterType.Simple => new SimpleNoiseFilter(settings),
            NoiseSettings.FilterType.Ridged => new RidgedNoiseFilter(settings),
            NoiseSettings.FilterType.Warped => new WarpedNoiseFilter(settings),
            _ => null,
        };
    }
}
