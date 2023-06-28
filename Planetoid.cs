using Godot;
using System;
using System.Collections.Generic;

public partial class Planetoid : Node3D
{
    public Vector3 initialVelocity;
    public int radius = 1;
    public int mass = 100;
    public Color color = Colors.Red;
    public MeshInstance3D meshInstance;
    public Vector3 currentVelocity;

    float _gravity;

    public override void _Ready()
    {
        _gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
        currentVelocity = initialVelocity;
        GeneratePlanet();
    }

    public override void _Process(double delta)
    {
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
        foreach(var otherBody in allBodies) 
        {
            if (otherBody != this)
            {
                Vector3 distance = otherBody.Transform.Origin - Transform.Origin;
                float sqrDist = distance.LengthSquared();
                Vector3 forceDir = distance.Normalized();
                Vector3 force = forceDir * _gravity * mass * otherBody.mass / sqrDist;
                Vector3 acceleration = (force / mass).Normalized();
                currentVelocity += acceleration * timeStep;
            }
        }
    }

    public void UpdatePosition(float timeStep) 
    {
        TranslateObjectLocal(currentVelocity * timeStep);
    }
}