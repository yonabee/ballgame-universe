
using System.Collections;
using System.Collections.Generic;
using Godot;

public class RidgedNoiseFilter : INoiseFilter {

    NoiseSettings settings;
    FastNoiseLite noise;

    public RidgedNoiseFilter(NoiseSettings settings)
    {
        this.settings = settings;
        noise = new FastNoiseLite();
        noise.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;
        noise.Frequency = settings.frequency;
        noise.FractalOctaves = 1;
    }

    public float Evaluate(Vector3 point)
    {
        float noiseValue = 0;
        float frequency = settings.frequency;
        float amplitude = 1;
        float weight = 1;

        for (int i = 0; i < settings.octaves; i++)
        {
            float v = 1 - Mathf.Abs(noise.GetNoise3Dv(point * frequency + settings.center));
            v *= v;
            v *= weight;
            weight = Mathf.Clamp(v * settings.weightMultiplier, 0, 1);

            noiseValue += v * amplitude;
            frequency *= settings.roughness;
            amplitude *= settings.persistence;
        }

        noiseValue = noiseValue - settings.minValue;
        return noiseValue * settings.strength;
    }
}