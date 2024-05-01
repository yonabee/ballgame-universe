using System.Collections;
using System.Collections.Generic;
using Godot;

public class ColorSettings
{
    public StandardMaterial3D planetMaterial;
    public StandardMaterial3D oceanMaterial;
    public BiomeColourSettings biomeColourSettings;
    public Color oceanColor;
    public MinMax elevation;

    [System.Serializable]
    public class BiomeColourSettings
    {
        public Biome[] biomes;
        public NoiseSettings biomeNoise;
        public float biomeNoiseOffset;
        public float biomeNoiseStrength;
        public float biomeBlendAmount;

        public NoiseSettings heightMapNoise;
        public float heightMapNoiseStrength;

        [System.Serializable]
        public class Biome
        {
            public Gradient gradient;
            public Color tint;
            public float startHeight;
            public float tintPercent;
        }
    }
}
