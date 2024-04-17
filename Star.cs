using System.Collections.Generic;
using Godot;

public partial class Star : OmniLight3D, HeavenlyBody 
{
    public float Mass { get; set; }
    public float Radius { get; set; }
    public float Gravity { get; set; }
    public Vector3 CurrentVelocity { get; set; }
    public bool OutOfBounds { get; set; }
    public float EventHorizon { get; set; }
    public MeshInstance3D SMesh = new MeshInstance3D();
    public CollisionShape3D SCollider = new CollisionShape3D();
    public Vector3 initialVelocity;
    public int radialSegments = 50;
    public int rings = 50;

    public override void _Ready()
    {
        Mass = Gravity * Radius * Radius / Universe.Gravity;
        CurrentVelocity = initialVelocity;
        GenerateMesh();
        AddChild(SMesh);
        SCollider.Shape = new SphereShape3D 
        {
            Radius = EventHorizon * 0.9f
        };
    }
    
    public void GenerateMesh()
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
            var color = LightColor;
            color.A = 0.1f;

            // Loop over segments in ring.
            for (var j = 0; j < radialSegments; j++)
            {
                var u = ((float)j) / radialSegments;
                var x = Mathf.Sin(u * Mathf.Pi * 2);
                var z = Mathf.Cos(u * Mathf.Pi * 2);
                var vert = new Vector3(x * EventHorizon * w, y * EventHorizon, z * EventHorizon * w);
                verts.Add(vert);
                normals.Add(vert.Normalized());
                colors.Add(color);
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

		SMesh.Mesh = new ArrayMesh();
		(SMesh.Mesh as ArrayMesh).AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surfaceArray);

        var material = new StandardMaterial3D
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
        };
        (SMesh.Mesh as ArrayMesh).SurfaceSetMaterial(0, material);
    }
}