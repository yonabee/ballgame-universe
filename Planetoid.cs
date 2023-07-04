using Godot;
using System;
using System.Collections.Generic;

public partial class Planetoid : RigidBody3D
{
    public Vector3 initialVelocity;
    public float radius = 1;
    public Color[] crayons = { Colors.Red };
    public MeshInstance3D meshInstance;
    public Vector3 currentVelocity;
    public int id = 0;

    float _gravity;

    bool _outOfBounds = false;

    public override void _Ready()
    {
        _gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
        CustomIntegrator = true;
        currentVelocity = initialVelocity;
        GeneratePlanet();
    }

    public void GeneratePlanet()
    {
        Initialize();
        GenerateColors();
        GenerateMesh();
    }

    public virtual void Initialize() {
		meshInstance = new MeshInstance3D();
		AddChild(meshInstance);
    }

    public virtual void GenerateMesh() {}

    public virtual void GenerateColors() {}

    public void UpdateVelocity(List<Planetoid> allBodies, float timeStep) 
    {
        var universeOrigin = GetParent<Node3D>().Transform.Origin;
        var distance = Math.Abs((universeOrigin - Transform.Origin).Length());
        if (distance < Universe.Radius * 2) {
            foreach(var otherBody in allBodies) 
            {
                if (otherBody != this)
                {
                    _ApplyVelocity(otherBody.ToGlobal(otherBody.Transform.Origin), otherBody.Mass, otherBody.radius, timeStep);
                }
            }
        }

        // sun
        _ApplyVelocity(universeOrigin, 10000000, 0, timeStep);
    }

    public void UpdatePosition(float timeStep) 
    {
        TranslateObjectLocal(currentVelocity * timeStep);
    }

    void _ApplyVelocity(Vector3 origin, float bodyMass, float bodyRadius, float timeStep) 
    {
        Vector3 distance = origin - Transform.Origin;
        if (bodyRadius == 0) {
            if (Math.Abs(distance.Length()) > Universe.Radius) {
                if (!_outOfBounds) {
                    currentVelocity = currentVelocity / 2;
                    _outOfBounds = true;
                }
                distance = distance.Normalized() * Universe.Radius;
            } else {
                _outOfBounds = false;
            } 
        }

        float sqrDist = distance.LengthSquared();
        Vector3 forceDir = distance.Normalized();
        Vector3 force = forceDir * _gravity * Mass * bodyMass / sqrDist;
        Vector3 acceleration = (force / Mass).Normalized();
        if (!Mathf.IsNaN(acceleration.Length())) {
            if (bodyRadius != 0 && distance.Length() > this.radius + bodyRadius) {
                currentVelocity += acceleration * timeStep;
            } else {
                if (bodyRadius > 0) {
                   currentVelocity += -acceleration * timeStep * 100;
                } else {
                    currentVelocity += acceleration * timeStep;
                }
            }
        }

    }
}