using Godot;
using System;
using System.Collections.Generic;

public partial class Spheroid : Planetoid
{
    public int radialSegments = 50;
    public int rings = 50;

    public override void GenerateMesh()
    {
        base.GenerateMesh();

        var random = new RandomNumberGenerator();
        random.Seed = (ulong)id;
        var chance = random.Randf();

        var noise = new FastNoiseLite();
        noise.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;
        noise.FractalOctaves = 4;
        noise.Seed = id;
        noise.Frequency = random.RandfRange(0.0005f, 0.001f);
        noise.DomainWarpEnabled = true;
        noise.DomainWarpFractalOctaves = 2;
        noise.DomainWarpFrequency = random.RandfRange(0.0005f, 0.005f);

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
                var vert = new Vector3(x * radius * w, y * radius, z * radius * w);
                verts.Add(vert);
                normals.Add(vert.Normalized());
                var zz = Mathf.Abs(noise.GetNoise3Dv(vert));
                if (chance < 0.1 && zz < 0.3) {
                    if (zz < 0.1) {
                        colors.Add(altColor.Lightened(0.2f));
                    } else {
                        colors.Add(altColor);
                    }
                } else if (zz < 0.1) {
                    colors.Add(color.Lightened(0.2f));
                } else if (zz > 0.6) {
                    if (chance < 0.3) {
                        colors.Add(altColor.Darkened(0.15f));
                    } else  {
                        colors.Add(color.Darkened(0.15f));
                    }
                } else {
                    colors.Add(color);
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

		meshInstance.Mesh = new ArrayMesh();
		(meshInstance.Mesh as ArrayMesh).AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surfaceArray);

        var material = new StandardMaterial3D();
        //material.EmissionEnabled = true;
        //material.AlbedoColor = color;
        material.VertexColorUseAsAlbedo = true;
        material.ClearcoatEnabled = true;
        //material.RimEnabled = true;
        (meshInstance.Mesh as ArrayMesh).SurfaceSetMaterial(0, material);
    }
}