using Godot;

public interface CelestialObject
{
    public float Radius { get; set; }
    public float Gravity { get; set; }
    public float Mass { get; set; }
    public Vector3 CurrentRotation { get; set; }
    public Vector3 CurrentVelocity { get; set; }
    public Transform3D Transform { get; }
}
