using Godot;

public partial class Star : OmniLight3D, HeavenlyBody 
{
    public float Mass { get; set; }
    public float Radius { get; set; }
    public float Gravity { get; set; }
    public Vector3 CurrentVelocity { get; set; }
    public bool OutOfBounds { get; set; }

    public Vector3 initialVelocity;

    public override void _Ready()
    {
        Mass = Gravity * Radius * Radius / Universe.Gravity;
        CurrentVelocity = initialVelocity;
    }
}