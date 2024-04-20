using Godot;
using System;
using System.Collections.Generic;

public partial class Planetoid : RigidBody3D, HeavenlyBody
{
    public Vector3 initialVelocity;
    public float Radius { get; set; }
    public int Faces { get; set; }
    public int Layers { get; set; }
    public Vector3 CurrentVelocity { get; set; }
    public RandomNumberGenerator Random;
    public int Seed { get; set; }
    public float Gravity { get; set; }
    public bool OutOfBounds { get; set; }
    public Vector3 BaseRotation { get; set; }

    public override void _Ready()
    {
        Configure();
        GeneratePlanet();
    }

    public override void _IntegrateForces(PhysicsDirectBodyState3D state)
    {
        state.LinearVelocity = CurrentVelocity;
        state.AngularVelocity = BaseRotation;
    }

    public void UpdatePosition(float timeStep)
    {
        // Do nothing and let _IntegrateForces handle it.
    }

    public virtual void Configure()
    {
        Faces = 1;
        Layers = 1;
        CustomIntegrator = true;
        CurrentVelocity = initialVelocity;
        Mass = Gravity * Radius * Radius / Universe.Gravity * 10000;
        Random = Universe.Random;
    }

    public void GeneratePlanet()
    {
        Initialize();
        GenerateMesh();
    }

    public virtual void Initialize() {
    }

    public virtual void GenerateMesh() {}
}