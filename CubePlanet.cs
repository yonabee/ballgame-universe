using Godot;
using System.Collections.Generic;
using static Utils;

public partial class CubePlanet : Planetoid
{
    public int Resolution = 400;
    public Utils.Face faceRenderMask;

    public ShapeGenerator.ShapeSettings shapeSettings;
    public ColorSettings colorSettings;

    public ShapeGenerator Shapes = new ShapeGenerator();
    public ColorGenerator Colors = new ColorGenerator();
    public TerrainFace[] TerrainFaces;

    StandardMaterial3D landRenderer;
    StandardMaterial3D oceanRenderer;

    ArrayMesh[] landMeshes;
    ArrayMesh[] oceanMeshes;

    Vector3[] directions = { Vector3.Up, Vector3.Down, Vector3.Left, Vector3.Right, Vector3.Forward, Vector3.Back };

    public override void _Ready() {
        Faces = 6;
        Layers = 2;
        faceRenderMask = Face.All;
        Gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
        CustomIntegrator = true;
        CurrentVelocity = initialVelocity;
        Random = new RandomNumberGenerator();
        Random.Seed = (ulong)Seed;
        GeneratePlanet();
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
            noiseLayer1.baseRoughness = 0.5f;
            noiseLayer1.roughness = 2.35f;
            noiseLayer1.persistence = 0.5f;
            noiseLayer1.minValue = 1.1f;
            noiseLayer1.center = center;
            noiseLayer1.filterType = NoiseSettings.FilterType.Simple;
            noiseLayer1.seed = Seed;

            var noiseLayer2 = new NoiseSettings();
            noiseLayer2.strength = 4f;
            noiseLayer2.octaves = 5;
            noiseLayer2.baseRoughness = 1f;
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
            noiseLayer3.baseRoughness = 2.5f;
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
            colorSettings.oceanColor = new Gradient();
            colorSettings.oceanColor.Colors = new[] { 
                new Color("#0f80ff"),
                new Color("#000080") 
            };
            colorSettings.biomeColourSettings = new ColorSettings.BiomeColourSettings();

            var biome1 = new ColorSettings.BiomeColourSettings.Biome();
            //biome1.tint = new Color("#6ff");
            biome1.tint = new Color(Crayons[Random.RandiRange(0,47)]);
            biome1.tintPercent = 0.3f;
            biome1.startHeight = 0f;
            biome1.gradient = new Gradient();
            // biome1.gradient.Colors = new[] {
            //     new Color("#6cf"),
            //     new Color("#cf6"),
            //     new Color("#118040"),
            //     new Color("#fd8008"),
            //     new Color("#804003")
            // };
            biome1.gradient.Colors = _CreateBiomeGradient();
            biome1.gradient.Offsets = new[] { 0f, 0.3f, 0.4f, 0.5f, 0.6f, 0.7f, 1f };
            
            var biome2 = new ColorSettings.BiomeColourSettings.Biome();
            biome2.tint = new Color(Crayons[Random.RandiRange(0,47)]);
            biome2.tintPercent = 0.3f;
            biome2.startHeight = 0.333f;
            biome2.gradient = new Gradient();
            // biome2.gradient.Colors = new[] {
            //     new Color("#6cf"),
            //     new Color("#fc66ff"),
            //     new Color("#8000ff"),
            //     new Color("#7f7f7f"),
            //     new Color("#804003")
            // };
            biome2.gradient.Colors = _CreateBiomeGradient();
            biome2.gradient.Offsets = new[] { 0f, 0.3f, 0.4f, 0.5f, 0.6f, 0.7f, 1f };

            var biome3 = new ColorSettings.BiomeColourSettings.Biome();
            biome3.tint =  new Color(Crayons[Random.RandiRange(0,47)]);
            biome3.tintPercent = 0.3f;
            biome3.startHeight = 0.666f;
            biome3.gradient = new Gradient();
            // biome3.gradient.Colors = new[] {
            //     new Color("#6cf"),
            //     new Color("#ccc"),
            //     new Color("#ffff0a"),
            //     new Color("#808004"),
            //     new Color("#108080")
            // };
            biome3.gradient.Colors = _CreateBiomeGradient();
            biome3.gradient.Offsets = new[] { 0f, 0.3f, 0.4f, 0.5f, 0.6f, 0.7f, 1f };

            colorSettings.biomeColourSettings.biomes = new[] { biome1, biome2, biome3 };

            var biomeNoise = new NoiseSettings();
            biomeNoise.filterType = NoiseSettings.FilterType.Simple;
            biomeNoise.strength = 1f;
            biomeNoise.octaves = 3;
            biomeNoise.baseRoughness = 1;
            biomeNoise.roughness = 2;
            biomeNoise.persistence = 0.5f;
            biomeNoise.minValue = 0f;

            colorSettings.biomeColourSettings.noise = biomeNoise;
            colorSettings.biomeColourSettings.noiseOffset = 0.66f;
            colorSettings.biomeColourSettings.noiseStrength = 0.4f;
            colorSettings.biomeColourSettings.blendAmount = 0.5f;
        }

        Shapes.UpdateSettings(shapeSettings);
        Colors.UpdateSettings(colorSettings);

        if (landMeshes == null || landMeshes.Length == 0) {
            landMeshes = new ArrayMesh[Faces];
        }
        if (oceanMeshes == null || oceanMeshes.Length == 0) {
            oceanMeshes = new ArrayMesh[Faces];
        }
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
        }

        for (int i = 0; i < Faces; i++) {
            landMeshes[i] = new ArrayMesh();
            oceanMeshes[i] = new ArrayMesh();
            TerrainFaces[i] = new TerrainFace(
                Colors, 
                Shapes, 
                shapeSettings, 
                landMeshes[i], 
                oceanMeshes[i], 
                Resolution, 
                directions[i]
            );
        }
        GD.Print("initialized");
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
            bool renderFace = faceRenderMask == Face.All || (int)faceRenderMask - 1 == i;
            if (renderFace) {
                TerrainFaces[i].ConstructMesh();
                Meshes[i].Mesh = TerrainFaces[i].landMesh;
                Meshes[Faces + i].Mesh = TerrainFaces[i].oceanMesh;
                Meshes[i].Mesh.SurfaceSetMaterial(0, landRenderer);
                Meshes[Faces + i].Mesh.SurfaceSetMaterial(0, oceanRenderer);
                Meshes[i].CreateMultipleConvexCollisions();
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