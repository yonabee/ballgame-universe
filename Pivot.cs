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

	float _strafeStep = 1500f;
	bool _jumping = false;
	float _jumpImpulse = 0f;
	float _targetHeight = 0f;
	int _jumpFrames = 0;
	float _jumpDecay = 0.01f;

    public override void _PhysicsProcess(double delta)
	{
		if (Universe.Planet.IsQueuedForDeletion()) 
		{
			return;
		}
		
		if (Camera == null) {
			return;
		}
		
		// Figure out where we are so we can find the elevation of that location.
		Vector3 cubemap = Utils.SphereToCube(
			Universe.Planet.ToLocal(
				Universe.PlayerPivot.ToGlobal(
					Universe.PlayerPivot.Transform.Origin 
						+ new Vector3(0f,Universe.CameraArm.Transform.Origin.Y, 0f)
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

		float elevation = terrainFace.Elevations[ x, y ].scaled;
		float maxElevation = new float[] {
			elevation,
			x > 0 ? terrainFace.Elevations[ x - 1, y ].scaled : 0,
			x > 0 && y > 0 ? terrainFace.Elevations[ x - 1, y - 1 ].scaled : 0,
			y > 0 ? terrainFace.Elevations[ x, y - 1 ].scaled : 0,
			x < xLength - 1 ? terrainFace.Elevations[ x + 1, y ].scaled : 0,
			x < xLength - 1 && y < yLength - 1 ? terrainFace.Elevations[ x + 1, y + 1 ].scaled : 0,
			y < yLength - 1 ? terrainFace.Elevations[ x, y + 1 ].scaled : 0
		}.Max();

		if (!_jumping) 
		{
			_targetHeight = Mathf.Max(maxElevation, Universe.Planet.Radius);
		}

		// Handle jumping.
		if (Input.IsActionJustPressed("jump")) 
		{
			// Start the jump with a large initial impulse and a fresh frame counter
			if (!_jumping) 
			{
				_jumping = true;
				_jumpImpulse = 50f;
				_jumpFrames = 0;
				_targetHeight = Mathf.Max(maxElevation, Universe.Planet.Radius);
			}
		}

		if (Input.IsActionPressed("jump"))
		{
			// As jump is held down increase the frame counter and add impulse
			_jumpFrames++;
			if (_jumpFrames < 500) 
			{
				_jumpImpulse += 1f;
			}
			// Weak gravity while jump held 
			else if (_targetHeight > Universe.Planet.Radius && _targetHeight > maxElevation ) {
				_targetHeight -= Universe.Planet.Gravity * (float)delta * (_jumpFrames / 2f);
			}
		} else {
			// Apply full gravity
			if (_jumping &&  _targetHeight > Universe.Planet.Radius && _targetHeight > maxElevation ) {
				_targetHeight -= Universe.Planet.Gravity * (float)delta * (100f + _jumpFrames);
			}
		} 

		// Apply impulse to height, and then decay
		if (_jumping && _jumpImpulse > 0f )
		{
			_targetHeight += _jumpImpulse * (float)delta * 10f;
			_jumpImpulse *= 1f - _jumpDecay;
		}

		// If we are on the ground the jump is over
		if ( _targetHeight <= maxElevation || _targetHeight <= Universe.Planet.Radius ) 
		{
			_targetHeight = Mathf.Max( Universe.Planet.Radius, maxElevation );
			_jumpImpulse = 0f;
			_jumpFrames = 0;
			_jumping = false;
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

		// The pivot rotates for X and the camera rotates for Y
		RotateObjectLocal(Vector3.Down, CameraRotation.X * (float)delta * Speed * 2);
		Camera.RotateObjectLocal(Vector3.Left, CameraRotation.Y * (float)delta * Speed * 2);

		if (_targetHeight < Universe.Planet.Radius) {
			_targetHeight = Universe.Planet.Radius;
		}

		float offset = (
				Universe.CameraArm.Transform.Origin.Y - 
				(Mathf.Max(maxElevation, _targetHeight) + Universe.CameraFloatHeight)
			) * (float)delta * 10;

		Universe.CameraArm.TranslateObjectLocal(new Vector3(0, -offset, 0));

		// Universe.InfoText2.Text = 
		// 	_jumpImpulse.ToString("f2") +
		// 	(_jumping ? " jump  and " : " hover and ") +
		// 	((_targetHeight > maxElevation && _targetHeight > Universe.Planet.Radius) ? "jumping  at " : "hovering at ") 
		// 	+ (_targetHeight - Universe.Planet.Radius).ToString("f2") 
		// 	+ " ft";

		CameraRotation = Vector2.Zero;
	}
}
