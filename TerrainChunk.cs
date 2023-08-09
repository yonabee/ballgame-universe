
using System.Collections;
using System.Collections.Generic;
using Godot;

public class TerrainChunk 
{
    public ColorGenerator colorGenerator;
    public ShapeGenerator shapeGenerator;
    public ShapeGenerator.ShapeSettings settings;
    public ArrayMesh landMesh;
    public TerrainFace face; 
    int chunkX;
    int chunkY;
    int chunkResolution;
    Vector3 axisA;
    Vector3 axisB;
    Vector3[] verts;
    Vector3[] normals;
    Color[] colors;
    int[] tris;

    public TerrainChunk(
        TerrainFace face,
        ColorGenerator colorGenerator,
        ShapeGenerator shapeGenerator, 
        ShapeGenerator.ShapeSettings settings,
        ArrayMesh landMesh,
        int chunkX,
        int chunkY
    ) {
        this.settings = settings;
        this.shapeGenerator = shapeGenerator;
        this.colorGenerator = colorGenerator;
        this.landMesh = landMesh;
        this.chunkX = chunkX;
        this.chunkY = chunkY;
        this.face = face;
        this.chunkResolution = face.chunkResolution;

        axisA = new Vector3(face.Up.Y, face.Up.Z, face.Up.X);
        axisB = face.Up.Cross(axisA);

        int dX = chunkResolution;
        int dY = chunkResolution;
        // if (chunkX == 0 && chunkY == 0) {
        //     dX = chunkResolution;
        //     dY = chunkResolution;
        // } else if (chunkX == 0 || chunkY == 0) {
        //     dX = chunkResolution + 1;
        //     dY = chunkResolution;
        // } else {
        //     dX = chunkResolution + 1;
        //     dY = chunkResolution + 1;
        // }

        // if (chunkX != face.resolution - face.chunkResolution && chunkY != face.resolution - face.chunkResolution) {
        //     dX++;
        //     dY++;
        // } else if (chunkX != face.resolution - face.chunkResolution || chunkY != face.resolution - face.chunkResolution) {
        //     dX++;
        // }

        verts = new Vector3[dX * dY];
        normals = new Vector3[dX * dY];
        colors = new Color[dX * dY];
        tris = new int[(dX - 1) * (dY - 1) * 6];
    }

    public void ConstructMesh()
    {
        int triIndex = 0;
        colors = (landMesh.GetSurfaceCount() > 0 && landMesh.SurfaceGetArrayLen(0) == verts.Length) 
            ? colors 
            : new Color[verts.Length];

		var landSurfaceArray = new Godot.Collections.Array();
		landSurfaceArray.Resize((int)Mesh.ArrayType.Max);

        var startY = chunkY * chunkResolution;
        var startX = chunkX * chunkResolution;
        var endY = startY + chunkResolution;
        var endX = startX + chunkResolution;

        // if (iY != 0) {
        //     iY--;
        // }
        // if (iX != 0) {
        //     iX--;
        // }

        // if (eY != face.resolution) {
        //     eY++;
        // }
        // if (eX != face.resolution) {
        //     eX++;
        // }

        //GD.Print(iX + "," + eX + " " + iY + " " + eY);

        for (int y = startY; y < endY; y++)
        {
            for (int x = startX; x < endX; x++)
            {
                int chunkX = x - startX;
                int chunkY = y - startY;
                int i = chunkX + (chunkY * chunkResolution);
                Vector2 percent = new Vector2(x, y) / (float)(face.resolution - 1);
                Vector3 pointOnUnitCube = face.Up + (percent.X - .5f) * 2 * axisA + (percent.Y - .5f) * 2 * axisB;
                Vector3 pointOnUnitSphere = Utils.CubeToSphere(pointOnUnitCube);

                Elevation elevation = face.Elevations[x,y];

                verts[i] = pointOnUnitSphere * elevation.scaled;
                normals[i] = verts[i].Normalized();
                colors[i] = colorGenerator.BiomeColorFromPoint(pointOnUnitSphere, elevation.unscaled);

                if (x != endX - 1 && y != endY - 1)
                {
                    tris[triIndex + 2] = i;
                    tris[triIndex + 1] = i + chunkResolution + 1; 
                    tris[triIndex] = i + chunkResolution;

                    tris[triIndex + 5] = i;
                    tris[triIndex + 4] = i + 1;
                    tris[triIndex + 3] = i + chunkResolution + 1;
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

        GD.Print("generated meshes for face " + face.Face + " and chunk " + chunkX + "," + chunkY);
    }
}