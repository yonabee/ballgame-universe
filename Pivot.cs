using Godot;
using System;

public partial class Pivot : Marker3D
{
	public override void _PhysicsProcess(double delta)
	{
		if (Input.IsActionPressed("camera_right")) {
			RotateObjectLocal(Vector3.Up, (float)delta);
		}
		if (Input.IsActionPressed("camera_left")) {
			RotateObjectLocal(Vector3.Down, (float)delta);
		}
		if (Input.IsActionPressed("camera_up")) {
			RotateObjectLocal(Vector3.Left, (float)delta);
		}
		if (Input.IsActionPressed("camera_down")) {
			RotateObjectLocal(Vector3.Right, (float)delta);
		}
	}
}
