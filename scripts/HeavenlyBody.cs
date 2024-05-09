using System.Collections.Generic;
using Godot;

public interface HeavenlyBody : CelestialObject
{
    public bool OutOfBounds { get; set; }
    public Vector3 Rotation { get; set; }
    public Vector3 ToGlobal(Vector3 vec);
    public Vector3 ToLocal(Vector3 vec);
    public void Translate(Vector3 vec);
    public void RotateObjectLocal(Vector3 vec, float deg);
    public void QueueFree();
    public void UpdateVelocity(
        List<HeavenlyBody> allBodies,
        Vector3 universeOrigin,
        float timeStep
    );
    public void UpdatePosition(float timeStep);
    public void Initialize();
    public void GenerateMesh();
}
