using System.Collections;
using System.Collections.Generic;
using Godot;

public class TerrainFace 
{
    public ColorGenerator colorGenerator;
    public ShapeGenerator shapeGenerator;
    public ShapeGenerator.ShapeSettings settings;
    public MeshInstance3D[] LandMeshes;
    public MeshInstance3D[] OceanMeshes;
    public Elevation[,] Elevations;
    public Vector3 Up;
    public Utils.Face Face;
    public int numChunks = 8;
    public int chunkResolution = 50;
    public int resolution;
    Vector3 axisA;
    Vector3 axisB;
    TerrainChunk[] Chunks;
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

        LandMeshes = new MeshInstance3D[(numChunks * numChunks) + 1];
        OceanMeshes = new MeshInstance3D[1];
        Chunks = new TerrainChunk[numChunks * numChunks];

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
        LandMeshes[0] = new MeshInstance3D();
        LandMeshes[0].Mesh = new ArrayMesh();
        (LandMeshes[0].Mesh as ArrayMesh).ClearSurfaces();
        (LandMeshes[0].Mesh as ArrayMesh).AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, landSurfaceArray);

		oceanSurfaceArray[(int)Mesh.ArrayType.Vertex] = oceanVerts;
        oceanSurfaceArray[(int)Mesh.ArrayType.Normal] = oceanNormals;
		oceanSurfaceArray[(int)Mesh.ArrayType.Color] = oceanColors;
		oceanSurfaceArray[(int)Mesh.ArrayType.Index] = tris;
        OceanMeshes[0] = new MeshInstance3D();
        OceanMeshes[0].Mesh = new ArrayMesh();
        (OceanMeshes[0].Mesh as ArrayMesh).ClearSurfaces();
        (OceanMeshes[0].Mesh as ArrayMesh).AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, oceanSurfaceArray);

        Universe.Planet.AddChild(LandMeshes[0]);
        Universe.Planet.AddChild(OceanMeshes[0]);

        GD.Print("generated meshes");
    }

    public void ConstructChunks()
    {
        for (int y = 0; y < numChunks; y++) {
            for (int x = 0; x < numChunks; x++) {
                int i = x + (y * numChunks);
                LandMeshes[i + 1] = new MeshInstance3D();
                LandMeshes[i + 1].Mesh = new ArrayMesh();
                Chunks[i] = new TerrainChunk(
                    this, 
                    colorGenerator, 
                    shapeGenerator, 
                    settings, 
                    LandMeshes[i + 1].Mesh as ArrayMesh, 
                    x, 
                    y
                );
                Chunks[i].ConstructMesh();
                Universe.Planet.AddChild(LandMeshes[i + 1]);
                Universe.Planet.AddChild(OceanMeshes[i + 1]);
            }
        } 
    }

    public void Show()
    {
        for (int i = 0; i < LandMeshes.Length; i++) {
            if (LandMeshes[i] != null) {
                LandMeshes[i].Show();
                LandMeshes[i].ProcessMode = Node.ProcessModeEnum.Inherit;
            }
        }
    }

    public void Hide()
    {
        for (int i = 0; i < LandMeshes.Length; i++) {
            if (LandMeshes[i] != null) {
                LandMeshes[i].Hide();
                LandMeshes[i].ProcessMode = Node.ProcessModeEnum.Disabled;
            }
        }
    }
}