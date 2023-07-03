using System.Collections;
using System.Collections.Generic;
using Godot;

public class ColorSettings 
{
    public StandardMaterial3D planetMaterial;
    public StandardMaterial3D oceanMaterial;
    public BiomeColourSettings biomeColourSettings;
    public Gradient oceanColor;
    public MinMax elevation;

    [System.Serializable]
    public class BiomeColourSettings
    {
        public Biome[] biomes;
        public NoiseSettings noise;
        public float noiseOffset;
        public float noiseStrength;
        public float blendAmount;

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