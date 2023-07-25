using Godot;
using System;
using static Utils;

public partial class Pivot : Marker3D
{
	public float Speed = 1f;
	public bool OrientForward = false;
	public Camera3D Camera;

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
		if (Camera == null) {
			return;
		}

		Vector3 cubemap = Utils.SphereToCube(Camera.ToGlobal(Camera.Transform.Origin));
		Face face = Utils.GetFace(cubemap);
		int x = 0;
		int y = 0;
		switch(face) {
			case Face.Top:
				x = Mathf.FloorToInt((cubemap.X + 1f) / 2f * Universe.Planet.Resolution); 
				y = Mathf.FloorToInt((cubemap.Z + 1f) / 2f * Universe.Planet.Resolution);
				GD.Print("Top");
				break;
			case Face.Bottom:
				x = Mathf.FloorToInt((cubemap.Z + 1f) / 2f * Universe.Planet.Resolution); 
				y = Mathf.FloorToInt((cubemap.X + 1f) / 2f * Universe.Planet.Resolution);
				GD.Print("Bottom");
				break;
			case Face.Left:
				x = Mathf.FloorToInt((cubemap.Y + 1f) / 2f * Universe.Planet.Resolution); 
				y = Mathf.FloorToInt((cubemap.Z + 1f) / 2f * Universe.Planet.Resolution);
				GD.Print("Left");
				break;
			case Face.Right:
				x = Mathf.FloorToInt((cubemap.Z + 1f) / 2f * Universe.Planet.Resolution); 
				y = Mathf.FloorToInt((cubemap.Y + 1f) / 2f * Universe.Planet.Resolution);
				GD.Print("Right");
				break;
			case Face.Front:
				x = Mathf.FloorToInt((cubemap.X + 1f) / 2f * Universe.Planet.Resolution); 
				y = Mathf.FloorToInt((cubemap.Y + 1f) / 2f * Universe.Planet.Resolution);
				GD.Print("Front");
				break;
			case Face.Back:
				x = Mathf.FloorToInt((cubemap.Y + 1f) / 2f * Universe.Planet.Resolution); 
				y = Mathf.FloorToInt((cubemap.X + 1f) / 2f * Universe.Planet.Resolution);
				GD.Print("Back");
				break;
		}
		GD.Print("x: " + x + "  y: " + y);
		float offset = (Camera.Transform.Origin.Y - Universe.Planet.TerrainFaces[(int)face - 1].Elevations[x,y].scaled) * (float)delta;
		//GD.Print(offset);
		Camera.TranslateObjectLocal(new Vector3(0, -offset, 0));
	}
}
