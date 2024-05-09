using System.Collections;
using System.Collections.Generic;
using Godot;

public struct Elevation
{
    public float unscaled;
    public float scaled;
}

public class ShapeGenerator
{
    public ShapeSettings settings;
    INoiseFilter[] noiseFilters;
    public MinMax elevationMinMax;
    public Vector3 Start = Vector3.Zero;

    readonly float _heightScaling = 1.1f;

    [System.Serializable]
    public class ShapeSettings
    {
        public float radius = 1;
        public NoiseSettings[] noiseSettings;
    }

    public void UpdateSettings(ShapeSettings settings)
    {
        this.settings = settings;
        noiseFilters = new INoiseFilter[settings.noiseSettings.Length];
        for (int i = 0; i < noiseFilters.Length; i++)
        {
            noiseFilters[i] = NoiseFilterFactory.CreateNoiseFilter(settings.noiseSettings[i]);
        }
        elevationMinMax = new MinMax();
    }

    public Elevation DetermineElevation(Vector3 pointOnUnitSphere)
    {
        Elevation result;
        result = new Elevation { unscaled = CalculateUnscaledElevation(pointOnUnitSphere) };
        result.scaled = CalculateScaledElevation(result.unscaled);
        if (Start == Vector3.Zero)
        {
            Start = pointOnUnitSphere;
        }
        return result;
    }

    // Elevation as a float from -1 to 1
    float CalculateUnscaledElevation(Vector3 pointOnUnitSphere)
    {
        float firstLayerValue = 0;
        float elevation = 0;

        if (noiseFilters.Length > 0)
        {
            firstLayerValue = noiseFilters[0].Evaluate(pointOnUnitSphere);
            if (settings.noiseSettings[0].enabled)
            {
                elevation = firstLayerValue;
            }
        }

        for (int i = 1; i < noiseFilters.Length; i++)
        {
            if (settings.noiseSettings[i].enabled)
            {
                float mask = (settings.noiseSettings[i].useFirstLayerAsMask) ? firstLayerValue : 1;
                elevation += noiseFilters[i].Evaluate(pointOnUnitSphere) * mask * _heightScaling;
            }
        }

        elevationMinMax.AddValue(elevation);
        return elevation;
    }

    // Elevation in units above sea level.
    float CalculateScaledElevation(float unscaledElevation)
    {
        float elevation = Mathf.Max(0, unscaledElevation);
        elevation = settings.radius * (1 + unscaledElevation);

        return elevation;
    }
}
