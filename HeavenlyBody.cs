using Godot;
using System.Collections.Generic;

public interface HeavenlyBody
{
    public float Mass { get; set; }
    public float Radius { get; set; }
    public float Gravity { get; set; }
    public Vector3 CurrentVelocity { get; set; }
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
        if (distance > Universe.Radius) {
            if (!OutOfBounds) {
                Universe.OutOfBounds++;
                OutOfBounds = true;
                CurrentVelocity /= 10;
            }
        } else {
            if (OutOfBounds) {
                Universe.OutOfBounds--;
                OutOfBounds = false;
            }
        } 
        foreach(var node in allBodies) 
        {
            if (node != this && (node.Mass * 0.8f) > Mass && node.Transform.Origin.DistanceTo(Transform.Origin) < 20 * node.Radius)
            {
                _ApplyBodyToVelocity(node.ToGlobal(node.Transform.Origin), node.Mass, node.Radius, timeStep);
            }
        }

        if (OutOfBounds) {
           _ApplyBodyToVelocity(Universe.Planet.Transform.Origin, 1000000000, 0, timeStep);
        }
    }

    public void UpdatePosition(float timeStep) 
    {
        Translate(CurrentVelocity * timeStep);
    }

    void _ApplyBodyToVelocity(Vector3 origin, float bodyMass, float bodyRadius, float timeStep) 
    {
        Vector3 distance = origin - Transform.Origin;
        float sqrDist = distance.LengthSquared();
        Vector3 forceDir = distance.Normalized();
        Vector3 force = forceDir * Gravity * bodyMass / sqrDist;
        Vector3 acceleration = force.Normalized();
        if (!Mathf.IsNaN(acceleration.Length())) {
            if (bodyRadius == 0) { 
                CurrentVelocity += acceleration * timeStep * 10f;
            } else {
                CurrentVelocity += acceleration * timeStep;
            }
        }
    }
}