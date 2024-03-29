using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Utils;

public enum LOD {
    Planet,
    NearOrbit,
    Orbit,
    Space,
}

public partial class CubePlanet : Planetoid
{
    public int Resolution = 400;
    public List<Face> faceRenderMask;

    public ShapeGenerator.ShapeSettings shapeSettings;
    public ColorSettings colorSettings;

    public ShapeGenerator Shapes = new ShapeGenerator();
    public ColorGenerator Colors = new ColorGenerator();
    public TerrainFace[] TerrainFaces;
    public LOD LOD;

    public StandardMaterial3D LandRenderer;
    public StandardMaterial3D OceanRenderer;

    Vector3[] directions = { Vector3.Up, Vector3.Down, Vector3.Left, Vector3.Right, Vector3.Forward, Vector3.Back };

    Dictionary<LOD, Action> createLOD;

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
        LOD = LOD.NearOrbit;

    }

    public override void Initialize()
    {
        base.Initialize();

        if (shapeSettings == null) {
            shapeSettings = new ShapeGenerator.ShapeSettings
            {
                mass = Mass,
                radius = Radius
            };

            var center = ToGlobal(Transform.Origin);

            var noiseLayer1 = new NoiseSettings
            {
                strength = 0.15f,
                octaves = 4,
                frequency = 0.5f,
                roughness = 2.35f,
                persistence = 0.5f,
                minValue = 1.1f,
                center = center,
                filterType = NoiseSettings.FilterType.Simple,
                seed = Seed
            };

            var noiseLayer2 = new NoiseSettings
            {
                strength = 4f,
                octaves = 5,
                frequency = 1f,
                roughness = 2f,
                persistence = 0.5f,
                minValue = 1.25f,
                center = center,
                filterType = NoiseSettings.FilterType.Simple,
                useFirstLayerAsMask = true,
                seed = Seed
            };

            var noiseLayer3 = new NoiseSettings
            {
                strength = 0.8f,
                octaves = 4,
                frequency = 2.5f,
                roughness = 2f,
                persistence = 0.5f,
                minValue = 0f,
                center = center,
                filterType = NoiseSettings.FilterType.Ridged,
                useFirstLayerAsMask = true,
                seed = Seed
            };

            NoiseSettings[] noiseSettings = new[] { noiseLayer1, noiseLayer2, noiseLayer3 };
            shapeSettings.noiseSettings = noiseSettings;
        }

        if (colorSettings == null) {
            colorSettings = new ColorSettings
            {
                oceanColor = new Color(Crayons[Random.RandiRange(0, 35)]),
                biomeColourSettings = new ColorSettings.BiomeColourSettings()
            };

            var biome1 = new ColorSettings.BiomeColourSettings.Biome
            {
                tint = new Color(Crayons[Random.RandiRange(0, 47)]),
                tintPercent = 0.3f,
                startHeight = 0f,
                gradient = new Gradient
                {
                    Colors = _CreateBiomeGradient(),
                    Offsets = new[] { 0f, 0.3f, 0.45f, 0.5f, 0.55f, 0.7f, 1f }
                }
            };

            var biome2 = new ColorSettings.BiomeColourSettings.Biome
            {
                tint = new Color(Crayons[Random.RandiRange(0, 47)]),
                tintPercent = 0.3f,
                startHeight = 0.333f,
                gradient = new Gradient
                {
                    Colors = _CreateBiomeGradient(),
                    Offsets = new[] { 0f, 0.3f, 0.45f, 0.5f, 0.55f, 0.7f, 1f }
                }
            };

            var biome3 = new ColorSettings.BiomeColourSettings.Biome
            {
                tint = new Color(Crayons[Random.RandiRange(0, 47)]),
                tintPercent = 0.3f,
                startHeight = 0.666f,
                gradient = new Gradient
                {
                    Colors = _CreateBiomeGradient(),
                    Offsets = new[] { 0f, 0.3f, 0.45f, 0.5f, 0.55f, 0.7f, 1f }
                }
            };

            colorSettings.biomeColourSettings.biomes = new[] { biome1, biome2, biome3 };

            var biomeNoise = new NoiseSettings
            {
                filterType = NoiseSettings.FilterType.Simple,
                strength = 1f,
                octaves = 3,
                frequency = 1,
                roughness = 2,
                persistence = 0.5f,
                minValue = 0f
            };

            colorSettings.biomeColourSettings.biomeNoise = biomeNoise;
            colorSettings.biomeColourSettings.biomeNoiseOffset = 0.66f;
            colorSettings.biomeColourSettings.biomeNoiseStrength = 0.4f;
            colorSettings.biomeColourSettings.biomeBlendAmount = 0.5f;

            var heightMapNoise = new NoiseSettings
            {
                filterType = NoiseSettings.FilterType.Warped,
                strength = 1f,
                octaves = 5,
                frequency = Random.RandfRange(0.1f, 1f),
                roughness = Random.RandfRange(1f, 3f),
                persistence = Random.RandfRange(0.3f, 0.7f),
                minValue = 0f,
                warpFrequency = Random.RandfRange(0.1f, 1f)
            };

            colorSettings.biomeColourSettings.heightMapNoise = heightMapNoise;
            colorSettings.biomeColourSettings.heightMapNoiseStrength = Random.RandfRange(0.2f, 0.5f);
        }

        Shapes.UpdateSettings(shapeSettings);
        Colors.UpdateSettings(colorSettings);

        if (TerrainFaces == null || TerrainFaces.Length == 0) {
            TerrainFaces = new TerrainFace[Faces];
        }

        if (LandRenderer == null) {
            LandRenderer = new StandardMaterial3D
            {
                VertexColorUseAsAlbedo = true,
                TextureFilter = StandardMaterial3D.TextureFilterEnum.Nearest
            };
        }
        if (OceanRenderer == null) {
            OceanRenderer = new StandardMaterial3D
            {
                VertexColorUseAsAlbedo = true,
                Transparency = StandardMaterial3D.TransparencyEnum.Alpha,
                ClearcoatEnabled = true,
                ClearcoatRoughness = 1.0f
            };
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
            if (LOD != LOD.Planet) {
                faceRenderMask.Clear();
                faceRenderMask.AddRange(GetFaces(Universe.CurrentFace));
                LOD = LOD.Planet;
            }
            Universe.InfoText.Text = string.Format("{0} {1} {2}x{3}", LOD, Universe.CurrentFace, Universe.Location.X, Universe.Location.Y);
        } else if (Universe.WatcherCam.Current) {
            if (LOD == LOD.Planet) {
                faceRenderMask.Clear();
                faceRenderMask.Add(Face.All);
            }
            var cameraZ = Universe.WatcherCam.Transform.Origin.Z - Universe.Planet.Radius;
            if (cameraZ >= 5000f) {
                LOD = LOD.Space;
            } else if (cameraZ < 5000f && cameraZ > 1500f) {
                LOD = LOD.Orbit;
            } else {
                LOD = LOD.NearOrbit;
            }
            Universe.InfoText.Text = String.Format("{0}", LOD);
        }

        //GD.Print(LOD);

        for (int i = 0; i < Faces; i++) {
            if (faceRenderMask.Contains(Face.All) || faceRenderMask.Contains((Face)i + 1)) {
                TerrainFaces[i].Show();
            } else {
                TerrainFaces[i].Hide();
            }
        }
    }

    public override async void GenerateMesh()
    {
        var generateLOD = async (LOD lod) => {
            for (int i = 0; i < Faces; i++) {
                bool renderFace = faceRenderMask.Contains(Face.All) || faceRenderMask.Contains((Face)i + 1);
                if (!renderFace) {
                    continue;
                }
                var face = TerrainFaces[i];
                await Task.Run(() => face.Elevate(lod));
            }

            for (int i = 0; i < Faces; i++) {
                bool renderFace = faceRenderMask.Contains(Face.All) || faceRenderMask.Contains((Face)i + 1);
                if (!renderFace) {
                    continue;
                }
                var face = TerrainFaces[i];
                await face.ConstructMesh(lod);
                face.Show();
                GD.Print("Generated mesh for face " + i);
            }
        };

        await generateLOD(LOD.Space);
        await generateLOD(LOD.Orbit);
        await generateLOD(LOD.NearOrbit);
    }

    public void OnChunkMeshCompleted(MeshInstance3D mesh, bool makeColliders = false) 
    {
        mesh.Mesh.CallDeferred(Mesh.MethodName.SurfaceSetMaterial, 0, LandRenderer);
        if (makeColliders) {
            Task.Run(() => mesh.CallDeferred(MeshInstance3D.MethodName.CreateMultipleConvexCollisions));
        }
        GD.Print("completed chunk mesh " + (makeColliders ? "and built colliders" : ""));
    }

    Color[] _CreateBiomeGradient()
    {
        var gradient = new Color[7];
        Color grey = new Color(Crayons[Crayons.Length + Random.RandiRange(0, 11) - 12]);
        if (grey.V < 0.5f) {
            gradient[6] = grey;
            var darkIdx = Crayons.Length + Random.RandiRange(0, 11) - 24;
            gradient[5] = new Color(Crayons[darkIdx]);
            var darkOffsetIdx = 24 + ((darkIdx - 24 + Offset(3)) % 12);
            gradient[4] = new Color(Crayons[darkOffsetIdx]);
            var mediumIdx =  12 + ((darkIdx - 24 + Offset(3)) % 12);
            gradient[3] = new Color(Crayons[mediumIdx]);
            var mediumOffsetIdx = 12 + ((mediumIdx - 12 + Offset(3)) % 12);
            gradient[2] = new Color(Crayons[mediumOffsetIdx]);
            var lightIdx = Mathf.Abs((mediumIdx - 12 + Offset(3)) % 12);
            gradient[1] = new Color(Crayons[lightIdx]);
            var lightOffsetIdx = Mathf.Abs((lightIdx + Offset(3)) % 12);
            gradient[0] = new Color(Crayons[lightOffsetIdx]);
        } else {
            gradient[0] = grey;
            var lightIdx = Random.RandiRange(0, 11);
            gradient[1] = new Color(Crayons[lightIdx]);
            var lightOffsetIdx = Mathf.Abs((lightIdx + Offset(3)) % 12);
            gradient[2] = new Color(Crayons[lightOffsetIdx]);
            var medIdx = 12 + ((lightIdx + Offset(3)) % 12);
            gradient[3] = new Color(Crayons[medIdx]);
            var mediumOffsetIdx = 12 + ((medIdx - 12 + Offset(3)) % 12);
            gradient[4] = new Color(Crayons[mediumOffsetIdx]);
            var darkIdx = 24 + ((medIdx - 12 + Offset(3)) % 12);
            gradient[5] = new Color(Crayons[darkIdx]);
            var darkOffsetIdx =  24 + ((darkIdx - 24 + Offset(3)) % 12);
            gradient[6] = new Color(Crayons[darkOffsetIdx]);
        }
        if (Random.Randf() >= 0.5f) {
            Array.Reverse(gradient);
        }
        return gradient;
    }

}