using Godot;
using System;
using System.Linq;
using static Utils;

public partial class Pivot : Marker3D
{
	public float Speed = 1f;
	public bool OrientForward = false;
	public Camera3D Camera;
	public Vector2 CameraRotation;

	float _strafeStep = 2000f;
	bool _jumping = false;
	float _jumpImpulse = 0f;
	float _jumpHeight = 0f;
	int _jumpFrames = 0;
	float _jumpDecay = 0.2f;

	public override void _PhysicsProcess(double delta)
	{
		if (Universe.Planet.IsQueuedForDeletion()) 
		{
			return;
		}

		if (Input.IsActionJustPressed("jump")) 
		{
			if (!_jumping) 
			{
				_jumping = true;
				_jumpImpulse = 1000f;
				_jumpFrames = 0;
			}
		}

		if (Input.IsActionPressed("jump"))
		{
			_jumpFrames++;
			Universe.InfoText2.Text = _jumpFrames.ToString();
			if (_jumpFrames < 60) 
			{
				_jumpImpulse += 100f;
			} 
			else 
			{
				_jumpHeight -= Universe.Planet.Gravity * (float)delta * 50f;
			}
		} else { 
			if (_jumpImpulse == 0f && _jumpHeight == 0f) 
			{
				_jumping = false;
				_jumpFrames = 0;
			}
			else 
			{
				_jumpImpulse -= 100f;
				_jumpHeight -= Universe.Planet.Gravity * (float)delta * 100f;
			}
		}

		if (_jumpImpulse < 0f) {
			_jumpImpulse = 0f;
		}
		if (_jumpHeight < 0f) {
			_jumpHeight = 0f;
			_jumpImpulse = 0f;
		}

		if (_jumpImpulse > 0)
		{
			_jumpHeight += _jumpImpulse * (float)delta;
			_jumpImpulse *= 1f - _jumpDecay;
			if (_jumpImpulse < 1f) 
			{
				_jumpImpulse = 0;
			}
		}

		if (Input.IsActionPressed("camera_up")) 
		{
			RotateObjectLocal(Vector3.Left, (float)delta * Speed);
		}

		if (Input.IsActionPressed("camera_down")) 
		{
			RotateObjectLocal(Vector3.Right, (float)delta * Speed);
		}

		if (Input.IsActionPressed("camera_left")) 
		{
			if (Universe.PlayerCam.Current) 
			{
				RotateObjectLocal(Vector3.Back, (float)delta * Speed / (Universe.Planet.Radius / _strafeStep));
			} 
			else 
			{
				RotateObjectLocal(OrientForward ? Vector3.Up : Vector3.Down, (float)delta * (OrientForward ? 1f : Speed));
			}
		}

		if (Input.IsActionPressed("camera_right")) 
		{
			if (Universe.PlayerCam.Current) 
			{
				RotateObjectLocal(Vector3.Forward, (float)delta * Speed / (Universe.Planet.Radius / _strafeStep));
			} 
			else 
			{
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
		var xLength = terrainFace.Elevations.GetLength(0);
		var yLength = terrainFace.Elevations.GetLength(1);
		if (x >=  xLength || x < 0 || y >= yLength || y < 0) {
			return;
		}

		float maxElevation = new float[] {
			terrainFace.Elevations[ x, y ].scaled,
			x > 0 ? terrainFace.Elevations[ x - 1, y ].scaled : 0,
			x > 0 && y > 0 ? terrainFace.Elevations[ x - 1, y - 1 ].scaled : 0,
			y > 0 ? terrainFace.Elevations[ x, y - 1 ].scaled : 0,
			x < xLength - 1 ? terrainFace.Elevations[ x + 1, y ].scaled : 0,
			x < xLength - 1 && y < yLength - 1 ? terrainFace.Elevations[ x + 1, y + 1 ].scaled : 0,
			y < yLength - 1 ? terrainFace.Elevations[ x, y + 1 ].scaled : 0
		}.Max();

		float offset = (
				Camera.Transform.Origin.Y - 
				(Mathf.Max(maxElevation, Universe.Planet.Radius + _jumpHeight) + Universe.CameraFloatHeight)
			) * (float)delta * 10;

		Camera.TranslateObjectLocal(new Vector3(0, -offset, 0));

		//Universe.InfoText2.Text = _jumpHeight + " ft";

		CameraRotation = Vector2.Zero;
	}
}
