using Godot;

[System.Serializable]
public class NoiseSettings
{
    public bool enabled = true;
    public bool useFirstLayerAsMask;

    public enum FilterType
    {
        Simple,
        Ridged,
        Warped
    };

    public FilterType filterType;
    public int radius = 1;
    public float strength = 1;
    public int octaves = 1;
    public float frequency = 1;
    public float roughness = 2;
    public float persistence = 0.15f;
    public float warpFrequency = 0.1f;
    public float warpRoughness = 0.5f;
    public float warpPersistence = 0.15f;
    public int warpOctaves = 3;
    public Vector3 center;
    public float minValue;
    public float weightMultiplier = 0.8f;
    public int seed;
}
