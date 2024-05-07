using System;
using System.Linq;
using Godot;
using static Universe;
using static Utils;

public partial class Pivot : Marker3D
{
    public float Speed = 1f;
    public bool OrientForward = false;
    public Camera3D Camera;
    public Vector2 CameraRotation;
    public Vector2 Velocity;
    public bool IsMouse = true;

    float _strafeStep = 1500f;
    bool _jumping = false;
    float _jumpImpulse = 0f;
    float _targetHeight = 0f;
    int _jumpFrames = 0;
    float _jumpDecay = 0.01f;
    bool _falling = false;
    int _fallFrames = 0;

    public override void _Process(double delta)
    {
        if (Planet == null || PlayerPivot == null || CameraArm == null)
        {
            return;
        }
        if (Planet.IsQueuedForDeletion())
        {
            return;
        }
        if (Camera == null)
        {
            return;
        }
        // Figure out where we are so we can find the elevation of that location.
        Vector3 cubemap = Utils.SphereToCube(
            Planet.ToLocal(
                PlayerPivot.ToGlobal(
                    PlayerPivot.Transform.Origin + new Vector3(0f, CameraArm.Transform.Origin.Y, 0f)
                )
            )
        );
        Face face = Utils.GetFace(cubemap);
        int x = 0;
        int y = 0;
        switch (face)
        {
            case Face.Top:
                x = Mathf.FloorToInt((cubemap.X + 1f) / 2f * Planet.Resolution);
                y = Mathf.FloorToInt((1f - ((cubemap.Z + 1f) / 2f)) * Planet.Resolution);
                break;
            case Face.Bottom:
                x = Mathf.FloorToInt((1f - ((cubemap.X + 1f) / 2f)) * Planet.Resolution);
                y = Mathf.FloorToInt((1f - ((cubemap.Z + 1f) / 2f)) * Planet.Resolution);
                break;
            case Face.Left:
                x = Mathf.FloorToInt((1f - ((cubemap.Z + 1f) / 2f)) * Planet.Resolution);
                y = Mathf.FloorToInt((1f - ((cubemap.Y + 1f) / 2f)) * Planet.Resolution);
                break;
            case Face.Right:
                x = Mathf.FloorToInt((cubemap.Z + 1f) / 2f * Planet.Resolution);
                y = Mathf.FloorToInt((1f - ((cubemap.Y + 1f) / 2f)) * Planet.Resolution);
                break;
            case Face.Front:
                x = Mathf.FloorToInt((1f - ((cubemap.Y + 1f) / 2f)) * Planet.Resolution);
                y = Mathf.FloorToInt((1f - ((cubemap.X + 1f) / 2f)) * Planet.Resolution);
                break;
            case Face.Back:
                x = Mathf.FloorToInt((cubemap.Y + 1f) / 2f * Planet.Resolution);
                y = Mathf.FloorToInt((1f - ((cubemap.X + 1f) / 2f)) * Planet.Resolution);
                break;
        }

        TerrainFace terrainFace = Planet.TerrainFaces[(int)face - 1];
        var xLength = terrainFace.Elevations.GetLength(0);
        var yLength = terrainFace.Elevations.GetLength(1);
        if (x >= xLength || x < 0 || y >= yLength || y < 0)
        {
            return;
        }

        CurrentFace = face;
        CurrentLocation = new Vector2(x, y);
        float elevation = terrainFace.Elevations[x, y].scaled;
        CurrentElevation = new float[]
        {
            elevation,
            // x > 0 ? terrainFace.Elevations[x - 1, y].scaled : 0,
            // x > 0 && y > 0 ? terrainFace.Elevations[x - 1, y - 1].scaled : 0,
            // y > 0 ? terrainFace.Elevations[x, y - 1].scaled : 0,
            // x < xLength - 1 ? terrainFace.Elevations[x + 1, y].scaled : 0,
            // x < xLength - 1 && y < yLength - 1 ? terrainFace.Elevations[x + 1, y + 1].scaled : 0,
            // y < yLength - 1 ? terrainFace.Elevations[x, y + 1].scaled : 0,
            x > 3
                ? terrainFace.Elevations[x - 4, y].scaled
                : 0,
            x > 3 && y > 3 ? terrainFace.Elevations[x - 4, y - 4].scaled : 0,
            y > 3 ? terrainFace.Elevations[x, y - 4].scaled : 0,
            x < xLength - 4 ? terrainFace.Elevations[x + 4, y].scaled : 0,
            x < xLength - 4 && y < yLength - 4 ? terrainFace.Elevations[x + 4, y + 4].scaled : 0,
            y < yLength - 4 ? terrainFace.Elevations[x, y + 4].scaled : 0
        }.Max();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Planet.IsQueuedForDeletion())
        {
            return;
        }
        if (Camera == null)
        {
            return;
        }
        if (CurrentElevation == 0f)
        {
            return;
        }

        if (!_jumping)
        {
            _targetHeight = Mathf.Max(CurrentElevation, Planet.Radius);
        }

        // Handle jumping.
        if (Input.IsActionJustPressed("jump"))
        {
            // Start the jump with a large initial impulse and a fresh frame counter
            if (!_jumping)
            {
                _jumping = true;
                _falling = false;
                _jumpImpulse = 50f;
                _jumpFrames = 0;
                _targetHeight = Mathf.Max(CurrentElevation, Planet.Radius);
            }
        }

        if (Input.IsActionPressed("jump"))
        {
            // As jump is held down increase the frame counter and add impulse
            _jumpFrames++;
            if (_jumpFrames < 50)
            {
                _jumpImpulse += 5f;
            }
            else if (_jumpFrames < 300)
            {
                _jumpImpulse += 1.5f;
            }
            // Weak gravity while jump held
            else if (_targetHeight > Planet.Radius && _targetHeight > CurrentElevation)
            {
                _targetHeight -= Planet.Gravity * (float)delta * (_jumpFrames / 10f);
            }
        }
        else
        {
            // Apply full gravity
            if (_jumping && _targetHeight > Planet.Radius && _targetHeight > CurrentElevation)
            {
                _targetHeight -= Planet.Gravity * (float)delta * (150f + _fallFrames * 5);
            }

            if (_falling)
            {
                _fallFrames++;
            }
        }

        if (Input.IsActionJustReleased("jump") && _jumping)
        {
            _falling = true;
            _fallFrames = 0;
        }

        // Apply impulse to height, and then decay
        if (_jumping && _jumpImpulse > 0f)
        {
            _targetHeight += _jumpImpulse * (float)delta * 10f;
            _jumpImpulse *= 1f - _jumpDecay;
        }

        // If we are on the ground the jump is over
        if (_targetHeight <= CurrentElevation || _targetHeight <= Planet.Radius)
        {
            _targetHeight = Mathf.Max(Planet.Radius, CurrentElevation);
            _jumpImpulse = 0f;
            _jumpFrames = 0;
            _jumping = false;
            _falling = false;
            _fallFrames = 0;
        }

        if (Input.IsActionPressed("move_up_key"))
        {
            _CameraUp((float)delta * Speed);
        }

        if (Input.IsActionPressed("move_down_key"))
        {
            _CameraDown((float)delta * Speed);
        }

        if (Input.IsActionPressed("move_left_key"))
        {
            _CameraLeft((float)delta * (!PlayerCam.Current && OrientForward ? 1f : Speed));
        }

        if (Input.IsActionPressed("move_right_key"))
        {
            _CameraRight((float)delta * (!PlayerCam.Current && OrientForward ? 1f : Speed));
        }

        if (Velocity.X > 0f)
        {
            _CameraRight(Velocity.X * (float)delta * Speed);
        }

        if (Velocity.X < 0f)
        {
            _CameraLeft(Mathf.Abs(Velocity.X) * (float)delta * Speed);
        }

        if (Velocity.Y > 0f)
        {
            _CameraDown(Velocity.Y * (float)delta * Speed);
        }

        if (Velocity.Y < 0f)
        {
            _CameraUp(Mathf.Abs(Velocity.Y) * (float)delta * Speed);
        }
        // The pivot rotates for X and the camera rotates for Y
        RotateObjectLocal(
            Vector3.Down,
            CameraRotation.X * (float)delta * Speed * (IsMouse ? 2 : 10)
        );
        Camera.RotateObjectLocal(
            Vector3.Left,
            CameraRotation.Y * (float)delta * Speed * (IsMouse ? 2 : 10)
        );

        if (_targetHeight < Planet.Radius)
        {
            _targetHeight = Planet.Radius;
        }

        float offset =
            (
                CameraArm.Transform.Origin.Y
                - (Mathf.Max(CurrentElevation, _targetHeight) + CameraFloatHeight)
            )
            * (float)delta
            * 10;

        CameraArm.TranslateObjectLocal(new Vector3(0, -offset, 0));
        GUI.Height.Text =
            (_targetHeight - Planet.Radius).ToString("f2") + " meters above sea level";

        if (IsMouse)
        {
            CameraRotation = Vector2.Zero;
        }
    }

    private void _CameraUp(float amount)
    {
        RotateObjectLocal(Vector3.Left, amount);
    }

    private void _CameraDown(float amount)
    {
        RotateObjectLocal(Vector3.Right, amount);
    }

    private void _CameraLeft(float amount)
    {
        if (PlayerCam.Current)
        {
            RotateObjectLocal(Vector3.Back, amount / (Planet.Radius / _strafeStep));
        }
        else
        {
            RotateObjectLocal(OrientForward ? Vector3.Up : Vector3.Down, amount);
        }
    }

    private void _CameraRight(float amount)
    {
        if (PlayerCam.Current)
        {
            RotateObjectLocal(Vector3.Forward, amount / (Planet.Radius / _strafeStep));
        }
        else
        {
            RotateObjectLocal(OrientForward ? Vector3.Down : Vector3.Up, amount);
        }
    }
}
