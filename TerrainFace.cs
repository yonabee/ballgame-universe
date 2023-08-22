using System.Collections;
using System.Collections.Generic;
using Godot;


public class TerrainFace 
{
    public ColorGenerator colorGenerator;
    public ShapeGenerator shapeGenerator;
    public ShapeGenerator.ShapeSettings settings;
    public MeshInstance3D LandMesh;
    public MeshInstance3D OceanMesh;
    public Elevation[,] Elevations;
    public Vector3 Up;
    public Utils.Face Face;
    public static int ChunkDimension = 1;
    public int resolution;
    public TerrainChunk[] Chunks;
    Vector3 axisA;
    Vector3 axisB;
    Vector3[] verts;
    Vector3[] normals;
    Vector3[] oceanVerts;
    Vector3[] oceanNormals;
    Color[] colors;
    Color[] oceanColors;
    int[] tris;

    public TerrainFace(
        ColorGenerator colorGenerator,
        ShapeGenerator shapeGenerator, 
        ShapeGenerator.ShapeSettings settings,
        int resolution,
        Vector3 localUp,
        Utils.Face face
    ) {
        this.settings = settings;
        this.shapeGenerator = shapeGenerator;
        this.colorGenerator = colorGenerator;
        this.resolution = resolution;
        this.Up = localUp;
        this.Face = face;

        LandMesh = new MeshInstance3D();
        OceanMesh = new MeshInstance3D();
        Chunks = new TerrainChunk[ChunkDimension * ChunkDimension];

        axisA = new Vector3(localUp.Y, localUp.Z, localUp.X);
        axisB = localUp.Cross(axisA);

        Elevations = new Elevation[resolution, resolution];
        verts = new Vector3[resolution * resolution];
        normals = new Vector3[resolution * resolution];
        oceanVerts = new Vector3[resolution * resolution];
        oceanNormals = new Vector3[resolution * resolution];
        colors = new Color[resolution * resolution];
        oceanColors = new Color[resolution * resolution];
        tris = new int[(resolution - 1) * (resolution - 1) * 6];
    }

    public void Elevate()
    {
        for (int y = 0; y < resolution; y++) {
            for (int x = 0; x < resolution; x++) {
                Vector2 percent = new Vector2(x, y) / (resolution - 1);
                Vector3 pointOnUnitCube = Up + (percent.X - .5f) * 2 * axisA + (percent.Y - .5f) * 2 * axisB;
                Vector3 pointOnUnitSphere = Utils.CubeToSphere(pointOnUnitCube);
                Elevations[x,y] = shapeGenerator.DetermineElevation(pointOnUnitSphere);
            }
        }
        colorGenerator.UpdateElevation(shapeGenerator.elevationMinMax);
    }    
    
    public void ConstructMesh()
    {
        int triIndex = 0;

		var landSurfaceArray = new Godot.Collections.Array();
		landSurfaceArray.Resize((int)Mesh.ArrayType.Max);
		var oceanSurfaceArray = new Godot.Collections.Array();
		oceanSurfaceArray.Resize((int)Mesh.ArrayType.Max);

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                int i = x + y * resolution;
                Vector2 percent = new Vector2(x, y) / (resolution - 1);
                Vector3 pointOnUnitCube = Up + (percent.X - .5f) * 2 * axisA + (percent.Y - .5f) * 2 * axisB;
                Vector3 pointOnUnitSphere = Utils.CubeToSphere(pointOnUnitCube);

                Elevation elevation = Elevations[x,y];

                verts[i] = pointOnUnitSphere * elevation.scaled;
                normals[i] = verts[i].Normalized();
                oceanVerts[i] = pointOnUnitSphere * settings.radius;
                oceanNormals[i] = oceanVerts[i].Normalized();
                colors[i] = colorGenerator.BiomeColorFromPoint(pointOnUnitSphere, elevation.unscaled);
                oceanColors[i] = colorGenerator.OceanColorFromPoint(pointOnUnitSphere);

                if (x != resolution - 1 && y != resolution - 1)
                {
                    tris[triIndex + 2] = i;
                    tris[triIndex + 1] = i + resolution + 1; 
                    tris[triIndex] = i + resolution;

                    tris[triIndex + 5] = i;
                    tris[triIndex + 4] = i + 1;
                    tris[triIndex + 3] = i + resolution + 1;
                    triIndex += 6;
                }
            }
        }

		landSurfaceArray[(int)Mesh.ArrayType.Vertex] = verts;
        landSurfaceArray[(int)Mesh.ArrayType.Normal] = normals;
		landSurfaceArray[(int)Mesh.ArrayType.Color] = colors;
		landSurfaceArray[(int)Mesh.ArrayType.Index] = tris;
        LandMesh = new MeshInstance3D();
        LandMesh.Mesh = new ArrayMesh();
        (LandMesh.Mesh as ArrayMesh).ClearSurfaces();
        (LandMesh.Mesh as ArrayMesh).AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, landSurfaceArray);

		oceanSurfaceArray[(int)Mesh.ArrayType.Vertex] = oceanVerts;
        oceanSurfaceArray[(int)Mesh.ArrayType.Normal] = oceanNormals;
		oceanSurfaceArray[(int)Mesh.ArrayType.Color] = oceanColors;
		oceanSurfaceArray[(int)Mesh.ArrayType.Index] = tris;
        OceanMesh = new MeshInstance3D();
        OceanMesh.Mesh = new ArrayMesh();
        (OceanMesh.Mesh as ArrayMesh).ClearSurfaces();
        (OceanMesh.Mesh as ArrayMesh).AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, oceanSurfaceArray);

        Universe.Planet.CallDeferred(Node.MethodName.AddChild, LandMesh);
        Universe.Planet.CallDeferred(Node.MethodName.AddChild, OceanMesh);
        ConstructChunks();

        GD.Print("generated meshes");
    }

    public void ConstructChunks()
    {
        for (int y = 0; y < ChunkDimension; y++) {
            for (int x = 0; x < ChunkDimension; x++) {
                int i = x + (y * ChunkDimension);
                Chunks[i] = new TerrainChunk(
                    this, 
                    colorGenerator, 
                    shapeGenerator, 
                    settings, 
                    x,
                    y,
                    y == ChunkDimension - 1 || x == ChunkDimension - 1
                );
                Chunks[i].ConstructMeshes();
            }
        } 

        GD.Print("constructed " + (ChunkDimension * ChunkDimension) + " chunks");
    }

    public void Show()
    {
        if (Universe.Planet.LOD == LOD.Orbit) {
            LandMesh.CallDeferred(Node3D.MethodName.Show);
            LandMesh.SetDeferred(Node.PropertyName.ProcessMode, (int)Node.ProcessModeEnum.Inherit);
            for (int i = 0; i < Chunks.Length; i++) {
                Chunks[i]?.Hide();
            }
        } else {
            LandMesh.CallDeferred(Node3D.MethodName.Hide);
            LandMesh.SetDeferred(Node.PropertyName.ProcessMode, (int)Node.ProcessModeEnum.Disabled);
            for (int i = 0; i < Chunks.Length; i++) {
                Chunks[i]?.Show();
            } 
        }
    }

    public void Hide()
    {
        if (Universe.Planet.LOD == LOD.Orbit) {
            LandMesh.CallDeferred(Node3D.MethodName.Hide);
            LandMesh.SetDeferred(Node.PropertyName.ProcessMode, (int)Node.ProcessModeEnum.Disabled);
        } else {
            for (int i = 0; i < Chunks.Length; i++) {
                Chunks[i]?.Hide();
            } 
        }
    }
}