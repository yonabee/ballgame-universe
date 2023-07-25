using System.Collections;
using System.Collections.Generic;
using Godot;

public class TerrainFace 
{
    public ColorGenerator colorGenerator;
    public ShapeGenerator shapeGenerator;
    public ShapeGenerator.ShapeSettings settings;
    public ArrayMesh landMesh;
    public ArrayMesh oceanMesh;
    public Elevation[,] Elevations;
    int resolution;
    Vector3 localUp;
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
        ArrayMesh landMesh,
        ArrayMesh oceanMesh,
        int resolution,
        Vector3 localUp
    ) {
        this.settings = settings;
        this.shapeGenerator = shapeGenerator;
        this.colorGenerator = colorGenerator;
        this.landMesh = landMesh;
        this.oceanMesh = oceanMesh;
        this.resolution = resolution;
        this.localUp = localUp;

        axisA = new Vector3(localUp.Y, localUp.Z, localUp.X);
        axisB = localUp.Cross(axisA);

        verts = new Vector3[resolution * resolution];
        normals = new Vector3[resolution * resolution];
        oceanVerts = new Vector3[resolution * resolution];
        oceanNormals = new Vector3[resolution * resolution];
        colors = new Color[resolution * resolution];
        oceanColors = new Color[resolution * resolution];
        tris = new int[(resolution - 1) * (resolution - 1) * 6];

        Elevations = new Elevation[resolution,resolution];
    }

    public void Elevate()
    {
        for (int y = 0; y < resolution; y++) {
            for (int x = 0; x < resolution; x++) {
                Vector2 percent = new Vector2(x, y) / (resolution - 1);
                Vector3 pointOnUnitCube = localUp + (percent.X - .5f) * 2 * axisA + (percent.Y - .5f) * 2 * axisB;
                Vector3 pointOnUnitSphere = Utils.CubeToSphere(pointOnUnitCube);
                Elevations[x,y] = shapeGenerator.DetermineElevation(pointOnUnitSphere);
            }
        }
        colorGenerator.UpdateElevation(shapeGenerator.elevationMinMax);
    }

    public void ConstructMesh()
    {
        int triIndex = 0;
        colors = (landMesh.GetSurfaceCount() > 0 && landMesh.SurfaceGetArrayLen(0) == verts.Length) 
            ? colors 
            : new Color[verts.Length];

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
                Vector3 pointOnUnitCube = localUp + (percent.X - .5f) * 2 * axisA + (percent.Y - .5f) * 2 * axisB;
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
        landMesh.ClearSurfaces();
        landMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, landSurfaceArray);

		oceanSurfaceArray[(int)Mesh.ArrayType.Vertex] = oceanVerts;
        oceanSurfaceArray[(int)Mesh.ArrayType.Normal] = oceanNormals;
		oceanSurfaceArray[(int)Mesh.ArrayType.Color] = oceanColors;
		oceanSurfaceArray[(int)Mesh.ArrayType.Index] = tris;
        oceanMesh.ClearSurfaces();
        oceanMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, oceanSurfaceArray);

        GD.Print("generated meshes");
    }

}