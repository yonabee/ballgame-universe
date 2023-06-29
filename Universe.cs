using Godot;
using System;
using System.Collections.Generic;

public partial class Universe : Node3D
{
	List<Planetoid> bodies = new List<Planetoid>();
	DirectionalLight3D sun = new DirectionalLight3D();

	public static int Radius = 500;

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

		var sun = new OmniLight3D();
		sun.OmniRange = 100000;
		sun.OmniAttenuation = 0.1f;
		sun.LightIntensityLumens = 100000;
		AddChild(sun);

		int sphereCount = random.RandiRange(75,125);

		for (int i = 0; i < sphereCount; i++) {
			var sphere = new Spheroid();
			sphere.radius = random.RandiRange(10, 250);
			sphere.rings = sphere.radius * 3;
			sphere.radialSegments = sphere.rings;
			sphere.mass = sphere.radius * 100;
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
			sphere.color = colors[i%colors.Length];
			bodies.Add(sphere);
			AddChild(sphere);
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);
			bodies.ForEach(body => body.UpdateVelocity(bodies, (float)delta));
			bodies.ForEach(body => body.UpdatePosition((float)delta));
		//sun.Rotation = new Vector3(
		//	Mathf.Wrap(sun.Rotation.X + (float)delta, -Mathf.Pi, Mathf.Pi), 
		//	sun.Rotation.Y, 
		//	sun.Rotation.Z
		//);
		for (int i = 0; i < bodies.Count; i++) {
			switch(i%4) {
				case 0:
					bodies[i].Rotation = new Vector3(
						Mathf.Wrap(bodies[i].Rotation.X + (float)delta, -Mathf.Pi, Mathf.Pi),
						bodies[i].Rotation.Y,
						bodies[i].Rotation.Z
					);
					break;
				case 1:
					bodies[i].Rotation = new Vector3(
						Mathf.Wrap(bodies[i].Rotation.X + (float)delta, -Mathf.Pi, Mathf.Pi),
						Mathf.Wrap(bodies[i].Rotation.Y + (float)delta * 2, -Mathf.Pi, Mathf.Pi),
						bodies[i].Rotation.Z
					);
					break;
				case 2:
					bodies[i].Rotation = new Vector3(
						Mathf.Wrap(bodies[i].Rotation.X + (float)delta, -Mathf.Pi, Mathf.Pi),
						Mathf.Wrap(bodies[i].Rotation.Y + (float)delta * 3, -Mathf.Pi, Mathf.Pi),
						bodies[i].Rotation.Z
					);
					break;
				case 3:
					bodies[i].Rotation = new Vector3(
						bodies[i].Rotation.X,
						Mathf.Wrap(bodies[i].Rotation.Y + (float)delta, -Mathf.Pi, Mathf.Pi),
						bodies[i].Rotation.Z
					);
					break;
			}
		}
	}
}