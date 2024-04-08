using Godot;
using System;
using static Utils;

public partial class Pivot : Marker3D
{
	public float Speed = 1f;
	public bool OrientForward = false;
	public Camera3D Camera;

	public Vector2 CameraRotation;

	public override void _PhysicsProcess(double delta)
	{
		if (Universe.Planet.IsQueuedForDeletion()) {
			return;
		}
		if (Input.IsActionPressed("camera_up")) {
			RotateObjectLocal(Vector3.Left, (float)delta * Speed);
		}
		if (Input.IsActionPressed("camera_down")) {
			RotateObjectLocal(Vector3.Right, (float)delta * Speed);
		}
		if (Input.IsActionPressed("camera_left")) {
			if (Universe.PlayerCam.Current) {
				RotateObjectLocal(Vector3.Back, (float)delta * Speed / (Universe.Planet.Radius / 1000f));
			} else {
				RotateObjectLocal(OrientForward ? Vector3.Up : Vector3.Down, (float)delta * (OrientForward ? 1f : Speed));
			}
		}
		if (Input.IsActionPressed("camera_right")) {
			if (Universe.PlayerCam.Current) {
				RotateObjectLocal(Vector3.Forward, (float)delta * Speed / (Universe.Planet.Radius / 1000f));
			} else {
				RotateObjectLocal(OrientForward ? Vector3.Down : Vector3.Up, (float)delta * (OrientForward ? 1f : Speed));
			}
		}
		RotateObjectLocal(Vector3.Down, CameraRotation.X * (float)delta * Speed * 2);

		if (Camera == null) {
			return;
		}

		Camera.RotateObjectLocal(Vector3.Left, CameraRotation.Y * (float)delta * Speed * 2);

		Vector3 cubemap = Utils.SphereToCube(
			Universe.Planet.ToLocal(
				Universe.PlayerPivot.ToGlobal(
					Universe.PlayerPivot.Transform.Origin 
						+ new Vector3(0f,Camera.Transform.Origin.Y, 0f)
				)
			)
		);
		Face face = Utils.GetFace(cubemap);
		int x = 0;
		int y = 0;
		switch(face) {
			case Face.Top:
				x = Mathf.FloorToInt((cubemap.X + 1f) / 2f * Universe.Planet.Resolution); 
				y = Mathf.FloorToInt((1f - ((cubemap.Z + 1f) / 2f)) * Universe.Planet.Resolution);
				break;
			case Face.Bottom:
				x = Mathf.FloorToInt((1f - ((cubemap.X + 1f) / 2f)) * Universe.Planet.Resolution); 
				y = Mathf.FloorToInt((1f - ((cubemap.Z + 1f) / 2f)) * Universe.Planet.Resolution);
				break;
			case Face.Left:
				x = Mathf.FloorToInt((1f - ((cubemap.Z + 1f) / 2f)) * Universe.Planet.Resolution); 
				y = Mathf.FloorToInt((1f - ((cubemap.Y + 1f) / 2f)) * Universe.Planet.Resolution);
				break;
			case Face.Right:
				x = Mathf.FloorToInt((cubemap.Z + 1f) / 2f * Universe.Planet.Resolution); 
				y = Mathf.FloorToInt((1f - ((cubemap.Y + 1f) / 2f)) * Universe.Planet.Resolution);
				break;
			case Face.Front:
				x = Mathf.FloorToInt((1f- ((cubemap.Y + 1f) / 2f)) * Universe.Planet.Resolution); 
				y = Mathf.FloorToInt((1f - ((cubemap.X + 1f) / 2f)) * Universe.Planet.Resolution);
				break;
			case Face.Back:
				x = Mathf.FloorToInt((cubemap.Y + 1f) / 2f * Universe.Planet.Resolution); 
				y = Mathf.FloorToInt((1f - ((cubemap.X + 1f) / 2f)) * Universe.Planet.Resolution);
				break;
		}
		Universe.CurrentFace = face;
		Universe.Location = new Vector2(x,y);

		TerrainFace terrainFace = Universe.Planet.TerrainFaces[(int)face - 1];
		if (x >= terrainFace.Elevations.GetLength(0) || x < 0 || y >= terrainFace.Elevations.GetLength(1) || y < 0) {
			return;
		}
		float elevation = terrainFace.Elevations[x,y].scaled;
		float offset = (
				Camera.Transform.Origin.Y - 
				(Mathf.Max(elevation, Universe.Planet.Radius) + 100f)
			) * (float)delta * 10;

		Camera.TranslateObjectLocal(new Vector3(0, -offset, 0));

		CameraRotation = Vector2.Zero;
	}
}
