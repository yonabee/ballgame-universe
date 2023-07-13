using System.Collections;
using System.Collections.Generic;
using Godot;

public class SimpleNoiseFilter : INoiseFilter {

    NoiseSettings settings;
    FastNoiseLite noise;

    public SimpleNoiseFilter(NoiseSettings settings)
    {
        this.settings = settings;
        noise = new FastNoiseLite();
        noise.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;
        noise.Frequency = settings.baseRoughness;
        noise.FractalOctaves = 1;
        noise.Seed = settings.seed;
    }

    public float Evaluate(Vector3 point)
    {
        float noiseValue = 0;
        float frequency = settings.baseRoughness;
        float amplitude = 1;

        for (int i = 0; i < settings.octaves; i++)
        {
            Vector3 vec = point * frequency + settings.center;
            float v = noise.GetNoise3Dv(vec);
            noiseValue += (v + 1) * .5f * amplitude;
            frequency *= settings.roughness;
            amplitude *= settings.persistence;
        }

        noiseValue = noiseValue - settings.minValue;
        return noiseValue * settings.strength;
    }
}