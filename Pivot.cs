using Godot;
using System;

public partial class Pivot : Marker3D
{
	public override void _PhysicsProcess(double delta)
	{
		if (Input.IsActionPressed("camera_up")) {
			Rotation = new Vector3(
				Mathf.Wrap(Rotation.X + (float)delta * -1f, -Mathf.Pi, Mathf.Pi),
				Rotation.Y,
				Rotation.Z
			);
		}
		if (Input.IsActionPressed("camera_down")) {
			Rotation = new Vector3(
				Mathf.Wrap(Rotation.X + (float)delta * 1f, -Mathf.Pi, Mathf.Pi),
				Rotation.Y,
				Rotation.Z
			);
		}
		if (Input.IsActionPressed("camera_left")) {
			Rotation = new Vector3(
				Rotation.X,
				Mathf.Wrap(Rotation.Y + (float)delta * -1f, -Mathf.Pi, Mathf.Pi),
				Rotation.Z
			);
		}
		if (Input.IsActionPressed("camera_right")) {
			Rotation = new Vector3(
				Rotation.X,
				Mathf.Wrap(Rotation.Y + (float)delta * 1f, -Mathf.Pi, Mathf.Pi),
				Rotation.Z
			);
		}
		GD.Print(Rotation);
	}
}
