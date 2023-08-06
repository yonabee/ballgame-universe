using System.Collections;
using System.Collections.Generic;
using Godot;
using static ColorSettings.BiomeColourSettings;

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
        height += biomeNoiseFilter.Evaluate(pointOnUnitSphere) * 0.1f; //settings.biomeColourSettings.noiseStrength;
        var min = (settings.elevation.Min + 1f) / 2f;
        var range = settings.elevation.Max - settings.elevation.Min;
        float index = BiomeIndexFromPoint(pointOnUnitSphere);
        int baseIndex = Mathf.FloorToInt(index);
        float remainder = index - (float)baseIndex;
        float percent = (height - min) / range;
        //percent += biomeNoiseFilter.Evaluate(pointOnUnitSphere) * settings.biomeColourSettings.noiseStrength;
        Mathf.Clamp(percent, 0f, 1f);
        Biome biome = settings.biomeColourSettings.biomes[baseIndex];
        Color biomeColor;
        Color tintColor;
        if (settings.biomeColourSettings.biomes.Length > baseIndex + 1 && remainder > 0) {
            Biome nextBiome = settings.biomeColourSettings.biomes[baseIndex + 1];
            biomeColor = biome.gradient.Sample(percent).Lerp(nextBiome.gradient.Sample(percent), remainder);
            tintColor = biome.tint.Lerp(nextBiome.tint, remainder);
        } else {
            biomeColor = biome.gradient.Sample((height - min) / range);
            tintColor = biome.tint;
        }
        return biomeColor.Lerp(tintColor, biome.tintPercent);
    }

    public Color OceanColorFromPoint(Vector3 pointOnUnitSphere)
    {
        var color = settings.oceanColor;
        color.A = 0.5f;
        return color;
    }
}