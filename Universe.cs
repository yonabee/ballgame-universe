using Godot;
using System;
using System.Collections.Generic;

public partial class Universe : Node3D
{
	List<HeavenlyBody> bodies = new List<HeavenlyBody>();
	public static CubePlanet Planet;
	public static Pivot PlayerPivot;
	public static Camera3D PlayerCam;
	public static Camera3D WatcherCam;
	public static float Gravity;

	public static int Radius = 2000;

	Vector3 _rotate = Vector3.Zero;

	DirectionalLight3D otherSun;
	OmniLight3D sun;

	RandomNumberGenerator random;

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
		var random = new RandomNumberGenerator();

        Gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

		_rotate.X = random.RandfRange(-0.3f, 0.3f);
		_rotate.Y = random.RandfRange(-0.3f, 0.3f);
		_rotate.Z = random.RandfRange(-0.3f, 0.3f);

		// if (sun == null) {
		// 	sun = new OmniLight3D();
		// 	sun.OmniRange = 2500f;
		// 	sun.OmniAttenuation = 0.2f;
		// 	sun.LightIntensityLumens = 1000;
		// 	sun.ShadowEnabled = true;
		// 	AddChild(sun);
		// }

		GD.Print("universe ready");

		if (Planet == null || Planet.IsQueuedForDeletion()) {
			GD.Print("adding planet");
			Planet = new CubePlanet();
			Planet.Seed = (int)random.Randi();
			Planet.Gravity = 10;
			Planet.Radius = 1000;
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

			PlayerPivot = new Pivot();
			PlayerPivot.Speed = 0.2f;
			PlayerPivot.OrientForward = true;
			Planet.AddChild(PlayerPivot);
			PlayerCam.Reparent(PlayerPivot);
			// Transform3D transform = PlayerCam.Transform;
			// transform.Basis = Basis.Identity;
			// PlayerCam.Transform = transform;
			PlayerPivot.Camera = PlayerCam;
			//PlayerPivot.RotateX(-Mathf.Pi / 2f);
		}

		if (otherSun == null) {
			otherSun = new DirectionalLight3D();
			otherSun.LightIntensityLumens = 10;
			otherSun.LightColor = new Color("#808080");
			otherSun.ShadowEnabled = true;
			otherSun.DirectionalShadowMode = DirectionalLight3D.ShadowMode.Parallel4Splits;
			otherSun.LightAngularDistance = 5.0f;
			otherSun.ShadowBias = 1f;
			otherSun.ShadowNormalBias = 2f;
			otherSun.DirectionalShadowPancakeSize = 0f;
			otherSun.DirectionalShadowBlendSplits = true;
			otherSun.DirectionalShadowMaxDistance = 500f;
			AddChild(otherSun);
		}

		if (WatcherCam == null) {
			WatcherCam = GetNode<Camera3D>("Pivot/Watcher");
		}

		int sphereCount = 80;
		int starCount = 4;
		float maxV = 1000f;

		for (int i = 0; i < starCount; i++) {
			var star = new Star();
			star.Gravity = random.RandiRange(100, 1000);
			star.Radius = 0.1f;
			star.OmniRange = 5000f;
			star.OmniAttenuation = 0.2f;
			star.LightIntensityLumens = 1000f;
			star.LightColor = colors[random.RandiRange(1, 10)];
			bodies.Add(star);
			AddChild(star);

			// var starChild = new Spheroid();
			// starChild.Radius = 25f;
			// starChild.rings = 20;
			// starChild.radialSegments = 20;
			// starChild.Gravity = 10;
			// starChild.crayons = new[] { new Color(star.LightColor.R * 10, star.LightColor.G * 10, star.LightColor.B * 10) };
			// star.AddChild(starChild);
		}

		for (int i = 0; i < sphereCount; i++) {
			var sphere = new Spheroid();
			sphere.Seed = i;
			sphere.Radius = random.RandiRange(50, 300);
			sphere.rings = Mathf.FloorToInt(sphere.Radius);
			sphere.radialSegments = sphere.rings;
			sphere.Gravity = sphere.Radius / 10f;
			sphere.initialVelocity = new Vector3(random.Randf() * maxV * 2 - maxV, random.Randf() * maxV * 2 - maxV, random.Randf() * maxV * 2 - maxV);
			float transX = random.RandfRange(-Radius, Radius);
			if (transX < 0) {
				transX -= Planet.Radius;
			} else {
				transX += Planet.Radius;
			}
			float transY = random.RandfRange(-Radius, Radius);
			if (transY < 0) {
				transY -= Planet.Radius;
			} else {
				transY += Planet.Radius;
			}
			float transZ = random.RandfRange(-Radius, Radius);
			if (transZ < 0) {
				transZ -= Planet.Radius;
			} else {
				transZ += Planet.Radius;
			}
			sphere.Translate(new Vector3(transX, transY, transZ));
			// switch(i%8) {
			// 	case 0:
			// 		sphere.TranslateObjectLocal(new Vector3(random.RandiRange(-Radius, -Mathf.FloorToInt(Planet.Radius)),random.RandiRange(-Radius, -Mathf.FloorToInt(Planet.Radius)),random.RandiRange(-Radius, -Mathf.FloorToInt(Planet.Radius))));
			// 		break;
			// 	case 1:
			// 		sphere.TranslateObjectLocal(new Vector3(random.RandiRange(Radius, Mathf.FloorToInt(Planet.Radius)),random.RandiRange(-Radius, -Mathf.FloorToInt(Planet.Radius)),random.RandiRange(-Radius, -Mathf.FloorToInt(Planet.Radius))));
			// 		break;
			// 	case 2:
			// 		sphere.TranslateObjectLocal(new Vector3(random.RandiRange(-Radius, -Mathf.FloorToInt(Planet.Radius)),random.RandiRange(Radius, Mathf.FloorToInt(Planet.Radius)),random.RandiRange(-Radius, -Mathf.FloorToInt(Planet.Radius))));
			// 		break;
			// 	case 3:
			// 		sphere.TranslateObjectLocal(new Vector3(random.RandiRange(-Radius, -Mathf.FloorToInt(Planet.Radius)),random.RandiRange(-Radius, -Mathf.FloorToInt(Planet.Radius)),random.RandiRange(Radius, Mathf.FloorToInt(Planet.Radius))));
			// 		break;
			// 	case 4:
			// 		sphere.TranslateObjectLocal(new Vector3(random.RandiRange(-Radius, -Mathf.FloorToInt(Planet.Radius)),random.RandiRange(Radius, Mathf.FloorToInt(Planet.Radius)),random.RandiRange(Radius, Mathf.FloorToInt(Planet.Radius))));
			// 		break;
			// 	case 5:
			// 		sphere.TranslateObjectLocal(new Vector3(random.RandiRange(Radius, Mathf.FloorToInt(Planet.Radius)),random.RandiRange(-Radius, -Mathf.FloorToInt(Planet.Radius)),random.RandiRange(Radius, Mathf.FloorToInt(Planet.Radius))));
			// 		break;
			// 	case 6:
			// 		sphere.TranslateObjectLocal(new Vector3(random.RandiRange(Radius, Mathf.FloorToInt(Planet.Radius)),random.RandiRange(Radius, Mathf.FloorToInt(Planet.Radius)),random.RandiRange(-Radius, -Mathf.FloorToInt(Planet.Radius))));
			// 		break;
			// 	case 7:
			// 		sphere.TranslateObjectLocal(new Vector3(random.RandiRange(Radius, Mathf.FloorToInt(Planet.Radius)),random.RandiRange(Radius, Mathf.FloorToInt(Planet.Radius)),random.RandiRange(Radius, Mathf.FloorToInt(Planet.Radius))));
			// 		break;
			// }

			var chance = random.Randf();
			if (chance < 0.2f) {
				chance = random.Randf();
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
				
				var offset = random.RandiRange(0, crayons.Length - 1);
				sphere.crayons = new Color[crayons.Length];
				for (var idx = offset; idx < crayons.Length + offset; idx++) {
					sphere.crayons[idx%crayons.Length] = crayons[idx%crayons.Length];
				}

			} else {
				sphere.crayons = new[] { 
					colors[i%colors.Length],
					colors[(i + random.RandiRange(1, 32))%colors.Length]
				};
			}
			bodies.Add(sphere);
			AddChild(sphere);
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);
		bodies.ForEach(body => body.UpdateVelocity(bodies, Transform.Origin, (float)delta));
		bodies.ForEach(body => body.UpdatePosition((float)delta));
		otherSun.Rotation = new Vector3(
			Mathf.Wrap(otherSun.Rotation.X + (float)delta / 4, -Mathf.Pi, Mathf.Pi), 
			Mathf.Wrap(otherSun.Rotation.Y + (float)delta / 8, -Mathf.Pi, Mathf.Pi), 
			Mathf.Wrap(otherSun.Rotation.Z + (float)delta / 20, -Mathf.Pi, Mathf.Pi) 
		);
		// for (int i = 0; i < bodies.Count; i++) {
		// 	bodies[i].Rotation = new Vector3(
		// 			Mathf.Wrap(bodies[i].Rotation.X + (float)delta * _rotate.X, -Mathf.Pi, Mathf.Pi),
		// 			bodies[i].Rotation.Y,
		// 			bodies[i].Rotation.Z
		// 		);		
		// }
		Planet.Rotation = new Vector3(
			Mathf.Wrap(Planet.Rotation.X + (float)delta * _rotate.X / 10, -Mathf.Pi, Mathf.Pi),
			Planet.Rotation.Y,
			Mathf.Wrap(Planet.Rotation.Z + (float)delta * _rotate.Z / 10, -Mathf.Pi, Mathf.Pi)
		);
		for (int i = 0; i < bodies.Count; i++) {
			switch(i%3) {
				case 0:
					bodies[i].RotateObjectLocal(new Vector3(1,0,0), (float)delta * _rotate.X);
					break;
				case 1:
					bodies[i].RotateObjectLocal(new Vector3(1,0,1).Normalized(), (float)delta * _rotate.Y);
					break;
				case 2:
					bodies[i].RotateObjectLocal(new Vector3(0,0,1), (float)delta * _rotate.Z);
					break;
			}
		}
		//PlayerCam.Position = Planet.Transform.Origin + PlayerCam.ToGlobal(Vector3.Up) * (Planet.Shapes.DetermineElevation(PlayerCam.ToGlobal(Vector3.Up)).scaled + 50);
	}

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("jump")) {
			bodies.ForEach(body => body.QueueFree());
			bodies.Clear();
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
    }
}