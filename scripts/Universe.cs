using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Godot;
using static Utils;

public partial class Universe : Node3D
{
    public static List<HeavenlyBody> Bodies = new List<HeavenlyBody>();
    public static CubePlanet Planet;
    public static Pivot PlayerPivot;
    public static Node3D CameraArm;
    public static Camera3D PlayerCam;
    public static Camera3D WatcherCam;
    public static float Gravity;
    public static int Radius = 7500;
    public static RandomNumberGenerator Random;
    public static Face CurrentFace;
    public static Vector2 Location;
    public static DirectionalLight3D Sunlight;
    public static Godot.Environment Environment;
    public static ShaderMaterial Sky;
    public static MultiMeshInstance3D Stars;
    public static Node3D Atmosphere;
    public static GDScript AtmosphereScript;
    public static GUI GuiManager;
    public static readonly bool ConstructPlanetColliders = true;
    public static int OutOfBounds = 0;
    public static bool Initialized = false;
    public static string Seed = "tatooine";
    public static readonly float CameraFloatHeight = 100f;

    float _previousDot = 0f;
    float _sunSpeed = 256f;
    readonly int _numGG = 5;
    readonly int _numMoons = 25;
    readonly int _numMoonlets = 150;
    readonly int _numStars = 10000;
    readonly float _playerSpeed = 0.3f;
    readonly float _cameraPadSpeed = 3f;
    readonly float _planetRadius = 2000f;

    // Multiple of 10, minimum 20.
    // This is of the full planet and is used as a base for LODs.
    readonly int _planetResolution = 700;
    readonly float _maxMoonInitialVelocity = 500f;
    readonly int _minMoonSize = 200;
    readonly int _maxMoonSize = 450;
    readonly int _minMoonlet = 100;
    readonly int _maxMoonlet = 150;
    readonly float _moonAlpha = 0.6f;
    readonly float _moonletAlpha = 0.6f;

    Color[] colors =
    {
        new Color("#000000"),
        new Color("#E50000"),
        new Color("#FF8D00"),
        new Color("#FFEE00"),
        new Color("#028121"),
        new Color("#004CFF"),
        new Color("#770088"),
        new Color("#FFFFFF"),
        new Color("#FFAFC7"),
        new Color("#73D7EE"),
        new Color("#613915")
    };

    float[] starLuminosity;
    Vector3 starRotation;
    float starSpeed;
    int starIndex;

    public override void _Ready()
    {
        _InitializeUniverse();

        GuiManager ??= GetNode<GUI>("GUI");
        GuiManager.Initialize();

        if (Planet == null || Planet.IsQueuedForDeletion())
        {
            _InitializePlanet();
        }

        WatcherCam ??= GetNode<Camera3D>("Pivot/Watcher");
        WatcherCam.Current = false;
        PlayerCam.Current = true;

        if (Sunlight == null)
        {
            Sunlight = new DirectionalLight3D
            {
                LightIntensityLumens = 10,
                LightColor = new Color("#808080"),
                ShadowEnabled = true,
                DirectionalShadowMode = DirectionalLight3D.ShadowMode.Parallel4Splits,
                LightAngularDistance = 5.0f,
                ShadowBias = 0.1f,
                ShadowNormalBias = 1f,
                DirectionalShadowPancakeSize = 0f,
                DirectionalShadowBlendSplits = true,
                DirectionalShadowMaxDistance = 500f
            };
            AddChild(Sunlight);
        }

        Environment ??= GetNode<WorldEnvironment>("WorldEnvironment").Environment;
        Sky ??= Environment.Sky.SkyMaterial as ShaderMaterial;
        int skyColor = Random.RandiRange(0, 11);
        Sky.SetShaderParameter(
            "rayleigh_color",
            new Color(Crayons[12 + ((skyColor + Offset(2)) % 12)])
        );
        var mieIndex = skyColor + Offset(1);
        // rolling -1 gives a chance at an unrelated mie color.
        var mieColor = new Color(Crayons[mieIndex > 0 ? mieIndex : Random.RandiRange(0, 11)]);
        Sky.SetShaderParameter("mie_color", mieColor);
        Sky.SetShaderParameter(
            "ground_color",
            new Color(Crayons[12 + ((skyColor + Offset(1)) % 12)])
        );

        Atmosphere.Call("set_shader_parameter", "u_atmosphere_ambient_color", mieColor);

        var ggCount = Random.RandiRange(1, _numGG);
        _InitializeGasGiants(ggCount);
        var moonCount = Random.RandiRange(10, _numMoons);
        _InitializeMoons(moonCount);
        var moonletCount = Random.RandiRange(50, _numMoonlets);
        _InitializeMoonlets(moonletCount);
        _InitializeStars(_numStars);

        GUI.Objects.Text =
            "pockets: "
            + ggCount.ToString()
            + ", "
            + "cues: "
            + moonCount.ToString()
            + ", "
            + "moons: "
            + moonletCount.ToString()
            + ", "
            + "stars: "
            + starIndex.ToString();

        Bodies.ForEach(body =>
            body.BaseRotation = Utils.RandomPointOnUnitSphere() * Random.RandfRange(0.5f, 2f)
        );
        Planet.BaseRotation = Utils.RandomPointOnUnitSphere();
        Planet.RotationSpeed = Random.RandfRange(0.03f, 0.07f);
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        if (Initialized)
        {
            Bodies.ForEach(body =>
                body.UpdateVelocity(Bodies, Planet.Transform.Origin, (float)delta)
            );
            Bodies.ForEach(body => body.UpdatePosition((float)delta));
            Planet.UpdatePosition((float)delta);
            Stars.RotateObjectLocal(starRotation.Normalized(), (float)delta * starSpeed);
        }

        Sunlight.Rotation = new Vector3(
            Mathf.Wrap(Sunlight.Rotation.X + (float)delta / _sunSpeed, -Mathf.Pi, Mathf.Pi),
            Mathf.Wrap(Sunlight.Rotation.Y + (float)delta / _sunSpeed * 2, -Mathf.Pi, Mathf.Pi),
            Mathf.Wrap(Sunlight.Rotation.Z + (float)delta / _sunSpeed * 3, -Mathf.Pi, Mathf.Pi)
        );

        var planetDot = (Planet.Transform.Basis * PlayerPivot.Transform.Basis).Y.Dot(
            Sunlight.Transform.Basis.Z
        );

        // Morning
        if (planetDot > _previousDot)
        {
            var time = Mathf.Lerp(0f, 12f, (planetDot + 1f) * 0.5f);
            var hour = Mathf.FloorToInt(time);
            var min = Mathf.FloorToInt(Mathf.Lerp(0f, 60f, time - (float)Math.Truncate(time)));
            if (hour == 0)
            {
                GUI.Time.Text = "12:" + (min < 10 ? "0" : "") + min.ToString() + " AM";
            }
            else
            {
                GUI.Time.Text =
                    hour.ToString() + ":" + (min < 10 ? "0" : "") + min.ToString() + " AM";
            }
        }
        // Evening
        else
        {
            var time = Mathf.Lerp(0f, 12f, 1f - (planetDot + 1f) * 0.5f);
            var hour = Mathf.CeilToInt(time);
            var min = Mathf.FloorToInt(Mathf.Lerp(0f, 60f, time - (float)Math.Truncate(time)));
            GUI.Time.Text = hour.ToString() + ":" + (min < 10 ? "0" : "") + min.ToString() + " PM";
        }

        _previousDot = planetDot;

        Sky.SetShaderParameter("sun_energy", Mathf.Lerp(0.3f, 1f, planetDot + 1f));
        Sky.SetShaderParameter("sun_fade", Mathf.Lerp(0.5f, 1f, planetDot + 1f));

        var starsOut = Mathf.FloorToInt(
            Mathf.Clamp(Mathf.Lerp(starIndex, 0f, planetDot + 1f), 0f, starIndex)
        );
        Stars.Multimesh.VisibleInstanceCount = starsOut;
        for (int i = 0; i < starsOut; i++)
        {
            var color = Stars.Multimesh.GetInstanceColor(i);
            color.A = Mathf.Clamp(Mathf.Lerp(starLuminosity[i], 0f, planetDot + 1f), 0f, 1f);
            Stars.Multimesh.SetInstanceColor(i, color);
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("reset"))
        {
            Bodies.ForEach(body => body.QueueFree());
            Bodies.Clear();
            Planet.QueueFree();
            Stars.Visible = false;
            GUI.Progress.Value = 0;
            GUI.Progress.Visible = true;
            _Ready();
        }

        if (@event.IsActionPressed("camera_toggle"))
        {
            if (PlayerCam.Current == false)
            {
                PlayerCam.Current = true;
                WatcherCam.Current = false;
            }
            else
            {
                PlayerCam.Current = false;
                WatcherCam.Current = true;
            }
        }

        if (@event.IsActionPressed("slower"))
        {
            _sunSpeed *= 2;
        }

        if (@event.IsActionPressed("faster"))
        {
            _sunSpeed /= 2;
        }

        if (@event is InputEventMouseMotion mouseMotion && PlayerPivot != null)
        {
            PlayerPivot.CameraRotation.X = mouseMotion.Relative.X;
            PlayerPivot.CameraRotation.Y = mouseMotion.Relative.Y;
            PlayerPivot.IsMouse = true;
        }

        if (@event is InputEventJoypadMotion && PlayerPivot != null)
        {
            Vector2 cameraVelocity = Input.GetVector(
                "camera_left",
                "camera_right",
                "camera_up",
                "camera_down"
            );
            Vector2 velocity = Input.GetVector("move_left", "move_right", "move_up", "move_down");
            PlayerPivot.Velocity = velocity;
            PlayerPivot.CameraRotation = cameraVelocity;
            PlayerPivot.IsMouse = false;
        }
    }

    void _InitializeUniverse()
    {
        Random = new RandomNumberGenerator();
        Random.Randomize();
        Seed = File.ReadLines("scrabble.txt").ElementAtOrDefault(Random.RandiRange(0, 267706));
        Random.Seed = (ulong)Seed.GetHashCode();
        OutOfBounds = 0;
        Gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
        CurrentFace = Face.Top;
        starLuminosity = new float[_numStars];

        GD.Print("universe ready");
    }

    void _InitializePlanet()
    {
        Planet = new CubePlanet
        {
            Seed = (int)Random.Randi(),
            Radius = _planetRadius,
            // Multiple of 10, minimum 20.
            // This is of the full planet and is used as a base for LODs.
            Resolution = _planetResolution
        };
        AddChild(Planet);

        if (CameraArm == null)
        {
            CameraArm = new Node3D();
            AddChild(CameraArm);
        }
        else
        {
            CameraArm.Reparent(GetParent());
            Transform3D trans = CameraArm.Transform;
            trans.Basis = Basis.Identity;
            trans.Origin = Planet.Transform.Origin;
            CameraArm.Transform = trans;
        }
        if (PlayerCam == null)
        {
            PlayerCam = GetNode<Camera3D>("PlayerCam");
        }
        else
        {
            PlayerCam.Reparent(GetParent());
            Transform3D trans = PlayerCam.Transform;
            trans.Basis = Basis.Identity;
            trans.Origin = Planet.Transform.Origin;
            PlayerCam.Transform = trans;
        }

        PlayerCam.Reparent(CameraArm);
        CameraArm.Translate(
            Planet.Transform.Origin
                + Vector3.Up
                    * (Planet.Shapes.DetermineElevation(Vector3.Up).scaled + CameraFloatHeight)
        );

        PlayerPivot = new Pivot { Speed = _playerSpeed, OrientForward = true };
        Planet.AddChild(PlayerPivot);
        CameraArm.Reparent(PlayerPivot);
        PlayerPivot.Camera = PlayerCam;

        if (Atmosphere == null)
        {
            AtmosphereScript = GD.Load<GDScript>(
                "res://addons/zylann.atmosphere/planet_atmosphere.gd"
            );
            Atmosphere = (Node3D)AtmosphereScript.New();
            Atmosphere.Set("sun_path", Sunlight);
            Atmosphere.Set("planet_radius", Planet.Radius);
            Atmosphere.Set("atmosphere_height", Planet.Radius / 10f);
            Planet.AddChild(Atmosphere);
        }
        else
        {
            Atmosphere.Reparent(Planet);
        }

        GD.Print("planet created");
    }

    void _InitializeMoons(int moonCount)
    {
        float maxV = _maxMoonInitialVelocity;
        float maxDistance = Radius;
        for (int i = 0; i < moonCount; i++)
        {
            var sphere = new Spheroid
            {
                Seed = i,
                Radius = Random.RandiRange(_minMoonSize, _maxMoonSize)
            };
            sphere.rings = Mathf.FloorToInt(sphere.Radius);
            sphere.radialSegments = sphere.rings;
            sphere.Gravity = sphere.Radius / 10f;
            sphere.initialVelocity = new Vector3(
                Random.Randf() * maxV * 2 - maxV,
                Random.Randf() * maxV * 2 - maxV,
                Random.Randf() * maxV * 2 - maxV
            );

            float distance =
                Random.RandfRange(Planet.Radius * 2, maxDistance) + (sphere.Radius * 10);
            if (Random.Randf() < 0.5f)
            {
                distance = -distance;
            }
            sphere.Translate(Utils.RandomPointOnUnitSphere() * distance);

            var chance = Random.Randf();
            if (chance < 0.2f)
            {
                chance = Random.Randf();
                Color[] crayons;
                // classic rainbow
                if (chance < 0.15f)
                {
                    crayons = new[]
                    {
                        new Color("#E50000"),
                        new Color("#FF8D00"),
                        new Color("#FFEE00"),
                        new Color("#028121"),
                        new Color("#004CFF"),
                        new Color("#770088")
                    };

                    // progress rainbow
                }
                else if (chance < 0.3f)
                {
                    crayons = new[]
                    {
                        new Color("#FFFFFF"),
                        new Color("#FFAFC7"),
                        new Color("#73D7EE"),
                        new Color("#613915"),
                        new Color("#000000"),
                        new Color("#E50000"),
                        new Color("#FF8D00"),
                        new Color("#FFEE00"),
                        new Color("#028121"),
                        new Color("#004CFF"),
                        new Color("#770088")
                    };

                    // transgender
                }
                else if (chance < 0.45f)
                {
                    crayons = new[]
                    {
                        new Color("#5BCFFB"),
                        new Color("#F5ABB9"),
                        new Color("#FFFFFF"),
                        new Color("#F5ABB9"),
                        new Color("#5BCFFB")
                    };

                    // lesbian
                }
                else if (chance < 0.6f)
                {
                    crayons = new[]
                    {
                        new Color("#D62800"),
                        new Color("#FF9B56"),
                        new Color("#FFFFFF"),
                        new Color("#D462A6"),
                        new Color("#A40062"),
                    };

                    // bisexual
                }
                else if (chance < 0.7f)
                {
                    crayons = new[]
                    {
                        new Color("#D60270"),
                        new Color("#9B4F96"),
                        new Color("#0038A8")
                    };

                    // pansexual
                }
                else if (chance < 0.8f)
                {
                    crayons = new[]
                    {
                        new Color("#FF1C8D"),
                        new Color("#FFD700"),
                        new Color("#1AB3FF")
                    };

                    // nonbinary
                }
                else if (chance < 0.9f)
                {
                    crayons = new[]
                    {
                        new Color("#FCF431"),
                        new Color("#FCFCFC"),
                        new Color("#9D59D2"),
                        new Color("#282828")
                    };

                    // genderfluid
                }
                else
                {
                    crayons = new[]
                    {
                        new Color("#FE76A2"),
                        new Color("#FFFFFF"),
                        new Color("#BF12D7"),
                        new Color("#000000"),
                        new Color("#303CBE")
                    };
                }

                var offset = Random.RandiRange(0, crayons.Length - 1);
                sphere.Crayons = new Color[crayons.Length];
                for (var idx = offset; idx < crayons.Length + offset; idx++)
                {
                    sphere.Crayons[idx % crayons.Length] = crayons[idx % crayons.Length];
                    sphere.Crayons[idx % crayons.Length].A = _moonAlpha;
                }
            }
            else
            {
                sphere.Crayons = new[]
                {
                    colors[i % colors.Length],
                    colors[(i + Random.RandiRange(1, 32)) % colors.Length]
                };
                for (int j = 0; j < sphere.Crayons.Length; j++)
                {
                    sphere.Crayons[j].A = _moonAlpha;
                }
            }
            sphere.Visible = false;
            Bodies.Add(sphere);
            AddChild(sphere);
        }

        GD.Print("throwing together " + moonCount + " moons");
    }

    void _InitializeGasGiants(int gasGiantCount)
    {
        Vector3 gasGiantPosition = Vector3.Zero;
        for (int i = 0; i < gasGiantCount; i++)
        {
            var giantSize = Random.RandiRange(1000, 10000);
            var lightColor = colors[Random.RandiRange(1, 10)];
            var gg = new GasGiant
            {
                Gravity = giantSize,
                EventHorizon = giantSize / 5f,
                Radius = 0.0f,
                OmniRange = giantSize * 2f,
                OmniAttenuation = 0.2f,
                LightColor = lightColor,
                // ShadowEnabled = true,
                // LightSize = giantSize / 15f,
                ShadowBias = 0.3f,
                ShadowBlur = 5f,
                RotationSpeed = Random.RandfRange(0.1f, 0.3f)
            };
            gg.rings = Mathf.FloorToInt(gg.EventHorizon);
            gg.radialSegments = gg.rings;

            float distance = Random.RandfRange(Planet.Radius * 2, Radius) + (gg.EventHorizon * 2);
            if (Random.Randf() < 0.5f)
            {
                distance = -distance;
            }
            var chance = Random.Randf();
            if (gasGiantPosition == Vector3.Zero || chance > 0.05f)
            {
                gasGiantPosition = Utils.RandomPointOnUnitSphere() * distance;
            }
            gg.Translate(gasGiantPosition);
            gg.Visible = false;
            // var gas = (Node3D)AtmosphereScript.New();
            // gas.Set("sun_path", Sunlight);
            // gas.Set("planet_radius", gasGiant.Radius);
            // gas.Set("atmosphere_height", gasGiant.Radius);
            // gas.Call("set_shader_parameter", "u_atmosphere_ambient_color", lightColor);
            // gasGiant.AddChild(gas);

            Bodies.Add(gg);
            AddChild(gg);
        }

        GD.Print("pondered " + gasGiantCount + " glowing orbs");
    }

    void _InitializeMoonlets(int asteroidCount)
    {
        float maxV = _maxMoonInitialVelocity / 10f;
        float maxDistance = Radius * 0.333f;
        for (int i = 0; i < asteroidCount; i++)
        {
            var sphere = new MicroSpheroid
            {
                Seed = i,
                Radius = Random.RandiRange(_minMoonlet, _maxMoonlet)
            };
            sphere.rings = Mathf.FloorToInt(sphere.Radius);
            sphere.radialSegments = sphere.rings;
            sphere.Gravity = sphere.Radius / 5f;
            sphere.initialVelocity = new Vector3(
                Random.Randf() * maxV * 2 - maxV,
                Random.Randf() * maxV * 2 - maxV,
                Random.Randf() * maxV * 2 - maxV
            );

            float distance = Random.RandfRange(Planet.Radius, maxDistance);
            if (Random.Randf() < 0.5f)
            {
                distance = -distance;
            }
            sphere.Translate(Utils.RandomPointOnUnitSphere() * distance);

            // sphere.Translate(new Vector3(transX, transY, transZ));
            sphere.crayons = new[]
            {
                colors[i % colors.Length],
                colors[(i + Random.RandiRange(1, 32)) % colors.Length]
            };
            for (int j = 0; j < sphere.crayons.Length; j++)
            {
                sphere.crayons[j].A = _moonletAlpha;
            }
            sphere.Visible = false;
            Bodies.Add(sphere);
            AddChild(sphere);
        }
        GD.Print("spun up " + asteroidCount + " little moonlets");
    }

    void _InitializeStars(int starCount)
    {
        if (Stars == null)
        {
            var material = new StandardMaterial3D
            {
                VertexColorUseAsAlbedo = true,
                ClearcoatEnabled = true,
                ClearcoatRoughness = 1.0f,
                EmissionEnabled = true,
                Roughness = 1.0f,
                RimEnabled = true,
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            };
            var multiMesh = new MultiMesh();
            Stars = new MultiMeshInstance3D { Multimesh = multiMesh, Position = Vector3.Zero };

            var mesh = MeshUtils.GenerateCubeMesh(Colors.White, 20);
            mesh.SurfaceSetMaterial(0, material);

            Stars.Multimesh.TransformFormat = MultiMesh.TransformFormatEnum.Transform3D;
            Stars.Multimesh.UseColors = true;
            Stars.Multimesh.Mesh = mesh;
            Planet.AddChild(Stars);
        }
        else
        {
            Stars.Reparent(Planet);
        }
        starRotation = Utils.RandomPointOnUnitSphere();
        starSpeed = Mathf.Lerp(0.001f, 0.03f, Random.Randf());

        Stars.Multimesh.InstanceCount = 0;
        Stars.Multimesh.InstanceCount = starCount;

        var starNoise = new FastNoiseLite
        {
            NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex,
            FractalOctaves = 4,
            Seed = Universe.Seed.GetHashCode(),
            Frequency = Universe.Random.RandfRange(0.00005f, 0.001f),
            DomainWarpEnabled = true,
            DomainWarpFractalOctaves = 4,
            DomainWarpAmplitude = 100f,
            DomainWarpFrequency = Universe.Random.RandfRange(0.0005f, 0.001f)
        };

        starIndex = 0;
        for (int i = 0; i < starCount; i++)
        {
            var distance = Random.RandfRange(Planet.Radius + 3500f, Planet.Radius + 10000f);
            var position = Utils.RandomPointOnUnitSphere() * distance;
            var noiseValue = starNoise.GetNoise3Dv(position);
            if (Random.Randf() * 0.25f > noiseValue)
            {
                continue;
            }

            var transform = Transform3D.Identity.Translated(position);
            Stars.Multimesh.SetInstanceTransform(starIndex, transform);

            var chance = Random.Randf();
            Color color = Colors.White;
            if (chance < 0.05)
            {
                color = new Color(Utils.Crayons[Random.RandiRange(0, 47)]);
            }
            else if (chance < 0.2f)
            {
                color = new Color(Utils.Crayons[Random.RandiRange(0, 47)]).Lightened(0.2f);
            }
            else if (chance < 0.7f)
            {
                color = new Color(Utils.Crayons[Random.RandiRange(0, 47)]).Lightened(0.4f);
            }
            starLuminosity[starIndex] = Mathf.Abs(Random.Randfn() - 0.5f) / 5f;
            color.A = starLuminosity[starIndex];
            Stars.Multimesh.SetInstanceColor(starIndex, color);

            starIndex++;
        }

        Stars.Multimesh.VisibleInstanceCount = starIndex + 1;
        Stars.Visible = false;
        GD.Print("putting " + starIndex + " stars in the sky for you");
    }
}
