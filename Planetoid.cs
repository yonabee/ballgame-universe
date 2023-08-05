using Godot;
using System;
using System.Collections.Generic;

public partial class Planetoid : RigidBody3D, HeavenlyBody
{
    public Vector3 initialVelocity;
    public float Radius { get; set; }
    public int Faces { get; set; }
    public int Layers { get; set; }
    public MeshInstance3D[] Meshes;
    public CollisionShape3D[] Colliders;
    public Vector3 CurrentVelocity { get; set; }
    public RandomNumberGenerator Random;
    public int Seed { get; set; }
    public float Gravity { get; set; }
    public bool OutOfBounds { get; set; }

    public override void _Ready()
    {
        Configure();
        GeneratePlanet();
    }

    public virtual void Configure()
    {
        Faces = 1;
        Layers = 1;
        CustomIntegrator = true;
        CurrentVelocity = initialVelocity;
        Mass = Gravity * Radius * Radius / Universe.Gravity;
        Random = new RandomNumberGenerator();
        Random.Seed = (ulong)Seed;
    }

    public void GeneratePlanet()
    {
        Initialize();
        GenerateMesh();
    }

    public virtual void Initialize() {
		Meshes = new MeshInstance3D[Faces * Layers];
        Colliders = new CollisionShape3D[Faces * Layers];
        for (int i = 0; i < Faces * Layers; i++) {
            Meshes[i] = new MeshInstance3D();
            Colliders[i] = new CollisionShape3D();
            AddChild(Meshes[i]);
            AddChild(Colliders[i]);
        }
    }

    public virtual void GenerateMesh() {}
}