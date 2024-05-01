using System.Collections;
using System.Collections.Generic;
using Godot;

public static class NoiseFilterFactory
{
    public static INoiseFilter CreateNoiseFilter(NoiseSettings settings)
    {
        switch (settings.filterType)
        {
            case NoiseSettings.FilterType.Simple:
                return new SimpleNoiseFilter(settings);
            case NoiseSettings.FilterType.Ridged:
                return new RidgedNoiseFilter(settings);
            case NoiseSettings.FilterType.Warped:
                return new WarpedNoiseFilter(settings);
        }
        return null;
    }
}
