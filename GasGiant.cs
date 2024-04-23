using System;
using System.Collections.Generic;
using Godot;

public partial class GasGiant : OmniLight3D, HeavenlyBody 
{
    public float Mass { get; set; }
    public float Radius { get; set; }
    public float Gravity { get; set; }
    public Vector3 CurrentVelocity { get; set; }
    public bool OutOfBounds { get; set; }
    public float EventHorizon { get; set; }
    public Vector3 BaseRotation { get; set; }
    public Color[] Crayons { get; set; }
    public MeshInstance3D SMesh = new MeshInstance3D();
    public Vector3 initialVelocity;
    public float RotationSpeed { get; set; }
    public int radialSegments = 200;
    public int rings = 200;
    FastNoiseLite _Noise;
    Func<Vector3, Color> _GetVertexColor;

    public override void _Ready()
    {
        Mass = Gravity * Radius * Radius / Universe.Gravity;
        CurrentVelocity = initialVelocity;

        Initialize();
        GenerateMesh();
    }

    public void Initialize()
    {
        _Noise = new FastNoiseLite
        {
            NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex,
            FractalOctaves = 4,
            Seed = Universe.Seed.GetHashCode(),
            Frequency = Universe.Random.RandfRange(0.00005f, 0.0001f),
            DomainWarpEnabled = true,
            DomainWarpFractalOctaves = 1,
            DomainWarpFrequency = Universe.Random.RandfRange(0.0005f, 0.001f)
        };

        Crayons = new Color[] { LightColor, new Color(Utils.Crayons[Universe.Random.RandiRange(0, 47)]).Lightened(0.3f)};
        Crayons[0].A = 0.3f;
        Crayons[1].A = 0.2f;

        _GetVertexColor = (Vector3 vert) => {
            var color = Colors.Black;
            var noiseValue = Mathf.Abs(_Noise.GetNoise3Dv(vert));
            if (noiseValue < 0.1f || noiseValue >= 0.9f) {
                if (Crayons[0] != Colors.White) {
                    color = Crayons[0].Lightened(0.2f);
                } else {
                    color = Crayons[1].Darkened(0.15f);
                }
            } else if (noiseValue < 0.2f || noiseValue >= 0.8f) {
                color = Crayons[1].Darkened(0.15f);
            } else if (noiseValue < 0.3f || noiseValue >= 0.7f) {
                if (Crayons[0] != Colors.White) {
                    color = Crayons[0].Darkened(0.15f);
                } else {
                    color = Crayons[1].Lightened(0.2f);
                }
            } else if (noiseValue < 0.4f || noiseValue >= 0.6f) {
                color = Crayons[0];
            } else {
                color = Crayons[1];
            }
            return color;
        };

        GenerateMesh();
        AddChild(SMesh);
    }
    
    public void UpdateVelocity(List<HeavenlyBody> allBodies, Vector3 universeOrigin, float timeStep) 
    {
        var distance = Transform.Origin.DistanceTo(universeOrigin);
        if (distance > Universe.Radius) {
            if (!OutOfBounds) {
                Universe.OutOfBounds++;
                OutOfBounds = true;
                CurrentVelocity /= 10;
            }
        } else {
            if (OutOfBounds) {
                Universe.OutOfBounds--;
                OutOfBounds = false;
            }
        } 

        foreach(var node in allBodies) 
        {
            if (node != this && node.Transform.Origin.DistanceTo(Transform.Origin) < 5 * node.Radius)
            {
                Utils.ApplyBodyToVelocity(this, node, node.Mass, node.Radius, timeStep);
            }
        }

        if (OutOfBounds) {
            Utils.ApplyBodyToVelocity(this, Universe.Planet, 1000000000, 0, timeStep);
        } else if (distance < Universe.Planet.Radius + Radius) {
            Utils.ApplyBodyToVelocity(this, Universe.Planet, 1000000000, 0, timeStep, true);
        } else {
            Utils.ApplyBodyToVelocity(this, Universe.Planet, Universe.Planet.Mass, Universe.Planet.Radius, timeStep);
        }
    }
    public void UpdatePosition(float timeStep) 
    {
        Translate(CurrentVelocity * timeStep);
        RotateObjectLocal(BaseRotation.Normalized(), timeStep * RotationSpeed);
    }
    
    public void GenerateMesh()
    {
		SMesh.Mesh = MeshUtils.GenerateSphereMesh(EventHorizon, rings, radialSegments, _GetVertexColor);
        // var verts = new List<Vector3>();
		// var uvs = new List<Vector2>();
		// var normals = new List<Vector3>();
		// var indices = new List<int>();
        // var colors = new List<Color>();

		// var surfaceArray = new Godot.Collections.Array();
		// surfaceArray.Resize((int)Mesh.ArrayType.Max);

        // // Vertex indices.
        // var thisRow = 0;
        // var prevRow = 0;
        // var point = 0;

        // // Loop over rings.
        // for (var i = 0; i < rings + 1; i++)
        // {
        //     var v = ((float)i) / rings;
        //     var w = Mathf.Sin(Mathf.Pi * v);
        //     var y = Mathf.Cos(Mathf.Pi * v);

        //     // Loop over segments in ring.
        //     for (var j = 0; j < radialSegments; j++)
        //     {
        //         var u = ((float)j) / radialSegments;
        //         var x = Mathf.Sin(u * Mathf.Pi * 2);
        //         var z = Mathf.Cos(u * Mathf.Pi * 2);
        //         var vert = new Vector3(x * EventHorizon * w, y * EventHorizon, z * EventHorizon * w);
        //         verts.Add(vert);
        //         normals.Add(vert.Normalized());
        //         colors.Add(_GetVertexColor(vert));
        //         uvs.Add(new Vector2(u, v));
        //         point += 1;
 
        //         // Create triangles in ring using indices.
        //         if (i > 0 && j > 0)
        //         {
        //             indices.Add(prevRow + j - 1);
        //             indices.Add(prevRow + j);
        //             indices.Add(thisRow + j - 1);

        //             indices.Add(prevRow + j);
        //             indices.Add(thisRow + j);
        //             indices.Add(thisRow + j - 1);
        //         }
        //     }

        //     if (i > 0)
        //     {
        //         indices.Add(prevRow + radialSegments - 1);
        //         indices.Add(prevRow);
        //         indices.Add(thisRow + radialSegments - 1);

        //         indices.Add(prevRow);
        //         indices.Add(prevRow + radialSegments);
        //         indices.Add(thisRow + radialSegments - 1);
        //     }

        //     prevRow = thisRow;
        //     thisRow = point;
        // }

		// surfaceArray[(int)Mesh.ArrayType.Vertex] = verts.ToArray();
		// surfaceArray[(int)Mesh.ArrayType.TexUV] = uvs.ToArray();
        // surfaceArray[(int)Mesh.ArrayType.Color] = colors.ToArray();
		// surfaceArray[(int)Mesh.ArrayType.Normal] = normals.ToArray();
		// surfaceArray[(int)Mesh.ArrayType.Index] = indices.ToArray();

		// SMesh.Mesh = new ArrayMesh();
		// (SMesh.Mesh as ArrayMesh).AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surfaceArray);

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
            CullMode = BaseMaterial3D.CullModeEnum.Disabled
        };
        (SMesh.Mesh as ArrayMesh).SurfaceSetMaterial(0, material);
    }
}