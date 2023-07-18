using Godot;
using System.Collections.Generic;

public partial class CubePlanet : Planetoid
{
    public int resolution = 400;
    public enum FaceRenderMask { All, Top, Bottom, Left, Right, Front, Back }
    public FaceRenderMask faceRenderMask;

    public ShapeGenerator.ShapeSettings shapeSettings;
    public ColorSettings colorSettings;

    public ShapeGenerator Shapes = new ShapeGenerator();
    public ColorGenerator Colors = new ColorGenerator();

    StandardMaterial3D landRenderer;
    StandardMaterial3D oceanRenderer;

    ArrayMesh[] landMeshes;
    ArrayMesh[] oceanMeshes;

    TerrainFace[] terrainFaces;
    Vector3[] directions = { Vector3.Up, Vector3.Down, Vector3.Left, Vector3.Right, Vector3.Forward, Vector3.Back };

    public override void _Ready() {
        Faces = 6;
        Layers = 2;
        faceRenderMask = FaceRenderMask.All;
        Gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
        CustomIntegrator = true;
        CurrentVelocity = initialVelocity;
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
            biome1.tint = new Color("#6ff");
            biome1.startHeight = 0f;
            biome1.gradient = new Gradient();
            biome1.gradient.Colors = new[] {
                new Color("#6cf"),
                new Color("#cf6"),
                new Color("#118040"),
                new Color("#fd8008"),
                new Color("#804003")
            };
            
            var biome2 = new ColorSettings.BiomeColourSettings.Biome();
            biome2.tint = new Color("#118002");
            biome2.startHeight = 0.333f;
            biome2.gradient = new Gradient();
            biome2.gradient.Colors = new[] {
                new Color("#6cf"),
                new Color("#fc66ff"),
                new Color("#8000ff"),
                new Color("#7f7f7f"),
                new Color("#804003")
            };

            var biome3 = new ColorSettings.BiomeColourSettings.Biome();
            biome3.tint = new Color("#c6f");
            biome3.startHeight = 0.666f;
            biome3.gradient = new Gradient();
            biome3.gradient.Colors = new[] {
                new Color("#6cf"),
                new Color("#ccc"),
                new Color("#ffff0a"),
                new Color("#808004"),
                new Color("#108080")
            };

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
        if (terrainFaces == null || terrainFaces.Length == 0) {
            terrainFaces = new TerrainFace[Faces];
        }

        if (landRenderer == null) {
            landRenderer = new StandardMaterial3D();
            landRenderer.VertexColorUseAsAlbedo = true;
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
            terrainFaces[i] = new TerrainFace(
                Colors, 
                Shapes, 
                shapeSettings, 
                landMeshes[i], 
                oceanMeshes[i], 
                resolution, 
                directions[i]
            );
        }
        GD.Print("initialized");
    }

    public void DetermineElevations()
    {
        for (int i = 0; i < Faces; i++) {
            terrainFaces[i].Elevate();
        }
    }
    public override void GenerateMesh()
    {
        DetermineElevations();
        for (int i = 0; i < Faces; i++) {
            bool renderFace = faceRenderMask == FaceRenderMask.All || (int)faceRenderMask - 1 == i;
            if (renderFace) {
                terrainFaces[i].ConstructMesh();
                meshes[i].Mesh = terrainFaces[i].landMesh;
                meshes[Faces + i].Mesh = terrainFaces[i].oceanMesh;
                meshes[i].Mesh.SurfaceSetMaterial(0, landRenderer);
                meshes[Faces + i].Mesh.SurfaceSetMaterial(0, oceanRenderer);
                meshes[i].CreateMultipleConvexCollisions();
            }
        }
        GD.Print("rendered");
    }
}