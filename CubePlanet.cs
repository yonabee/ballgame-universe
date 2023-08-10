using Godot;
using System;
using System.Collections.Generic;
using static Utils;

public partial class CubePlanet : Planetoid
{
    public int Resolution = 400;
    public List<Face> faceRenderMask;

    public ShapeGenerator.ShapeSettings shapeSettings;
    public ColorSettings colorSettings;

    public ShapeGenerator Shapes = new ShapeGenerator();
    public ColorGenerator Colors = new ColorGenerator();
    public TerrainFace[] TerrainFaces;

    StandardMaterial3D landRenderer;
    StandardMaterial3D oceanRenderer;

    Vector3[] directions = { Vector3.Up, Vector3.Down, Vector3.Left, Vector3.Right, Vector3.Forward, Vector3.Back };

    public override void _Ready() {
        Configure();
        GeneratePlanet();
    }

    public override void Configure() {
        Gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

        base.Configure();

        Faces = 6;
        Layers = 2;
        faceRenderMask = new List<Face>{ Face.All };
    }

    public override void Initialize()
    {
        base.Initialize();

        if (shapeSettings == null) {
            shapeSettings = new ShapeGenerator.ShapeSettings();
            shapeSettings.mass = Mass;
            shapeSettings.radius = Radius;

            var center = ToGlobal(Transform.Origin);

            var noiseLayer1 = new NoiseSettings();
            noiseLayer1.strength = 0.15f;
            noiseLayer1.octaves = 4;
            noiseLayer1.frequency = 0.5f;
            noiseLayer1.roughness = 2.35f;
            noiseLayer1.persistence = 0.5f;
            noiseLayer1.minValue = 1.1f;
            noiseLayer1.center = center;
            noiseLayer1.filterType = NoiseSettings.FilterType.Simple;
            noiseLayer1.seed = Seed;

            var noiseLayer2 = new NoiseSettings();
            noiseLayer2.strength = 4f;
            noiseLayer2.octaves = 5;
            noiseLayer2.frequency = 1f;
            noiseLayer2.roughness = 2f;
            noiseLayer2.persistence = 0.5f;
            noiseLayer2.minValue = 1.25f;
            noiseLayer2.center = center;
            noiseLayer2.filterType = NoiseSettings.FilterType.Simple;
            noiseLayer2.useFirstLayerAsMask = true;
            noiseLayer2.seed = Seed;

            var noiseLayer3 = new NoiseSettings();
            noiseLayer3.strength = 0.8f;
            noiseLayer3.octaves = 4;
            noiseLayer3.frequency = 2.5f;
            noiseLayer3.roughness = 2f;
            noiseLayer3.persistence = 0.5f;
            noiseLayer3.minValue = 0f;
            noiseLayer3.center = center;
            noiseLayer3.filterType = NoiseSettings.FilterType.Ridged;
            noiseLayer3.useFirstLayerAsMask = true;
            noiseLayer3.seed = Seed;

            NoiseSettings[] noiseSettings = new[] { noiseLayer1, noiseLayer2, noiseLayer3 };
            shapeSettings.noiseSettings = noiseSettings;
        }

        if (colorSettings == null) {
            colorSettings = new ColorSettings();
            colorSettings.oceanColor = new Color(Crayons[Random.RandiRange(0,35)]); 
            colorSettings.biomeColourSettings = new ColorSettings.BiomeColourSettings();

            var biome1 = new ColorSettings.BiomeColourSettings.Biome();
            biome1.tint = new Color(Crayons[Random.RandiRange(0,47)]);
            biome1.tintPercent = 0.3f;
            biome1.startHeight = 0f;
            biome1.gradient = new Gradient();
            biome1.gradient.Colors = _CreateBiomeGradient();
            biome1.gradient.Offsets = new[] { 0f, 0.3f, 0.45f, 0.5f, 0.55f, 0.7f, 1f };
            
            var biome2 = new ColorSettings.BiomeColourSettings.Biome();
            biome2.tint = new Color(Crayons[Random.RandiRange(0,47)]);
            biome2.tintPercent = 0.3f;
            biome2.startHeight = 0.333f;
            biome2.gradient = new Gradient();
            biome2.gradient.Colors = _CreateBiomeGradient();
            biome2.gradient.Offsets = new[] { 0f, 0.3f, 0.45f, 0.5f, 0.55f, 0.7f, 1f };

            var biome3 = new ColorSettings.BiomeColourSettings.Biome();
            biome3.tint =  new Color(Crayons[Random.RandiRange(0,47)]);
            biome3.tintPercent = 0.3f;
            biome3.startHeight = 0.666f;
            biome3.gradient = new Gradient();
            biome3.gradient.Colors = _CreateBiomeGradient();
            biome3.gradient.Offsets = new[] { 0f, 0.3f, 0.45f, 0.5f, 0.55f, 0.7f, 1f };

            colorSettings.biomeColourSettings.biomes = new[] { biome1, biome2, biome3 };

            var biomeNoise = new NoiseSettings();
            biomeNoise.filterType = NoiseSettings.FilterType.Simple;
            biomeNoise.strength = 1f;
            biomeNoise.octaves = 3;
            biomeNoise.frequency = 1;
            biomeNoise.roughness = 2;
            biomeNoise.persistence = 0.5f;
            biomeNoise.minValue = 0f;

            colorSettings.biomeColourSettings.biomeNoise = biomeNoise;
            colorSettings.biomeColourSettings.biomeNoiseOffset = 0.66f;
            colorSettings.biomeColourSettings.biomeNoiseStrength = 0.4f;
            colorSettings.biomeColourSettings.biomeBlendAmount = 0.5f;

            var heightMapNoise = new NoiseSettings();
            heightMapNoise.filterType = NoiseSettings.FilterType.Warped;
            heightMapNoise.strength = 1f;
            heightMapNoise.octaves = 5;
            heightMapNoise.frequency = Random.RandfRange(0.1f, 1f);
            heightMapNoise.roughness = Random.RandfRange(1f,3f);
            heightMapNoise.persistence = Random.RandfRange(0.3f, 0.7f);
            heightMapNoise.minValue = 0f;
            heightMapNoise.warpFrequency = Random.RandfRange(0.1f, 1f);

            colorSettings.biomeColourSettings.heightMapNoise = heightMapNoise;
            colorSettings.biomeColourSettings.heightMapNoiseStrength = Random.RandfRange(0.2f, 0.5f);
        }

        Shapes.UpdateSettings(shapeSettings);
        Colors.UpdateSettings(colorSettings);

        if (TerrainFaces == null || TerrainFaces.Length == 0) {
            TerrainFaces = new TerrainFace[Faces];
        }

        if (landRenderer == null) {
            landRenderer = new StandardMaterial3D();
            landRenderer.VertexColorUseAsAlbedo = true;
            landRenderer.TextureFilter = StandardMaterial3D.TextureFilterEnum.Nearest;
        }
        if (oceanRenderer == null) {
            oceanRenderer = new StandardMaterial3D();
            oceanRenderer.VertexColorUseAsAlbedo = true;
            oceanRenderer.Transparency = StandardMaterial3D.TransparencyEnum.Alpha;
            oceanRenderer.ClearcoatEnabled = true;
            oceanRenderer.ClearcoatRoughness = 1.0f;
        }

        for (int i = 0; i < Faces; i++) {
            TerrainFaces[i] = new TerrainFace(
                Colors, 
                Shapes, 
                shapeSettings, 
                Resolution, 
                directions[i],
                (Face)i + 1
            );
        }
        GD.Print("initialized");
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (TerrainFaces == null) {
            return;
        }

        if (Universe.PlayerCam.Current) {
            faceRenderMask.Clear();
            faceRenderMask.AddRange(GetFaces(Universe.CurrentFace));
        } else if (!faceRenderMask.Contains(Face.All)) {
            faceRenderMask.Clear();
            faceRenderMask.Add(Face.All);
        }

        for (int i = 0; i < Faces; i++) {
            if (faceRenderMask.Contains(Face.All) || faceRenderMask.Contains((Face)i + 1)) {
                TerrainFaces[i].Show();
            } else {
                TerrainFaces[i].Hide();
            }
        }
    }

    public void DetermineElevations()
    {
        for (int i = 0; i < Faces; i++) {
            TerrainFaces[i].Elevate();
        }
    }
    public override void GenerateMesh()
    {
        DetermineElevations();
        for (int i = 0; i < Faces; i++) {
            bool renderFace = faceRenderMask.Contains(Face.All) || faceRenderMask.Contains((Face)i + 1);
            if (renderFace) {
                TerrainFaces[i].ConstructMesh();
                for (int j = 0; j < TerrainFaces[i].LandMeshes.Length; j++) {
                    if (TerrainFaces[i].LandMeshes[j] != null) {
                        for (int k = 0; k < TerrainFaces[i].LandMeshes[j].Mesh.GetSurfaceCount(); k++) {
                            TerrainFaces[i].LandMeshes[j].Mesh.SurfaceSetMaterial(k, landRenderer);
                        }
                        //TerrainFaces[i].LandMeshes[j].CreateMultipleConvexCollisions();
                    }
                    if (TerrainFaces[i].OceanMeshes.Length > j && TerrainFaces[i].OceanMeshes[j] != null) {
                        TerrainFaces[i].OceanMeshes[j].Mesh.SurfaceSetMaterial(0, oceanRenderer);
                        TerrainFaces[i].OceanMeshes[j].CreateMultipleConvexCollisions();
                    }
                } 
            }
        }
        GD.Print("rendered");
    }

    Color[] _CreateBiomeGradient()
    {
        var gradient = new Color[7];
        Color grey = new Color(Crayons[Crayons.Length + Random.RandiRange(0, 11) - 12]);
        if (grey.V < 0.5f) {
            gradient[6] = grey;
            var darkIdx = Crayons.Length + Random.RandiRange(0, 11) - 24;
            gradient[5] = new Color(Crayons[darkIdx]);
            var darkOffsetIdx = 24 + ((darkIdx - 24 + _Offset()) % 12);
            gradient[4] = new Color(Crayons[darkOffsetIdx]);
            var mediumIdx =  12 + ((darkIdx - 24 + _Offset()) % 12);
            gradient[3] = new Color(Crayons[mediumIdx]);
            var mediumOffsetIdx = 12 + ((mediumIdx - 12 + _Offset()) % 12);
            gradient[2] = new Color(Crayons[mediumOffsetIdx]);
            var lightIdx = Mathf.Abs((mediumIdx - 12 + _Offset()) % 12);
            gradient[1] = new Color(Crayons[lightIdx]);
            var lightOffsetIdx = Mathf.Abs((lightIdx + _Offset()) % 12);
            gradient[0] = new Color(Crayons[lightOffsetIdx]);
        } else {
            gradient[0] = grey;
            var lightIdx = Random.RandiRange(0, 11);
            gradient[1] = new Color(Crayons[lightIdx]);
            var lightOffsetIdx = Mathf.Abs((lightIdx + _Offset()) % 12);
            gradient[2] = new Color(Crayons[lightOffsetIdx]);
            var medIdx = 12 + ((lightIdx + _Offset()) % 12);
            gradient[3] = new Color(Crayons[medIdx]);
            var mediumOffsetIdx = 12 + ((medIdx - 12 + _Offset()) % 12);
            gradient[4] = new Color(Crayons[mediumOffsetIdx]);
            var darkIdx = 24 + ((medIdx - 12 + _Offset()) % 12);
            gradient[5] = new Color(Crayons[darkIdx]);
            var darkOffsetIdx =  24 + ((darkIdx - 24 + _Offset()) % 12);
            gradient[6] = new Color(Crayons[darkOffsetIdx]);
        }
        if (Random.Randf() >= 0.5f) {
            Array.Reverse(gradient);
        }
        return gradient;
    }

    // Returns a number from -3 to 3 excluding 0;
    int _Offset()
    {
        var offset = Random.RandiRange(1, 6) - 3;
        if (offset <=0) {
            offset -=1;
        }
        return offset;
    }
}