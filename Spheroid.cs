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
    }
    
    public override void _IntegrateForces(PhysicsDirectBodyState3D state)
    {
        state.LinearVelocity = CurrentVelocity;
        state.AngularVelocity = BaseRotation;
    }


    public override void GenerateMesh()
    {
        base.GenerateMesh();

        var chance = Random.Randf();

        var noise = new FastNoiseLite
        {
            NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex,
            FractalOctaves = 4,
            Seed = Seed,
            Frequency = Random.RandfRange(0.0005f, 0.001f),
            DomainWarpEnabled = true,
            DomainWarpFractalOctaves = 2,
            DomainWarpFrequency = Random.RandfRange(0.0005f, 0.005f)
        };

        var verts = new List<Vector3>();
		var uvs = new List<Vector2>();
		var normals = new List<Vector3>();
		var indices = new List<int>();
        var colors = new List<Color>();

		var surfaceArray = new Godot.Collections.Array();
		surfaceArray.Resize((int)Mesh.ArrayType.Max);

        // Vertex indices.
        var thisRow = 0;
        var prevRow = 0;
        var point = 0;

        // Loop over rings.
        for (var i = 0; i < rings + 1; i++)
        {
            var v = ((float)i) / rings;
            var w = Mathf.Sin(Mathf.Pi * v);
            var y = Mathf.Cos(Mathf.Pi * v);

            // Loop over segments in ring.
            for (var j = 0; j < radialSegments; j++)
            {
                var u = ((float)j) / radialSegments;
                var x = Mathf.Sin(u * Mathf.Pi * 2);
                var z = Mathf.Cos(u * Mathf.Pi * 2);
                var vert = new Vector3(x * Radius * w, y * Radius, z * Radius * w);
                verts.Add(vert);
                normals.Add(vert.Normalized());
                var noizz = Mathf.Abs(noise.GetNoise3Dv(vert));

                // "Pride" marbles 🏳️‍🌈🏳️‍⚧️
                if (Crayons.Length > 2) {
                    for (var k = 0; k < Crayons.Length; k++) {
                        if (noizz < (float)(k + 1)/(float)Crayons.Length) {
                            if (chance < 0.1) {
                                colors.Add(Crayons[k].Lightened(0.2f));
                            } else if (chance < 0.2) {
                                colors.Add(Crayons[k].Darkened(0.15f));
                            } else {
                                colors.Add(Crayons[k]);
                            }
                            break;
                        }
                    }

                }

                // Contrast stripes
                else if (chance < 0.2 && noizz < 0.3 && Crayons.Length > 1) {
                    if (noizz < 0.1) {
                        colors.Add(Crayons[1].Lightened(0.2f));
                    } else {
                        colors.Add(Crayons[1]);
                    }

                // Regular racing stripes, occasionally with spots
                } else if (noizz < 0.1) {
                    if (Crayons[0] != Colors.White) {
                        colors.Add(Crayons[0].Lightened(0.2f));
                    } else {
                        colors.Add(Crayons[1].Darkened(0.15f));
                    }
                } else if (noizz > 0.6) {
                    if (chance < 0.4 && Crayons.Length > 1) {
                        colors.Add(Crayons[1].Darkened(0.15f));
                    } else  {
                        if (Crayons[0] != Colors.White) {
                            colors.Add(Crayons[0].Darkened(0.15f));
                        } else {
                            colors.Add(Crayons[1].Lightened(0.2f));
                        }
                    }
                } else {
                    colors.Add(Crayons[0]);
                }
                uvs.Add(new Vector2(u, v));
                point += 1;
 
                // Create triangles in ring using indices.
                if (i > 0 && j > 0)
                {
                    indices.Add(prevRow + j - 1);
                    indices.Add(prevRow + j);
                    indices.Add(thisRow + j - 1);

                    indices.Add(prevRow + j);
                    indices.Add(thisRow + j);
                    indices.Add(thisRow + j - 1);
                }
            }

            if (i > 0)
            {
                indices.Add(prevRow + radialSegments - 1);
                indices.Add(prevRow);
                indices.Add(thisRow + radialSegments - 1);

                indices.Add(prevRow);
                indices.Add(prevRow + radialSegments);
                indices.Add(thisRow + radialSegments - 1);
            }

            prevRow = thisRow;
            thisRow = point;
        }

		surfaceArray[(int)Mesh.ArrayType.Vertex] = verts.ToArray();
		surfaceArray[(int)Mesh.ArrayType.TexUV] = uvs.ToArray();
        surfaceArray[(int)Mesh.ArrayType.Color] = colors.ToArray();
		surfaceArray[(int)Mesh.ArrayType.Normal] = normals.ToArray();
		surfaceArray[(int)Mesh.ArrayType.Index] = indices.ToArray();

		Meshes[0].Mesh = new ArrayMesh();
		(Meshes[0].Mesh as ArrayMesh).AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surfaceArray);

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
            SpecularMode = BaseMaterial3D.SpecularModeEnum.Toon
        };
        (Meshes[0].Mesh as ArrayMesh).SurfaceSetMaterial(0, material);
    }
}