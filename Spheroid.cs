using Godot;
using System;
using System.Collections.Generic;

public partial class Spheroid : Planetoid
{
    public int radialSegments = 50;
    public int rings = 50;
    public Color[] Crayons = { Colors.Red };
    public MeshInstance3D[] Meshes;
    public CollisionShape3D[] Colliders;
    Func<Vector3, Color> _GetVertexColor;
    FastNoiseLite _Noise;
    float _StripeChance;

    public override void Initialize()
    {
        base.Initialize();
		Meshes = new MeshInstance3D[Faces * Layers];
        Colliders = new CollisionShape3D[Faces * Layers];
        for (int i = 0; i < Faces * Layers; i++) {
            Meshes[i] = new MeshInstance3D();
            Colliders[i] = new CollisionShape3D();
            AddChild(Meshes[i]);
            AddChild(Colliders[i]);
        }
        if (Colliders[0].Shape == null) {
            var shape = new SphereShape3D
            {
                Radius = Radius
            };
            Colliders[0].Shape = shape;
        }
        
        _Noise = new FastNoiseLite
        {
            NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex,
            FractalOctaves = 4,
            Seed = Seed,
            Frequency = Random.RandfRange(0.0005f, 0.001f),
            DomainWarpEnabled = true,
            DomainWarpFractalOctaves = Random.RandiRange(1,3),
            DomainWarpFractalGain = Random.Randf() * 0.75f,
            DomainWarpAmplitude = Random.RandfRange(10f, 100f),
            DomainWarpFrequency = Random.RandfRange(0.0005f, 0.005f)
        };

        _StripeChance = Random.Randf();

        _GetVertexColor = (Vector3 vert) => {
            var noiseValue = Mathf.Abs(_Noise.GetNoise3Dv(vert));
            var color = Colors.Black;

            // "Pride" marbles 🏳️‍🌈🏳️‍⚧️
            if (Crayons.Length > 2) {
                for (var k = 0; k < Crayons.Length; k++) {
                    if (noiseValue < (float)(k + 1)/(float)Crayons.Length) {
                        if (_StripeChance < 0.1) {
                            color = Crayons[k].Lightened(0.2f);
                        } else if (_StripeChance < 0.2) {
                            color = Crayons[k].Darkened(0.15f);
                        } else {
                            color = Crayons[k];
                        }
                        break;
                    }
                }
            }

            // Contrast stripes
            else if (_StripeChance < 0.2 && noiseValue < 0.3 && Crayons.Length > 1) {
                if (noiseValue < 0.1) {
                    color = Crayons[1].Lightened(0.2f);
                } else {
                    color = Crayons[1];
                }

            // Regular racing stripes, occasionally with spots
            } else if (noiseValue < 0.1) {
                if (Crayons[0] != Colors.White) {
                    color = Crayons[0].Lightened(0.2f);
                } else {
                    color = Crayons[1].Darkened(0.15f);
                }

            } else if (noiseValue > 0.6) {
                if (_StripeChance < 0.4 && Crayons.Length > 1) {
                    color = Crayons[1].Darkened(0.15f);
                } else  {
                    if (Crayons[0] != Colors.White) {
                        color = Crayons[0].Darkened(0.15f);
                    } else {
                        color = Crayons[1].Lightened(0.2f);
                    }
                }

            } else {
                color = Crayons[0];
            }
            return color;
        };
    }
    
    public override void _IntegrateForces(PhysicsDirectBodyState3D state)
    {
        state.LinearVelocity = CurrentVelocity;
        state.AngularVelocity = BaseRotation;
    }


    public override void GenerateMesh()
    {
        base.GenerateMesh();
        Meshes[0].Mesh = MeshUtils.GenerateSphereMesh(Radius, rings, radialSegments, _GetVertexColor);
        var material = new StandardMaterial3D
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
        (Meshes[0].Mesh as ArrayMesh).SurfaceSetMaterial(0, material);
    }
}