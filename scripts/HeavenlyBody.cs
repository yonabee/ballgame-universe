using System.Collections.Generic;
using Godot;

public interface HeavenlyBody
{
    public float Mass { get; set; }
    public float Radius { get; set; }
    public float Gravity { get; set; }
    public Vector3 CurrentVelocity { get; set; }
    public Vector3 BaseRotation { get; set; }
    public bool OutOfBounds { get; set; }
    public Transform3D Transform { get; }
    public Vector3 Rotation { get; set; }
    public Vector3 ToGlobal(Vector3 vec);
    public void Translate(Vector3 vec);
    public void RotateObjectLocal(Vector3 vec, float deg);
    public void QueueFree();

    public void UpdateVelocity(List<HeavenlyBody> allBodies, Vector3 universeOrigin, float timeStep)
    {
        var distance = Transform.Origin.DistanceTo(universeOrigin);
        if (distance > Universe.Radius)
        {
            if (!OutOfBounds)
            {
                Universe.OutOfBounds++;
                OutOfBounds = true;
                CurrentVelocity /= 10;
            }
        }
        else
        {
            if (OutOfBounds)
            {
                Universe.OutOfBounds--;
                OutOfBounds = false;
            }
        }
        foreach (var node in allBodies)
        {
            var nodeDistance = node.Transform.Origin.DistanceTo(Transform.Origin);
            if (node != this && (node.Mass * 0.8f) > Mass && nodeDistance < 20 * node.Radius)
            {
                if (nodeDistance > node.Radius)
                {
                    Utils.ApplyBodyToVelocity(this, node, node.Mass, node.Radius, timeStep);
                }
                else
                {
                    Utils.ApplyBodyToVelocity(this, node, node.Mass * 100f, 0, timeStep, true);
                }
            }
        }

        if (OutOfBounds)
        {
            Utils.ApplyBodyToVelocity(this, Universe.Planet, 1000000000, 0, timeStep);
        }
        else if (distance > Radius + (Universe.Planet.Radius * 2f))
        {
            Utils.ApplyBodyToVelocity(
                this,
                Universe.Planet,
                Universe.Planet.Mass,
                Universe.Planet.Radius,
                timeStep
            );
        }
        else
        {
            Utils.ApplyBodyToVelocity(
                this,
                Universe.Planet,
                Universe.Planet.Mass,
                Universe.Planet.Radius,
                timeStep,
                true
            );
        }
    }

    public void UpdatePosition(float timeStep);

    public void GeneratePlanet()
    {
        Initialize();
        GenerateMesh();
    }

    public void Initialize() { }

    public void GenerateMesh() { }
}
