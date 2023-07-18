using Godot;
using System;

public partial class Pivot : Marker3D
{
	public float Speed = 1f;
	public bool OrientForward = false;
	public override void _PhysicsProcess(double delta)
	{
		if (Input.IsActionPressed("camera_right")) {
			RotateObjectLocal(OrientForward ? Vector3.Down : Vector3.Up, (float)delta * (OrientForward ? 1f : Speed));
		}
		if (Input.IsActionPressed("camera_left")) {
			RotateObjectLocal(OrientForward ? Vector3.Up : Vector3.Down, (float)delta * (OrientForward ? 1f : Speed));
		}
		if (Input.IsActionPressed("camera_up")) {
			RotateObjectLocal(Vector3.Left, (float)delta * Speed);
		}
		if (Input.IsActionPressed("camera_down")) {
			RotateObjectLocal(Vector3.Right, (float)delta * Speed);
		}
		if (Input.IsActionPressed("camera_strafe_left")) {
			RotateObjectLocal(Vector3.Back, (float)delta * Speed);
		}
		if (Input.IsActionPressed("camera_strafe_right")) {
			RotateObjectLocal(Vector3.Forward, (float)delta * Speed);
		}
	}
}
