
using Godot;

[System.Serializable]
public class NoiseSettings {
    public bool enabled = true;
    public bool useFirstLayerAsMask;
    public enum FilterType { Simple, Ridged };
    public FilterType filterType;
    public int radius = 1;
    public float strength = 1;
    public int octaves = 1;
    public float baseRoughness = 1;
    public float roughness = 2;
    public float persistence = 0.15f;
    public Vector3 center;
    public float minValue;
    public float weightMultiplier = 0.8f;
    public int seed;
}