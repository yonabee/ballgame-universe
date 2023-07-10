using Godot;
using System;
using System.Collections.Generic;

public partial class Planetoid : RigidBody3D, HeavenlyBody
{
    public Vector3 initialVelocity;
    public float Radius { get; set; }
    public int Faces { get; set; }
    public int Layers { get; set; }
    public Color[] crayons = { Colors.Red };
    public MeshInstance3D[] meshes;
    public CollisionShape3D[] colliders;
    public Vector3 CurrentVelocity { get; set; }
    public int Id { get; set; }
    public float Gravity { get; set; }
    public bool OutOfBounds { get; set; }

    public override void _Ready()
    {
        Faces = 1;
        Layers = 1;
        Gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
        CustomIntegrator = true;
        CurrentVelocity = initialVelocity;
        GeneratePlanet();
    }

    public void GeneratePlanet()
    {
        Initialize();
        GenerateMesh();
    }

    public virtual void Initialize() {
		meshes = new MeshInstance3D[Faces * Layers];
        colliders = new CollisionShape3D[Faces * Layers];
        for (int i = 0; i < Faces * Layers; i++) {
            meshes[i] = new MeshInstance3D();
            colliders[i] = new CollisionShape3D();
            AddChild(meshes[i]);
            AddChild(colliders[i]);
        }
    }

    public virtual void GenerateMesh() {}
}