using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using static Utils;

public enum LOD
{
    Planet,
    NearOrbit,
    Orbit,
    Space,
    Collision,
}

public partial class CubePlanet : Planetoid
{
    public int Resolution = 400;
    public float RotationSpeed = 0.3f;
    public List<Face> faceRenderMask;

    public ShapeGenerator.ShapeSettings shapeSettings;
    public ColorSettings colorSettings;

    public ShapeGenerator Shapes = new ShapeGenerator();
    public ColorGenerator Colors = new ColorGenerator();
    public TerrainFace[] TerrainFaces;
    public LOD LOD;

    public StandardMaterial3D LandRenderer;
    public StandardMaterial3D OceanRenderer;

    Vector3[] directions =
    {
        Vector3.Up,
        Vector3.Down,
        Vector3.Left,
        Vector3.Right,
        Vector3.Forward,
        Vector3.Back
    };

    public override void _Ready()
    {
        Configure();
        GeneratePlanet();
    }

    public override void Configure()
    {
        Gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

        base.Configure();

        Faces = 6;
        Layers = 2;
        faceRenderMask = new List<Face> { Face.All };
        LOD = LOD.NearOrbit;
        CollisionLayer = 2;
        SetCollisionMaskValue(1, false);
    }

    public override void Initialize()
    {
        base.Initialize();

        if (shapeSettings == null)
        {
            shapeSettings = new ShapeGenerator.ShapeSettings { mass = Mass, radius = Radius };

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

        if (colorSettings == null)
        {
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
            colorSettings.biomeColourSettings.heightMapNoiseStrength = Random.RandfRange(
                0.2f,
                0.5f
            );
        }

        Shapes.UpdateSettings(shapeSettings);
        Colors.UpdateSettings(colorSettings);

        if (TerrainFaces == null || TerrainFaces.Length == 0)
        {
            TerrainFaces = new TerrainFace[Faces];
        }

        if (LandRenderer == null)
        {
            LandRenderer = new StandardMaterial3D
            {
                VertexColorUseAsAlbedo = true,
                TextureFilter = StandardMaterial3D.TextureFilterEnum.Nearest
            };
        }
        if (OceanRenderer == null)
        {
            OceanRenderer = new StandardMaterial3D
            {
                VertexColorUseAsAlbedo = true,
                Transparency = StandardMaterial3D.TransparencyEnum.Alpha,
                SpecularMode = BaseMaterial3D.SpecularModeEnum.Toon,
            };
        }

        for (int i = 0; i < Faces; i++)
        {
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

        if (TerrainFaces == null)
        {
            return;
        }

        if (Universe.PlayerCam.Current)
        {
            var cameraY = Universe.CameraArm.Transform.Origin.Y - Universe.Planet.Radius;
            if (cameraY >= 5000f)
            {
                LOD = LOD.Space;
            }
            else if (cameraY < 5000f && cameraY > 1500f)
            {
                LOD = LOD.Orbit;
            }
            else if (cameraY <= 1500f && cameraY > 500f)
            {
                LOD = LOD.NearOrbit;
            }
            else if (LOD != LOD.Planet)
            {
                faceRenderMask.Clear();
                faceRenderMask.AddRange(GetFaces(Universe.CurrentFace));
                LOD = LOD.Planet;
            }
            Universe.InfoText2.Text = string.Format(
                "{0} {1} {2}x{3}",
                LOD,
                Universe.CurrentFace,
                Universe.Location.X,
                Universe.Location.Y
            );
        }
        else if (Universe.WatcherCam.Current)
        {
            if (LOD == LOD.Planet)
            {
                faceRenderMask.Clear();
                faceRenderMask.Add(Face.All);
            }
            var cameraZ = Universe.WatcherCam.Transform.Origin.Z - Universe.Planet.Radius;
            if (cameraZ >= 5000f)
            {
                LOD = LOD.Space;
            }
            else if (cameraZ < 5000f && cameraZ > 1500f)
            {
                LOD = LOD.Orbit;
            }
            else
            {
                LOD = LOD.NearOrbit;
            }
            Universe.InfoText2.Text = String.Format(
                "{0} {1}",
                LOD,
                Universe.WatcherCam.Position.DistanceTo(Position).ToString()
            );
        }

        // Universe.InfoText2.Text = String.Format("{0} lost", Universe.OutOfBounds);

        //GD.Print(LOD);

        for (int i = 0; i < Faces; i++)
        {
            if (faceRenderMask.Contains(Face.All) || faceRenderMask.Contains((Face)i + 1))
            {
                TerrainFaces[i].Show();
            }
            else
            {
                TerrainFaces[i].Hide();
            }
        }
    }

    public new void UpdatePosition(float timeStep)
    {
        RotateObjectLocal(BaseRotation.Normalized(), timeStep * RotationSpeed);
    }

    public override async void GenerateMesh()
    {
        var generateLOD = async (LOD lod) =>
        {
            for (int i = 0; i < Faces; i++)
            {
                bool renderFace =
                    faceRenderMask.Contains(Face.All) || faceRenderMask.Contains((Face)i + 1);
                if (!renderFace)
                {
                    continue;
                }
                var face = TerrainFaces[i];
                await Task.Run(() => face.Elevate(lod));
            }

            // Track seam values for successive faces so they can be stitched together.
            // Make this here and pass it in so we can throw it away when we're done.
            var seams = new Dictionary<Vector3, Vector3>();

            for (int i = 0; i < Faces; i++)
            {
                bool renderFace =
                    faceRenderMask.Contains(Face.All) || faceRenderMask.Contains((Face)i + 1);
                if (!renderFace)
                {
                    continue;
                }
                var face = TerrainFaces[i];
                await face.ConstructMesh(lod, seams);
                GD.Print("Generated mesh for face " + face.Face);
                Universe.Progress.Value += Universe.ConstructPlanetColliders ? 4.1 : 6.1;
            }
            GD.Print("Generated meshes for " + lod);
        };
        GetTree().Paused = true;

        await generateLOD(LOD.Collision);
        await generateLOD(LOD.Space);
        await generateLOD(LOD.Orbit);
        await generateLOD(LOD.NearOrbit);

        if (Universe.ConstructPlanetColliders)
        {
            foreach (var face in TerrainFaces)
            {
                await face.MakeColliders();
                GD.Print("Generating colliders for face " + face.Face);
            }
            var planetBody = new AnimatableBody3D() { CollisionLayer = 2, };
            planetBody.SetCollisionMaskValue(1, false);
            var planetCollider = new CollisionShape3D
            {
                Shape = new SphereShape3D() { Radius = Radius }
            };
            planetBody.AddChild(planetCollider);
            // Primarily so it doesn't rotate
            GetNode("/root/Universe").AddChild(planetBody);
        }
        GetTree().Paused = false;
        Universe.Progress.Visible = false;
        Universe.Bodies.ForEach(body => (body as Node3D).Visible = true);
        Universe.Stars.Visible = true;
        Universe.Initialized = true;
    }

    Color[] _CreateBiomeGradient()
    {
        var gradient = new Color[7];
        Color grey = new Color(Crayons[Crayons.Length + Random.RandiRange(0, 11) - 12]);
        if (grey.V < 0.5f)
        {
            gradient[6] = grey;
            var darkIdx = Crayons.Length + Random.RandiRange(0, 11) - 24;
            gradient[5] = new Color(Crayons[darkIdx]);
            var darkOffsetIdx = 24 + ((darkIdx - 24 + Offset(3)) % 12);
            gradient[4] = new Color(Crayons[darkOffsetIdx]);
            var mediumIdx = 12 + ((darkIdx - 24 + Offset(3)) % 12);
            gradient[3] = new Color(Crayons[mediumIdx]);
            var mediumOffsetIdx = 12 + ((mediumIdx - 12 + Offset(3)) % 12);
            gradient[2] = new Color(Crayons[mediumOffsetIdx]);
            var lightIdx = Mathf.Abs((mediumIdx - 12 + Offset(3)) % 12);
            gradient[1] = new Color(Crayons[lightIdx]);
            var lightOffsetIdx = Mathf.Abs((lightIdx + Offset(3)) % 12);
            gradient[0] = new Color(Crayons[lightOffsetIdx]);
        }
        else
        {
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
            var darkOffsetIdx = 24 + ((darkIdx - 24 + Offset(3)) % 12);
            gradient[6] = new Color(Crayons[darkOffsetIdx]);
        }
        if (Random.Randf() >= 0.5f)
        {
            Array.Reverse(gradient);
        }
        return gradient;
    }
}
