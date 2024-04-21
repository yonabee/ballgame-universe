using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using static Utils;

public partial class Universe : Node3D
{
	public static List<HeavenlyBody> Bodies = new List<HeavenlyBody>();
	public static CubePlanet Planet;
	public static Pivot PlayerPivot;
	public static Label InfoText;
	public static Label InfoText2;
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
	public static ProgressBar Progress;
	public static Node3D Atmosphere;
	public static GDScript AtmosphereScript;

	public static readonly bool ConstructPlanetColliders = true;
	public static int OutOfBounds = 0;
	public static bool Initialized = false;
	public static string Seed = "tatooine";

	Vector3 _rotate = Vector3.Zero;


	float _sunSpeed = 64f;
	readonly int _numStars = 5;
	readonly int _numMoons = 25;
	readonly int _numMoonlets = 100;
	readonly float _cameraFloatHeight = 75f;
	readonly float _cameraSpeed = 0.3f;
	readonly float _planetRadius = 2000f;
	// Multiple of 10, minimum 20. 
	// This is of the full planet and is used as a base for LODs.
	readonly int _planetResolution = 700;
	readonly float _maxMoonInitialVelocity = 500f;
	readonly int _minMoonSize = 100;
	readonly int _maxMoonSize = 350;
	readonly int _minMoonlet = 10;
	readonly int _maxMoonlet = 70;
	readonly float _moonAlpha = 0.6f;
	readonly float _moonletAlpha = 0.6f;

	Color[] colors = {
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

	public override void _Ready() 
	{
		Random = new RandomNumberGenerator();
		Random.Randomize();
		Seed = File.ReadLines("scrabble.txt").ElementAtOrDefault(Random.RandiRange(0,267751));
		Random.Seed = (ulong)Seed.GetHashCode();

		OutOfBounds = 0;

        Gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
		CurrentFace = Face.Top;

		_rotate.X = Random.RandfRange(-0.3f, 0.3f);
		_rotate.Y = Random.RandfRange(-0.3f, 0.3f);
		_rotate.Z = Random.RandfRange(-0.3f, 0.3f);

		GD.Print("universe ready");

		if (Planet == null || Planet.IsQueuedForDeletion()) {
			_InitializePlanet();
		}

		if (Sunlight == null) {
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

		WatcherCam ??= GetNode<Camera3D>("Pivot/Watcher");

		WatcherCam.Current = false;
		PlayerCam.Current = true;

		InfoText ??= GetNode<Label>("InfoText");
		InfoText2 ??= GetNode<Label>("InfoText2");
		Progress ??= GetNode<ProgressBar>("ProgressBar");
		InfoText.Text = Seed.ToLower();
		// InfoText2.Text = Seed.GetHashCode().ToString();

		Environment ??= GetNode<WorldEnvironment>("WorldEnvironment").Environment;
		Sky ??= Environment.Sky.SkyMaterial as ShaderMaterial;
		int skyColor = Random.RandiRange(0, 11);
		Sky.SetShaderParameter("rayleigh_color", new Color(Crayons[12 + ((skyColor + Offset(2)) % 12)]));
		var mieIndex = skyColor + Offset(1);
		// rolling -1 gives a chance at an unrelated mie color.
		var mieColor = new Color(Crayons[mieIndex > 0 ? mieIndex : Random.RandiRange(0,11)]);
		Sky.SetShaderParameter("mie_color", mieColor);
		Sky.SetShaderParameter("ground_color", new Color(Crayons[12 + ((skyColor + Offset(1)) % 12)]));

		Atmosphere.Call("set_shader_parameter", "u_atmosphere_ambient_color", mieColor);

		_InitializeGasGiants(Random.RandiRange(1, _numStars));
		_InitializeMoons(Random.RandiRange(1, _numMoons));
		_InitializeMoonlets(Random.RandiRange(100, _numMoonlets));

		Bodies.ForEach(body => body.BaseRotation = Utils.RandomPointOnUnitSphere() * Random.RandfRange(0.5f, 2f));
		Planet.BaseRotation = Utils.RandomPointOnUnitSphere();
		Planet.RotationSpeed = Random.RandfRange(0.03f, 0.07f);
	}

    public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);

		if (Initialized) {
			Bodies.ForEach(body => body.UpdateVelocity(Bodies, Planet.Transform.Origin, (float)delta));
			Bodies.ForEach(body => body.UpdatePosition((float)delta));
			Planet.UpdatePosition((float)delta);
		}

		Sunlight.Rotation = new Vector3(
			Mathf.Wrap(Sunlight.Rotation.X + (float)delta / _sunSpeed, -Mathf.Pi, Mathf.Pi), 
			Mathf.Wrap(Sunlight.Rotation.Y + (float)delta / _sunSpeed * 2, -Mathf.Pi, Mathf.Pi), 
			Mathf.Wrap(Sunlight.Rotation.Z + (float)delta / _sunSpeed * 3, -Mathf.Pi, Mathf.Pi) 
		);

		var planetDot = (Planet.Transform.Basis * PlayerPivot.Transform.Basis).Y.Dot(Sunlight.Transform.Basis.Z);

		// GD.Print(planetDot);

		Sky.SetShaderParameter("sun_energy", Mathf.Lerp(0.3f, 1f, planetDot + 1f));
		Sky.SetShaderParameter("sun_fade", Mathf.Lerp(0.5f, 1f, planetDot + 1f ));
	}

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("reset")) {
			Bodies.ForEach(body => body.QueueFree());
			Bodies.Clear();
			Planet.QueueFree();
			Progress.Value = 0;
			Progress.Visible = true;
			_Ready();
		}

		if (@event.IsActionPressed("camera_toggle")) {
			if (PlayerCam.Current == false) {
				PlayerCam.Current = true;
				WatcherCam.Current = false;
			} else {
				PlayerCam.Current = false;
				WatcherCam.Current = true;
			}
		}

		if (@event.IsActionPressed("info_toggle")) {
			InfoText.Visible = !InfoText.Visible;
		}

		if (@event.IsActionPressed("slower")) {
			_sunSpeed *=2;
		}

		if (@event.IsActionPressed("faster")) {
			_sunSpeed /=2;
		}
		      
        if(@event is InputEventMouseMotion mouseMotion && PlayerPivot != null)
		{
			PlayerPivot.CameraRotation.X = mouseMotion.Relative.X;
			PlayerPivot.CameraRotation.Y = mouseMotion.Relative.Y;
		}
    }

	void _InitializePlanet() {
		GD.Print("adding planet");
		Planet = new CubePlanet
		{
			Seed = (int)Random.Randi(),
			Radius = _planetRadius,
			// Multiple of 10, minimum 20. 
			// This is of the full planet and is used as a base for LODs.
			Resolution = _planetResolution
		};
		AddChild(Planet);

		if (PlayerCam == null) {
			PlayerCam = GetNode<Camera3D>("PlayerCam");
		} else {
			PlayerCam.Reparent(GetParent());
			Transform3D trans = PlayerCam.Transform;
			trans.Basis = Basis.Identity;
			trans.Origin = Planet.Transform.Origin;
			PlayerCam.Transform = trans;
		}
		PlayerCam.Translate(Planet.Transform.Origin + Vector3.Up * (Planet.Shapes.DetermineElevation(Vector3.Up).scaled + _cameraFloatHeight));
		PlayerPivot = new Pivot
		{
			Speed = _cameraSpeed,
			OrientForward = true
		};
		Planet.AddChild(PlayerPivot);
		PlayerCam.Reparent(PlayerPivot);
		PlayerPivot.Camera = PlayerCam;
		
		if (Atmosphere == null) {
			AtmosphereScript = GD.Load<GDScript>("res://addons/zylann.atmosphere/planet_atmosphere.gd");
			Atmosphere = (Node3D)AtmosphereScript.New();
			Atmosphere.Set("sun_path", Sunlight);
			Atmosphere.Set("planet_radius", Planet.Radius);
			Atmosphere.Set("atmosphere_height", Planet.Radius / 10f);
			Planet.AddChild(Atmosphere);
		} else {
			Atmosphere.Reparent(Planet);
		}
	}

	void _InitializeMoons(int moonCount) 
	{
		float maxV = _maxMoonInitialVelocity;
		float maxDistance = Radius;
		for (int i = 0; i < moonCount; i++) {
            var sphere = new Spheroid
            {
                Seed = i,
                Radius = Random.RandiRange(_minMoonSize, _maxMoonSize)
            };
            sphere.rings = Mathf.FloorToInt(sphere.Radius);
			sphere.radialSegments = sphere.rings;
			sphere.Gravity = sphere.Radius / 10f;
			sphere.initialVelocity = new Vector3(Random.Randf() * maxV * 2 - maxV, Random.Randf() * maxV * 2 - maxV, Random.Randf() * maxV * 2 - maxV);

			float distance = Random.RandfRange(Planet.Radius * 2, maxDistance) + (sphere.Radius * 10);
			if (Random.Randf() < 0.5f) {
				distance = -distance;
			}
			sphere.Translate(Utils.RandomPointOnUnitSphere() * distance);

			var chance = Random.Randf();
			if (chance < 0.2f) {
				chance = Random.Randf();
				Color[] crayons;
				// classic rainbow
				if (chance < 0.15f) {
					crayons = new[] {
						new Color("#E50000"),
						new Color("#FF8D00"),
						new Color("#FFEE00"),
						new Color("#028121"),
						new Color("#004CFF"),
						new Color("#770088")
					};

				// progress rainbow
				} else if (chance < 0.3f) {
					crayons = new[] {
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
				} else if (chance < 0.45f) {
					crayons = new[] {
						new Color("#5BCFFB"),
						new Color("#F5ABB9"),
						new Color("#FFFFFF"),
						new Color("#F5ABB9"),
						new Color("#5BCFFB")
					};

				// lesbian
				} else if (chance < 0.6f) {
					crayons = new[] {
						new Color("#D62800"),
						new Color("#FF9B56"),
						new Color("#FFFFFF"),
						new Color("#D462A6"),
						new Color("#A40062"),
					};
				
				// bisexual
				} else if (chance < 0.7f) {
					crayons = new[] {
						new Color("#D60270"),
						new Color("#9B4F96"),
						new Color("#0038A8")
					};

				// pansexual
				} else if (chance < 0.8f) {
					crayons = new[] {
						new Color("#FF1C8D"),
						new Color("#FFD700"),
						new Color("#1AB3FF")
					};

				// nonbinary
				} else if (chance < 0.9f) {
					crayons = new[] {
						new Color("#FCF431"),
						new Color("#FCFCFC"),
						new Color("#9D59D2"),
						new Color("#282828")
					};
				
				// genderfluid
				} else {
					crayons = new[] {
						new Color("#FE76A2"),
						new Color("#FFFFFF"),
						new Color("#BF12D7"),
						new Color("#000000"),
						new Color("#303CBE")
					};
				}					
				
				var offset = Random.RandiRange(0, crayons.Length - 1);
				sphere.Crayons = new Color[crayons.Length];
				for (var idx = offset; idx < crayons.Length + offset; idx++) {
					sphere.Crayons[idx%crayons.Length] = crayons[idx%crayons.Length];
					sphere.Crayons[idx%crayons.Length].A = _moonAlpha;
				}

			} else {
				sphere.Crayons = new[] { 
					colors[i%colors.Length],
					colors[(i + Random.RandiRange(1, 32))%colors.Length]
				};
				for(int j = 0; j < sphere.Crayons.Length; j++) {
					sphere.Crayons[j].A = _moonAlpha;
				}
			}
			sphere.Visible = false;
			Bodies.Add(sphere);
			AddChild(sphere);
		}
	}

	void _InitializeGasGiants(int gasGiantCount) 
	{
		Vector3 gasGiantPosition = Vector3.Zero;
		for (int i = 0; i < gasGiantCount; i++) {
			var giantSize = Random.RandiRange(1000, 10000);
			var lightColor = colors[Random.RandiRange(1, 10)];
            var gg = new GasGiant
            {
                Gravity = giantSize,
				EventHorizon = giantSize / 5f,
                Radius = 0.0f,
                OmniRange = giantSize,
                OmniAttenuation = 0.2f,
                LightColor = lightColor,
				// ShadowEnabled = true,
				// LightSize = giantSize / 15f,
				ShadowBias = 0.3f,
				ShadowBlur = 5f,
				RotationSpeed = Random.RandfRange(0.1f, 0.3f)
            };

			float distance = Random.RandfRange(Planet.Radius * 2, Radius) + (gg.EventHorizon * 2);
			if (Random.Randf() < 0.5f) {
				distance = -distance;
			}
			var chance = Random.Randf();
			if (gasGiantPosition == Vector3.Zero || chance > 0.05f) {
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
	}

	void _InitializeMoonlets(int moonletCount)
	{
		
		float maxV = _maxMoonInitialVelocity / 10f;
		float maxDistance = Radius * 0.333f;
		for (int i = 0; i < moonletCount; i++) {
            var sphere = new MicroSpheroid
            {
                Seed = i,
                Radius = Random.RandiRange(_minMoonlet, _maxMoonlet)
            };
            sphere.rings = Mathf.FloorToInt(sphere.Radius);
			sphere.radialSegments = sphere.rings;
			sphere.Gravity = sphere.Radius / 5f;
			sphere.initialVelocity = new Vector3(Random.Randf() * maxV * 2 - maxV, Random.Randf() * maxV * 2 - maxV, Random.Randf() * maxV * 2 - maxV);

			float distance = Random.RandfRange(Planet.Radius, maxDistance);
			if (Random.Randf() < 0.5f) {
				distance = -distance;
			}
			sphere.Translate(Utils.RandomPointOnUnitSphere() * distance);

			// sphere.Translate(new Vector3(transX, transY, transZ));
			sphere.crayons = new[] { 
				colors[i%colors.Length],
				colors[(i + Random.RandiRange(1, 32))%colors.Length]
			};
			for(int j = 0; j < sphere.crayons.Length; j++) {
				sphere.crayons[j].A = _moonletAlpha;
			}
			sphere.Visible = false;
			Bodies.Add(sphere);
			AddChild(sphere);
		}

	} 
}