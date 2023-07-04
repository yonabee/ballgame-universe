using Godot;
using System;
using System.Collections.Generic;

public partial class Universe : Node3D
{
	List<Planetoid> bodies = new List<Planetoid>();
	//DirectionalLight3D sun = new DirectionalLight3D();

	public static int Radius = 1000;

	Vector3 _rotate = Vector3.Zero;

	DirectionalLight3D otherSun;

	Color[] colors = {
		new Color("#5e0086"),
		new Color("#201ec1"),
		new Color("#00b200"),
		new Color("#ffe200"),
		new Color("#ff4600"),
		new Color("#ff001f"),
		new Color("#000000"),
		new Color("#492708"),
		new Color("#00dcfa"), 
		new Color("#da17ff")
	};

	public override void _Ready() 
	{
		var random = new RandomNumberGenerator();

		_rotate.X = random.RandfRange(-1f, 1f);
		_rotate.Y = random.RandfRange(-1f, 1f);
		_rotate.Z = random.RandfRange(-1f, 1f);

		var sun = new OmniLight3D();
		sun.OmniRange = 1000f;
		sun.OmniAttenuation = 1f;
		sun.LightIntensityLumens = 1000;
		AddChild(sun);

		otherSun = new DirectionalLight3D();
		otherSun.LightIntensityLumens = 10;
		AddChild(otherSun);

		int sphereCount = 100;

		for (int i = 0; i < sphereCount; i++) {
			var sphere = new Spheroid();
			sphere.id = i;
			sphere.radius = random.RandiRange(10, 500);
			sphere.rings = Mathf.FloorToInt(sphere.radius);
			sphere.radialSegments = sphere.rings;
			sphere.Mass = sphere.radius * 1000;
			switch(i%8) {
				case 0:
					sphere.TranslateObjectLocal(new Vector3(random.RandiRange(-Radius, 0),random.RandiRange(-Radius, 0),random.RandiRange(-Radius, 0)));
					break;
				case 1:
					sphere.TranslateObjectLocal(new Vector3(random.RandiRange(Radius, 0),random.RandiRange(-Radius, 0),random.RandiRange(-Radius, 0)));
					break;
				case 2:
					sphere.TranslateObjectLocal(new Vector3(random.RandiRange(-Radius, 0),random.RandiRange(Radius, 0),random.RandiRange(-Radius, 0)));
					break;
				case 3:
					sphere.TranslateObjectLocal(new Vector3(random.RandiRange(-Radius, 0),random.RandiRange(-Radius, 0),random.RandiRange(Radius, 0)));
					break;
				case 4:
					sphere.TranslateObjectLocal(new Vector3(random.RandiRange(-Radius, 0),random.RandiRange(Radius, 0),random.RandiRange(Radius, 0)));
					break;
				case 5:
					sphere.TranslateObjectLocal(new Vector3(random.RandiRange(Radius, 0),random.RandiRange(-Radius, 0),random.RandiRange(Radius, 0)));
					break;
				case 6:
					sphere.TranslateObjectLocal(new Vector3(random.RandiRange(Radius, 0),random.RandiRange(Radius, 0),random.RandiRange(-Radius, 0)));
					break;
				case 7:
					sphere.TranslateObjectLocal(new Vector3(random.RandiRange(Radius, 0),random.RandiRange(Radius, 0),random.RandiRange(Radius, 0)));
					break;
			}

			var chance = random.Randf();
			if (chance < 0.2f) {
				chance = random.Randf();

				// classic rainbow
				if (chance < 0.15f) {
					sphere.crayons = new[] {
						new Color("#E50000"),
						new Color("#FF8D00"),
						new Color("#FFEE00"),
						new Color("#028121"),
						new Color("#004CFF"),
						new Color("#770088")
					};
				
				// progress rainbow
				} else if (chance < 0.3f) {
					sphere.crayons = new[] {
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
					sphere.crayons = new[] {
						new Color("#5BCFFB"),
						new Color("#F5ABB9"),
						new Color("#FFFFFF"),
						new Color("#F5ABB9"),
						new Color("#5BCFFB")
					};

				// lesbian
				} else if (chance < 0.6f) {
					sphere.crayons = new[] {
						new Color("#D62800"),
						new Color("#FF9B56"),
						new Color("#FFFFFF"),
						new Color("#D462A6"),
						new Color("#A40062"),
					};
				
				// bisexual
				} else if (chance < 0.7f) {
					sphere.crayons = new[] {
						new Color("#D60270"),
						new Color("#9B4F96"),
						new Color("#0038A8")
					};

				// pansexual
				} else if (chance < 0.8f) {
					sphere.crayons = new[] {
						new Color("#FF1C8D"),
						new Color("#FFD700"),
						new Color("#1AB3FF")
					};

				// nonbinary
				} else if (chance < 0.9f) {
					sphere.crayons = new[] {
						new Color("#FCF431"),
						new Color("#FCFCFC"),
						new Color("#9D59D2"),
						new Color("#282828")
					};
				
				// genderfluid
				} else {
					sphere.crayons = new[] {
						new Color("#FE76A2"),
						new Color("#FFFFFF"),
						new Color("#BF12D7"),
						new Color("#000000"),
						new Color("#303CBE")
					};
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
		bodies.ForEach(body => body.UpdateVelocity(bodies, (float)delta));
		bodies.ForEach(body => body.UpdatePosition((float)delta));
		otherSun.Rotation = new Vector3(
			Mathf.Wrap(otherSun.Rotation.X + (float)delta / 4, -Mathf.Pi, Mathf.Pi), 
			Mathf.Wrap(otherSun.Rotation.Y + (float)delta / 8, -Mathf.Pi, Mathf.Pi), 
			Mathf.Wrap(otherSun.Rotation.Z + (float)delta / 20, -Mathf.Pi, Mathf.Pi) 
		);
		// for (int i = 0; i < bodies.Count; i++) {
		// 	bodies[i].Rotation = new Vector3(
		// 		Mathf.Wrap(bodies[i].Rotation.X + (float)delta * _rotate.X, -Mathf.Pi, Mathf.Pi),
		// 		Mathf.Wrap(bodies[i].Rotation.Y + (float)delta * _rotate.Y, -Mathf.Pi, Mathf.Pi),
		// 		Mathf.Wrap(bodies[i].Rotation.Z + (float)delta * _rotate.Z, -Mathf.Pi, Mathf.Pi)
		// 	);
		// }
		for (int i = 0; i < bodies.Count; i++) {
			switch(i%4) {
				case 0:
					bodies[i].Rotation = new Vector3(
						Mathf.Wrap(bodies[i].Rotation.X + (float)delta * _rotate.X, -Mathf.Pi, Mathf.Pi),
						bodies[i].Rotation.Y,
						bodies[i].Rotation.Z
					);
					break;
				case 1:
					bodies[i].Rotation = new Vector3(
						Mathf.Wrap(bodies[i].Rotation.X + (float)delta * _rotate.X, -Mathf.Pi, Mathf.Pi),
						Mathf.Wrap(bodies[i].Rotation.Y + (float)delta * _rotate.Y, -Mathf.Pi, Mathf.Pi),
						bodies[i].Rotation.Z
					);
					break;
				case 2:
					bodies[i].Rotation = new Vector3(
						Mathf.Wrap(bodies[i].Rotation.X + (float)delta * _rotate.X, -Mathf.Pi, Mathf.Pi),
						Mathf.Wrap(bodies[i].Rotation.Y + (float)delta * _rotate.Y, -Mathf.Pi, Mathf.Pi),
						Mathf.Wrap(bodies[i].Rotation.Z + (float)delta * _rotate.Z, -Mathf.Pi, Mathf.Pi)
					);
					break;
				case 3:
					bodies[i].Rotation = new Vector3(
						bodies[i].Rotation.X,
						Mathf.Wrap(bodies[i].Rotation.Y + (float)delta * _rotate.Y, -Mathf.Pi, Mathf.Pi),
						bodies[i].Rotation.Z
					);
					break;
			}
		}
	}

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("jump")) {
			bodies.ForEach(body => body.QueueFree());
			bodies.Clear();
			_Ready();
		}
    }
}