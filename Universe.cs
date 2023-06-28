using Godot;
using System;
using System.Collections.Generic;

public partial class Universe : Node3D
{
	List<Planetoid> bodies = new List<Planetoid>();
	DirectionalLight3D sun = new DirectionalLight3D();

	public override void _Ready() 
	{
		var sphere1 = new Spheroid();
		sphere1.radius = 30;
		sphere1.rings = 100;
		sphere1.radialSegments = 100;
		sphere1.TranslateObjectLocal(new Vector3(-30,0,0));
		bodies.Add(sphere1);

		var sphere2 = new Spheroid();
		sphere2.radius = 20;
		sphere2.color = new Color(Colors.Blue);
		sphere2.TranslateObjectLocal(new Vector3(30,0,0));
		bodies.Add(sphere2);

		var sphere3 = new Spheroid();
		sphere3.radius = 25;
		sphere3.color = Colors.Green;
		sphere3.TranslateObjectLocal(new Vector3(15, 0, 40));
		bodies.Add(sphere3);

		AddChild(sun);
		AddChild(sphere1);
		AddChild(sphere2);
		AddChild(sphere3);
	}

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);
		bodies.ForEach(body => body.UpdateVelocity(bodies, (float)delta));
		bodies.ForEach(body => body.UpdatePosition((float)delta));
		sun.Rotation = new Vector3(
			Mathf.Wrap(sun.Rotation.X + (float)delta, -Mathf.Pi, Mathf.Pi), 
			sun.Rotation.Y, 
			sun.Rotation.Z
		);
		bodies[0].Rotation = new Vector3(
			Mathf.Wrap(bodies[0].Rotation.X + (float)delta, -Mathf.Pi, Mathf.Pi), 
			bodies[0].Rotation.Y, 
			bodies[0].Rotation.Z
		);
		bodies[1].Rotation = new Vector3(
			Mathf.Wrap(bodies[1].Rotation.X + (float)delta * 2, -Mathf.Pi, Mathf.Pi), 
			Mathf.Wrap(bodies[1].Rotation.Y + (float)delta, -Mathf.Pi, Mathf.Pi), 
			bodies[1].Rotation.Z
		);
		bodies[2].Rotation = new Vector3(
			Mathf.Wrap(bodies[2].Rotation.X + (float)delta, -Mathf.Pi, Mathf.Pi),
			Mathf.Wrap(bodies[2].Rotation.Y + (float)delta * 10, -Mathf.Pi, Mathf.Pi),
			Mathf.Wrap(bodies[2].Rotation.Z + (float)delta, -Mathf.Pi, Mathf.Pi)
		);
	}
}