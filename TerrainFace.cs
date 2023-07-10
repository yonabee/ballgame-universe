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
    int resolution;
    Vector3 localUp;
    Vector3 axisA;
    Vector3 axisB;
    Vector3[] verts;
    Vector3[] oceanVerts;
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
        oceanVerts = new Vector3[resolution * resolution];
        colors = new Color[resolution * resolution];
        oceanColors = new Color[resolution * resolution];
        tris = new int[(resolution - 1) * (resolution - 1) * 6];
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
                Vector3 pointOnUnitSphere = pointOnUnitCube.Normalized();

                Elevation elevation = shapeGenerator.GetElevation(pointOnUnitSphere);

                verts[i] = pointOnUnitSphere * elevation.scaled;
                oceanVerts[i] = pointOnUnitSphere * settings.radius;

                if (x != resolution - 1 && y != resolution - 1)
                {
                    tris[triIndex] = i;
                    tris[triIndex + 1] = i + resolution + 1; 
                    tris[triIndex + 2] = i + resolution;

                    tris[triIndex + 3] = i;
                    tris[triIndex + 4] = i + 1;
                    tris[triIndex + 5] = i + resolution + 1;
                    triIndex += 6;
                }
            }
        }

        colorGenerator.UpdateElevation(shapeGenerator.elevationMinMax);

        for (int y = 0; y < resolution; y++) {
            for (int x = 0; x < resolution; x++) {
                int i = x + y * resolution;
                Vector2 percent = new Vector2(x, y) / (resolution - 1);
                Vector3 pointOnUnitCube = localUp + (percent.X - .5f) * 2 * axisA + (percent.Y - .5f) * 2 * axisB;
                Vector3 pointOnUnitSphere = pointOnUnitCube.Normalized();

                Elevation elevation = shapeGenerator.GetElevation(pointOnUnitSphere);

                colors[i] = colorGenerator.BiomeColorFromPoint(pointOnUnitSphere, elevation.unscaled);
                oceanColors[i] = colorGenerator.OceanColorFromPoint(pointOnUnitSphere);
            }
        }

		landSurfaceArray[(int)Mesh.ArrayType.Vertex] = verts;
		landSurfaceArray[(int)Mesh.ArrayType.Color] = colors;
		landSurfaceArray[(int)Mesh.ArrayType.Index] = tris;
        landMesh.ClearSurfaces();
        landMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, landSurfaceArray);
        //landMesh.RegenNormalMaps();

		// oceanSurfaceArray[(int)Mesh.ArrayType.Vertex] = verts;
		// oceanSurfaceArray[(int)Mesh.ArrayType.Color] = colors;
		// oceanSurfaceArray[(int)Mesh.ArrayType.Index] = tris;
        // oceanMesh.ClearSurfaces();
        // oceanMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, oceanSurfaceArray);
        //oceanMesh.RegenNormalMaps();

        GD.Print("generated meshes");
    }
}