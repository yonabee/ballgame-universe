using System;
using System.Collections.Generic;
using Godot;

public partial class MicroSpheroid : Planetoid
{
    public int radialSegments = 50;
    public int rings = 50;
    public Color[] crayons = { Colors.Red };
    public MeshInstance3D[] Meshes;
    public CollisionShape3D[] Colliders;
    FastNoiseLite _Noise;
    Func<Vector3, Color> _GetVertexColor;
    float _StripeChance;

    public override void Initialize()
    {
        base.Initialize();
        Meshes = new MeshInstance3D[Faces * Layers];
        Colliders = new CollisionShape3D[Faces * Layers];
        for (int i = 0; i < Faces * Layers; i++)
        {
            Meshes[i] = new MeshInstance3D();
            Colliders[i] = new CollisionShape3D();
            AddChild(Meshes[i]);
            AddChild(Colliders[i]);
        }
        if (Colliders[0].Shape == null)
        {
            var shape = new SphereShape3D { Radius = Radius };
            Colliders[0].Shape = shape;
        }

        _Noise = new FastNoiseLite
        {
            NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex,
            FractalOctaves = 4,
            Seed = Seed,
            Frequency = Random.RandfRange(0.0005f, 0.001f),
            DomainWarpEnabled = true,
            DomainWarpFractalOctaves = 2,
            DomainWarpFrequency = Random.RandfRange(0.0005f, 0.005f)
        };

        _StripeChance = Random.Randf();

        _GetVertexColor = (Vector3 vert) =>
        {
            var color = Colors.Black;
            var noiseValue = Mathf.Abs(_Noise.GetNoise3Dv(vert));

            // Contrast stripes
            if (_StripeChance < 0.2 && noiseValue < 0.3 && crayons.Length > 1)
            {
                if (noiseValue < 0.1)
                {
                    color = crayons[1].Lightened(0.2f);
                }
                else
                {
                    color = crayons[1];
                }

                // Regular racing stripes, occasionally with spots
            }
            else if (noiseValue < 0.1)
            {
                if (crayons[0] != Colors.White)
                {
                    color = crayons[0].Lightened(0.2f);
                }
                else
                {
                    color = crayons[1].Darkened(0.15f);
                }
            }
            else if (noiseValue > 0.6)
            {
                if (_StripeChance < 0.4 && crayons.Length > 1)
                {
                    color = crayons[1].Darkened(0.15f);
                }
                else
                {
                    if (crayons[0] != Colors.White)
                    {
                        color = crayons[0].Darkened(0.15f);
                    }
                    else
                    {
                        color = crayons[1].Lightened(0.2f);
                    }
                }
            }
            else
            {
                color = crayons[0];
            }
            return color;
        };
    }

    public override void _IntegrateForces(PhysicsDirectBodyState3D state)
    {
        IntegrateForces(state);
    }

    public override void GenerateMesh()
    {
        base.GenerateMesh();
        Meshes[0].Mesh = MeshUtils.GenerateSphereMesh(
            Radius,
            rings,
            radialSegments,
            _GetVertexColor
        );
        var materialChance = Universe.Random.Randf();
        StandardMaterial3D material;
        if (materialChance < 0.01)
        {
            material = GetMaterial(MaterialType.Metallic);
        }
        else if (materialChance > 0.95)
        {
            material = GetMaterial(MaterialType.SolidGlass);
        }
        else
        {
            material = GetMaterial(MaterialType.Standard);
        }
        (Meshes[0].Mesh as ArrayMesh).SurfaceSetMaterial(0, material);
    }
}
