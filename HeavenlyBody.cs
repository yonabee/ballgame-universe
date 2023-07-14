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

    public void UpdateVelocity(List<HeavenlyBody> allBodies, Vector3 origin, float timeStep) 
    {
        var distance = Mathf.Abs((origin - Transform.Origin).Length());
        if (distance < Universe.Radius * 2) {
            foreach(var node in allBodies) 
            {
                if (node != this)
                {
                    _ApplyVelocity(node.ToGlobal(node.Transform.Origin), node.Mass, node.Radius, timeStep);
                }
            }
        }

        // sun
        _ApplyVelocity(origin, 1000000000, 0, timeStep);
    }

    public void UpdatePosition(float timeStep) 
    {
        Translate(CurrentVelocity * timeStep);
    }

    void _ApplyVelocity(Vector3 origin, float bodyMass, float bodyRadius, float timeStep) 
    {
        Vector3 distance = origin - Transform.Origin;
        if (bodyRadius == 0) {
            if (Mathf.Abs(distance.Length()) > Universe.Radius) {
                if (!OutOfBounds) {
                    CurrentVelocity = CurrentVelocity / 2;
                    OutOfBounds = true;
                }
                distance = distance.Normalized() * Universe.Radius;
            } else {
                OutOfBounds = false;
            } 
        }

        float sqrDist = distance.LengthSquared();
        Vector3 forceDir = distance.Normalized();
        Vector3 force = forceDir * Gravity * Mass * bodyMass / sqrDist;
        Vector3 acceleration = (force / Mass).Normalized();
        if (!Mathf.IsNaN(acceleration.Length())) {
            if (bodyRadius != 0 && distance.Length() > this.Radius + bodyRadius) {
                CurrentVelocity += acceleration * timeStep;
            } else {
                if (bodyRadius > 0) {
                   CurrentVelocity += -acceleration * timeStep * 10;
                } else {
                    CurrentVelocity += acceleration * timeStep;
                }
            }
        }
    }
}