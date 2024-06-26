using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using static Universe;
using static Utils;

public enum LOD
{
    Planet,
    NearOrbit,
    Orbit,
    Space,
    Collision,
}

public partial class CubePlanet : Node3D, CelestialObject
{
    public float Radius { get; set; }
    public int Faces { get; set; }
    public int Layers { get; set; }
    public float Gravity { get; set; }
    public float Mass { get; set; }
    public Vector3 CurrentRotation { get; set; }
    public Vector3 CurrentVelocity { get; set; }

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
        Initialize();
        GenerateMesh();
    }

    public void Configure()
    {
        Gravity = 9.8f; //ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
        Mass = Gravity * Radius * Radius / Universe.Gravity * 10000;
        Faces = 6;
        Layers = 2;
        faceRenderMask = new List<Face> { Face.All };
        LOD = LOD.NearOrbit;
    }

    public void Initialize()
    {
        if (shapeSettings == null)
        {
            shapeSettings = new ShapeGenerator.ShapeSettings { radius = Radius };

            var center = ToGlobal(Transform.Origin);
            var warpChance = Universe.Random.Randf();

            var noiseLayer1 = new NoiseSettings
            {
                strength = Universe.Random.Randfn(0.15f, 0.01f), //Range(0.1f, 0.2f), //0.15f,
                octaves = Universe.Random.RandiRange(2, 6), //4,
                frequency = Universe.Random.Randfn(0.5f, 0.01f), // Range(0.4f, 0.6f), //0.5f,
                roughness = Universe.Random.Randfn(2.35f, 0.5f), //2.35f,
                persistence = Universe.Random.Randfn(0.5f, 0.25f), //0.5f,
                warpOctaves = Universe.Random.RandiRange(1, 2),
                warpFrequency = Universe.Random.RandfRange(0.1f, 0.2f),
                warpRoughness = Universe.Random.RandfRange(0.1f, 2f),
                warpPersistence = Universe.Random.RandfRange(0.25f, 1f),
                minValue = 1.1f,
                center = center,
                filterType =
                    warpChance < 0.666f
                        ? NoiseSettings.FilterType.Warped
                        : NoiseSettings.FilterType.Simple,
                seed = (int)Universe.Random.Randi()
            };

            GUI.Noise1.Text =
                noiseLayer1.strength.ToString("f2")
                + ", "
                + noiseLayer1.octaves.ToString()
                + ", "
                + noiseLayer1.frequency.ToString("f2")
                + ", "
                + noiseLayer1.roughness.ToString("f2")
                + ", "
                + noiseLayer1.persistence.ToString("f2")
                + " :hills";

            if (noiseLayer1.filterType == NoiseSettings.FilterType.Warped)
            {
                GUI.Noise1.Text +=
                    " "
                    + noiseLayer1.warpOctaves.ToString()
                    + ", "
                    + noiseLayer1.warpFrequency.ToString("f2")
                    + ", "
                    + noiseLayer1.warpRoughness.ToString("f2")
                    + ", "
                    + noiseLayer1.warpPersistence.ToString("f2")
                    + " :hills-x";
            }

            var noiseLayer2 = new NoiseSettings
            {
                strength = Universe.Random.Randfn(4f, 0.1f), // 4f,
                octaves = Universe.Random.RandiRange(2, 6), // 5,
                frequency = Universe.Random.Randfn(1f, 0.1f), //1f,
                roughness = Universe.Random.Randfn(2f, 0.2f), //2f,
                persistence = Universe.Random.Randfn(0.5f, 0.15f), // 0.5f,
                minValue = 1.25f,
                center = center,
                filterType = NoiseSettings.FilterType.Simple,
                useFirstLayerAsMask = true,
                seed = (int)Universe.Random.Randi()
            };

            GUI.Noise2.Text =
                noiseLayer2.strength.ToString("f2")
                + ", "
                + noiseLayer2.octaves.ToString()
                + ", "
                + noiseLayer2.frequency.ToString("f2")
                + ", "
                + noiseLayer2.roughness.ToString("f2")
                + ", "
                + noiseLayer2.persistence.ToString("f2")
                + " :mount";

            var noiseLayer3 = new NoiseSettings
            {
                strength = Universe.Random.Randfn(0.8f, 0.05f), //0.8f,
                octaves = Universe.Random.RandiRange(1, 4), //4,
                frequency = Universe.Random.RandfRange(0.5f, 2.75f), //2.5f,
                roughness = Universe.Random.Randfn(2f, 0.1f), //2f,
                persistence = Universe.Random.Randfn(0.5f, 0.1f), //0.5f,
                minValue = 0f,
                center = center,
                filterType = NoiseSettings.FilterType.Simple,
                useFirstLayerAsMask = true,
                seed = (int)Universe.Random.Randi()
            };

            GUI.Noise3.Text =
                noiseLayer3.strength.ToString("f2")
                + ", "
                + noiseLayer3.octaves.ToString()
                + ", "
                + noiseLayer3.frequency.ToString("f2")
                + ", "
                + noiseLayer3.roughness.ToString("f2")
                + ", "
                + noiseLayer3.persistence.ToString("f2")
                + " :ridge";

            NoiseSettings[] noiseSettings = new[] { noiseLayer1, noiseLayer2, noiseLayer3 };
            shapeSettings.noiseSettings = noiseSettings;
        }

        if (colorSettings == null)
        {
            colorSettings = new ColorSettings
            {
                oceanColor = new Color(Crayons[Universe.Random.RandiRange(0, 35)]),
                biomeColourSettings = new ColorSettings.BiomeColourSettings()
            };

            var biome1 = new ColorSettings.BiomeColourSettings.Biome
            {
                tint = new Color(Crayons[Universe.Random.RandiRange(0, 47)]),
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
                tint = new Color(Crayons[Universe.Random.RandiRange(0, 47)]),
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
                tint = new Color(Crayons[Universe.Random.RandiRange(0, 47)]),
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
                frequency = Universe.Random.RandfRange(0.1f, 1f),
                roughness = Universe.Random.RandfRange(1f, 3f),
                persistence = Universe.Random.RandfRange(0.3f, 0.7f),
                minValue = 0f,
                warpFrequency = Universe.Random.RandfRange(0.1f, 1f)
            };

            colorSettings.biomeColourSettings.heightMapNoise = heightMapNoise;
            colorSettings.biomeColourSettings.heightMapNoiseStrength = Universe.Random.RandfRange(
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
            GUI.Location.Text = string.Format(
                "{0}, Sector {1} at {2}x{3}",
                LOD,
                Universe.CurrentFace,
                Universe.CurrentLocation.X,
                Universe.CurrentLocation.Y
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
            GUI.Location.Text = String.Format(
                "{0} {1}",
                LOD,
                Universe.WatcherCam.Position.DistanceTo(Position).ToString()
            );
        }

        GUI.Status.Text = String.Format("{0} heavenly bodies out of bounds", Universe.OutOfBounds);

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

    public void UpdatePosition(float timeStep)
    {
        RotateObjectLocal(CurrentRotation.Normalized(), timeStep * RotationSpeed);
    }

    public async void GenerateMesh()
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
                GUI.Progress.Value += Universe.ConstructPlanetColliders ? 4.1 : 6.1;
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
        GUI.Progress.Visible = false;
        Universe.Bodies.ForEach(body => (body as Node3D).Visible = true);
        Universe.Stars.Visible = true;
        Universe.Initialized = true;
    }

    Color[] _CreateBiomeGradient()
    {
        var gradient = new Color[7];
        Color grey = new Color(Crayons[Crayons.Length + Universe.Random.RandiRange(0, 11) - 12]);
        if (grey.V < 0.5f)
        {
            gradient[6] = grey;
            var darkIdx = Crayons.Length + Universe.Random.RandiRange(0, 11) - 24;
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
            var lightIdx = Universe.Random.RandiRange(0, 11);
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
        if (Universe.Random.Randf() >= 0.5f)
        {
            Array.Reverse(gradient);
        }
        return gradient;
    }
}
