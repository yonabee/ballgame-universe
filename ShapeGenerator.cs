using System.Collections;
using System.Collections.Generic;
using Godot;

public struct Elevation
{
    public float unscaled;
    public float scaled;
}

public class ShapeGenerator {

    public ShapeSettings settings;
    INoiseFilter[] noiseFilters;
    public MinMax elevationMinMax;

    [System.Serializable]
    public class ShapeSettings {
        public float radius = 1;
        public float mass = 1;
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

    public Elevation GetElevation(Vector3 pointOnUnitSphere) {
        Elevation result = new Elevation();
        result.unscaled = CalculateUnscaledElevation(pointOnUnitSphere);
        result.scaled = GetScaledElevation(result.unscaled);
        return result;
    }

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
                elevation += noiseFilters[i].Evaluate(pointOnUnitSphere) * mask;
            }
        }

        elevationMinMax.AddValue(elevation);
        return elevation;
    }

    float GetScaledElevation(float unscaledElevation) 
    {
        float elevation = Mathf.Max(0, unscaledElevation);
        elevation = settings.radius * (1 + unscaledElevation);

        return elevation;
    }
}