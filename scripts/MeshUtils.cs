using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public static class MeshUtils
{
    public static ArrayMesh GenerateCubeMesh(Color color, float size = 1f)
    {
        var verts = new List<Vector3>();
        var uvs = new List<Vector2>();
        var normals = new List<Vector3>();
        var indices = new List<int>();
        var colors = new List<Color>();

        var surfaceArray = new Godot.Collections.Array();
        surfaceArray.Resize((int)Mesh.ArrayType.Max);

        //      Y
        //      |
        //      o ---- X
        //     /
        //    Z    A ------------ B
        //         /|           /|
        //       F ------------ E|
        //        | |          | |
        //        |D ----------|- C
        //        |/           |/
        //       G ------------ H

        var a = new Vector3(0f, size, 0f);
        var b = new Vector3(size, size, 0f);
        var c = new Vector3(size, 0f, 0f);
        var d = new Vector3(0f, 0f, 0f); // D is at the origin
        var e = new Vector3(size, size, size); // E is opposite of origin
        var f = new Vector3(0f, size, size);
        var g = new Vector3(0f, 0f, size);
        var h = new Vector3(size, 0f, size);

        verts.AddRange(
            new Vector3[]
            {
                b,
                a,
                d,
                c,
                f,
                e,
                h,
                g,
                a,
                f,
                g,
                d,
                e,
                b,
                c,
                h,
                a,
                b,
                e,
                f,
                g,
                h,
                c,
                d,
            }
        );

        normals.AddRange(verts.Select(vert => vert.Normalized()));
        colors.AddRange(verts.Select(vert => color));

        var uv_a = new Vector2(0f, 0f);
        var uv_b = new Vector2(size, 0f);
        var uv_c = new Vector2(size, size);
        var uv_d = new Vector2(0f, size);

        uvs.AddRange(
            new Vector2[]
            {
                uv_a,
                uv_b,
                uv_c,
                uv_d,
                uv_a,
                uv_b,
                uv_c,
                uv_d,
                uv_a,
                uv_b,
                uv_c,
                uv_d,
                uv_a,
                uv_b,
                uv_c,
                uv_d,
                uv_a,
                uv_b,
                uv_c,
                uv_d,
                uv_a,
                uv_b,
                uv_c,
                uv_d,
            }
        );

        var addIndices = (int a, int b, int c, int d) =>
            indices.AddRange(new int[] { a, c, d, a, b, c });

        addIndices(0, 1, 2, 3); // North (Z)
        addIndices(4, 5, 6, 7); // South
        addIndices(8, 9, 10, 11); // West (X)
        addIndices(12, 13, 14, 15); // East
        addIndices(16, 17, 18, 19); // Top (Y)
        addIndices(20, 21, 22, 23); // Bottom

        surfaceArray[(int)Mesh.ArrayType.Vertex] = verts.ToArray();
        surfaceArray[(int)Mesh.ArrayType.TexUV] = uvs.ToArray();
        surfaceArray[(int)Mesh.ArrayType.Color] = colors.ToArray();
        surfaceArray[(int)Mesh.ArrayType.Normal] = normals.ToArray();
        surfaceArray[(int)Mesh.ArrayType.Index] = indices.ToArray();

        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surfaceArray);

        return mesh;
    }

    public static ArrayMesh GenerateSphereMesh(
        float radius,
        int rings,
        int radialSegments,
        Func<Vector3, Color> vertexColor
    )
    {
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
                colors.Add(vertexColor(vert));
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

        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surfaceArray);

        return mesh;
    }
}
