using System.Collections;
using System.Collections.Generic;
using Godot;

public class ColorGenerator
{
    ColorSettings settings;
    const int textureResolution = 50;
    INoiseFilter biomeNoiseFilter;
    Image _image;

    public void UpdateSettings(ColorSettings settings)
    {
        this.settings = settings;
        biomeNoiseFilter = NoiseFilterFactory.CreateNoiseFilter(settings.biomeColourSettings.noise);
    }

    public void UpdateElevation(MinMax elevationMinMax)
    {
        settings.elevation = elevationMinMax;
    }

    public float BiomePercentFromPoint(Vector3 pointOnUnitSphere)
    {
        return BiomeIndexFromPoint(pointOnUnitSphere) / Mathf.Max(1, settings.biomeColourSettings.biomes.Length - 1);
    }

    public float BiomeIndexFromPoint(Vector3 pointOnUnitSphere) 
    {
        float heightPercent = (pointOnUnitSphere.Y + 1) / 2f;
        heightPercent += (biomeNoiseFilter.Evaluate(pointOnUnitSphere) - settings.biomeColourSettings.noiseOffset) * settings.biomeColourSettings.noiseStrength;
        float biomeIndex = 0;
        int numBiomes = settings.biomeColourSettings.biomes.Length;
        float blendRange = settings.biomeColourSettings.blendAmount / 2f + .001f;
        for (int i = 0; i < numBiomes; i++)
        {
            float dst = heightPercent - settings.biomeColourSettings.biomes[i].startHeight;
            float weight = Mathf.Clamp(Mathf.InverseLerp(-blendRange, blendRange, dst),0,1);
            biomeIndex *= (1 - weight);
            biomeIndex += i * weight;
        }

        return biomeIndex;

    }

    public Color BiomeColorFromPoint(Vector3 pointOnUnitSphere, float height)
    {
        height = (height + 1f) / 2f;

        float index = BiomeIndexFromPoint(pointOnUnitSphere);
        int baseIndex = Mathf.FloorToInt(index);
        float remainder = index - (float)baseIndex;
        if (settings.biomeColourSettings.biomes.Length > baseIndex + 1 && remainder > 0) {
            return settings.biomeColourSettings.biomes[baseIndex].gradient.Sample(height / settings.elevation.Max)
                .Lerp(settings.biomeColourSettings.biomes[baseIndex + 1].gradient.Sample(height / settings.elevation.Max), remainder);
        } else {
            return settings.biomeColourSettings.biomes[baseIndex].gradient.Sample(height / settings.elevation.Max);
        }
    }

    public Color OceanColorFromPoint(Vector3 pointOnUnitSphere)
    {
        var color = settings.oceanColor.Sample(0.5f);
        color.A = 0.5f;
        return color;
    }
}