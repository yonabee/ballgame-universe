using Godot;
using System;
using System.Collections.Generic;

public partial class Universe : Node3D
{
	List<Planetoid> bodies = new List<Planetoid>();

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
		sphere2.TranslateObjectLocal(new Vector3(30,0,0));
		bodies.Add(sphere2);

		AddChild(sphere1);
		AddChild(sphere2);
	}

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);
		bodies.ForEach(body => body.UpdateVelocity(bodies, (float)delta));
		bodies.ForEach(body => body.UpdatePosition((float)delta));
	}
}
