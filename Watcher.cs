using Godot;
using System;

public partial class Watcher : Camera3D
{
	public override void _PhysicsProcess(double delta)
	{
		if (Input.IsActionPressed("camera_in") && Transform.Origin.Z > Universe.Planet.Radius) {
			TranslateObjectLocal(new Vector3(0, 0, -(float)delta * 1000f));
		}
		if (Input.IsActionPressed("camera_out") && Transform.Origin.Z < Universe.Radius * 5) {
			TranslateObjectLocal(new Vector3(0, 0, (float)delta * 1000f));
		}
	}
}
