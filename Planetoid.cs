using Godot;
using System;
using System.Collections.Generic;

public partial class Planetoid : RigidBody3D, HeavenlyBody
{
    public Vector3 initialVelocity;
    public float Radius { get; set; }
    public Color[] crayons = { Colors.Red };
    public MeshInstance3D meshInstance;
    public Vector3 CurrentVelocity { get; set; }
    public int id = 0;
    public float Gravity { get; set; }
    public bool OutOfBounds { get; set; }

    public override void _Ready()
    {
        Gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
        CustomIntegrator = true;
        CurrentVelocity = initialVelocity;
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
}