using Godot;
using System;
using System.Collections.Generic;
using static Utils;

public partial class Universe : Node3D
{
	public static List<HeavenlyBody> Bodies = new List<HeavenlyBody>();
	public static CubePlanet Planet;
	public static CollisionShape3D PlanetCollider;
	public static Pivot PlayerPivot;
	public static Label InfoText;
	public static Camera3D PlayerCam;
	public static Camera3D WatcherCam;
	public static float Gravity;
	public static int Radius = 7500;
	public static RandomNumberGenerator Random;
	public static int Seed;
	public static Face CurrentFace;
	public static Vector2 Location;
	public static DirectionalLight3D Sunlight;
	public static Godot.Environment Environment;

	Vector3 _rotate = Vector3.Zero;

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
		//Random.Seed = 123456;
		Random.Randomize();

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
		InfoText ??= GetNode<Label>("InfoText");

		Environment ??= GetNode<WorldEnvironment>("WorldEnvironment").Environment;
		var sky = Environment.Sky.SkyMaterial as PhysicalSkyMaterial;
		int skyColor = Random.RandiRange(0, 11);
		sky.RayleighColor = new Color(Crayons[12 + ((skyColor + Offset(2)) % 12)]);
		sky.MieColor = new Color(Crayons[skyColor]);
		sky.GroundColor = sky.RayleighColor.Darkened(0.05f);

		_InitializeStars(4);
		_InitializeMoons(40);
	}

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);
		Bodies.ForEach(body => body.UpdateVelocity(Bodies, Transform.Origin, (float)delta));
		Bodies.ForEach(body => body.UpdatePosition((float)delta));

		Sunlight.Rotation = new Vector3(
			Mathf.Wrap(Sunlight.Rotation.X + (float)delta / 16, -Mathf.Pi, Mathf.Pi), 
			Mathf.Wrap(Sunlight.Rotation.Y + (float)delta / 32, -Mathf.Pi, Mathf.Pi), 
			Mathf.Wrap(Sunlight.Rotation.Z + (float)delta / 96, -Mathf.Pi, Mathf.Pi) 
		);

		Planet.Rotation = new Vector3(
			Mathf.Wrap(Planet.Rotation.X + (float)delta * _rotate.X / 10, -Mathf.Pi, Mathf.Pi),
			Planet.Rotation.Y,
			Mathf.Wrap(Planet.Rotation.Z + (float)delta * _rotate.Z / 10, -Mathf.Pi, Mathf.Pi)
		);

		for (int i = 0; i < Bodies.Count; i++) {
			switch(i%3) {
				case 0:
					Bodies[i].RotateObjectLocal(new Vector3(1,0,0), (float)delta * _rotate.X);
					break;
				case 1:
					Bodies[i].RotateObjectLocal(new Vector3(1,0,1).Normalized(), (float)delta * _rotate.Y);
					break;
				case 2:
					Bodies[i].RotateObjectLocal(new Vector3(0,0,1), (float)delta * _rotate.Z);
					break;
			}
		}
	}

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("jump")) {
			Bodies.ForEach(body => body.QueueFree());
			Bodies.Clear();
			Planet.QueueFree();
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
    }

	void _InitializePlanet() {
		GD.Print("adding planet");
		Planet = new CubePlanet
		{
			Seed = (int)Random.Randi(),
			Radius = 2000,
			Resolution = 600
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
		PlayerCam.Translate(Planet.Transform.Origin + Vector3.Up * (Planet.Shapes.DetermineElevation(Vector3.Up).scaled + 50f));
		PlayerPivot = new Pivot
		{
			Speed = 0.2f,
			OrientForward = true
		};
		Planet.AddChild(PlayerPivot);
		PlayerCam.Reparent(PlayerPivot);
		PlayerPivot.Camera = PlayerCam;

		if (PlanetCollider == null) {
			PlanetCollider = new CollisionShape3D
			{
				Shape = new SphereShape3D
				{
					Radius = Planet.Radius
				}
			};
		}

		if (PlanetCollider.GetParent() != null) {
			PlanetCollider.Reparent(Planet);
		} else {
			Planet.AddChild(PlanetCollider);
		}
	}

	void _InitializeMoons(int moonCount) 
	{
		float maxV = 1000f;
		for (int i = 0; i < moonCount; i++) {
            var sphere = new Spheroid
            {
                Seed = i,
                Radius = Random.RandiRange(50, 300)
            };
            sphere.rings = Mathf.FloorToInt(sphere.Radius);
			sphere.radialSegments = sphere.rings;
			sphere.Gravity = sphere.Radius / 10f;
			sphere.initialVelocity = new Vector3(Random.Randf() * maxV * 2 - maxV, Random.Randf() * maxV * 2 - maxV, Random.Randf() * maxV * 2 - maxV);
			float transX = Random.RandfRange(-Radius, Radius);
			if (transX < 0) {
				transX -= Planet.Radius;
			} else {
				transX += Planet.Radius;
			}
			float transY = Random.RandfRange(-Radius, Radius);
			if (transY < 0) {
				transY -= Planet.Radius;
			} else {
				transY += Planet.Radius;
			}
			float transZ = Random.RandfRange(-Radius, Radius);
			if (transZ < 0) {
				transZ -= Planet.Radius;
			} else {
				transZ += Planet.Radius;
			}
			sphere.Translate(new Vector3(transX, transY, transZ));

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
				sphere.crayons = new Color[crayons.Length];
				for (var idx = offset; idx < crayons.Length + offset; idx++) {
					sphere.crayons[idx%crayons.Length] = crayons[idx%crayons.Length];
				}

			} else {
				sphere.crayons = new[] { 
					colors[i%colors.Length],
					colors[(i + Random.RandiRange(1, 32))%colors.Length]
				};
			}
			Bodies.Add(sphere);
			AddChild(sphere);
		}
	}

	void _InitializeStars(int starCount) 
	{
		for (int i = 0; i < starCount; i++) {
            var star = new Star
            {
                Gravity = Random.RandiRange(100, 1000),
                Radius = 0.1f,
                OmniRange = 5000f,
                OmniAttenuation = 0.2f,
                LightIntensityLumens = 1000f,
                LightColor = colors[Random.RandiRange(1, 10)]
            };
            Bodies.Add(star);
			AddChild(star);
		}
	} 
}