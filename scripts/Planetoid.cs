using System;
using System.Collections.Generic;
using Godot;

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
    public Vector3 CurrentRotation { get; set; }

    public enum MaterialType
    {
        Standard,
        Metallic,
        Glass,
        SolidGlass,
        BlackHole
    }

    public override void _Ready()
    {
        Configure();
        GeneratePlanet();
    }

    public void UpdatePosition(float timeStep)
    {
        // Do nothing and let _IntegrateForces handle it.
    }

    public virtual void Configure()
    {
        Faces = 1;
        Layers = 1;
        // CustomIntegrator = true;
        CurrentVelocity = initialVelocity;
        Mass = Gravity * Radius * Radius / Universe.Gravity * 10000;
        Random = Universe.Random;
        CollisionLayer = 1;
        SetCollisionMaskValue(1, true);
        SetCollisionMaskValue(2, true);
    }

    public void GeneratePlanet()
    {
        Initialize();
        GenerateMesh();
    }

    public StandardMaterial3D GetMaterial(MaterialType type)
    {
        StandardMaterial3D material;
        if (type == MaterialType.BlackHole)
        {
            material = new StandardMaterial3D
            {
                EmissionEnabled = true,
                EmissionEnergyMultiplier = 200f / Radius,
                VertexColorUseAsAlbedo = true,
                Roughness = 0f,
                Metallic = 1f,
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                RefractionEnabled = true,
                RimEnabled = true,
                RimTint = 0.5f,
                SpecularMode = BaseMaterial3D.SpecularModeEnum.Toon,
            };
        }
        else if (type == MaterialType.Glass)
        {
            material = new StandardMaterial3D
            {
                EmissionEnabled = true,
                VertexColorUseAsAlbedo = true,
                ClearcoatEnabled = true,
                ClearcoatRoughness = 0.5f,
                Roughness = 0.3f,
                Metallic = 0.3f,
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                RimEnabled = true,
                RimTint = 0.5f,
                CullMode = BaseMaterial3D.CullModeEnum.Disabled
            };
        }
        else if (type == MaterialType.SolidGlass)
        {
            material = new StandardMaterial3D
            {
                EmissionEnabled = true,
                VertexColorUseAsAlbedo = true,
                ClearcoatEnabled = true,
                ClearcoatRoughness = 0.5f,
                Roughness = 0.3f,
                Metallic = 0.8f,
                RimEnabled = true,
                RimTint = 1f,
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                CullMode = BaseMaterial3D.CullModeEnum.Disabled
            };
        }
        else if (type == MaterialType.Metallic)
        {
            material = new StandardMaterial3D
            {
                EmissionEnabled = true,
                EmissionEnergyMultiplier = 200f / Radius,
                ClearcoatEnabled = true,
                ClearcoatRoughness = 0.5f,
                VertexColorUseAsAlbedo = true,
                Roughness = 0.2f,
                Metallic = 1f,
            };
        }
        else
        {
            material = new StandardMaterial3D
            {
                EmissionEnabled = true,
                EmissionEnergyMultiplier = 200f / Radius,
                VertexColorUseAsAlbedo = true,
                ClearcoatEnabled = true,
                ClearcoatRoughness = 1.0f,
                Roughness = 1.0f,
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                RefractionEnabled = true,
                SpecularMode = BaseMaterial3D.SpecularModeEnum.Toon,
            };
        }
        return material;
    }

    public virtual void Initialize() { }

    public virtual void GenerateMesh() { }
}
