
using System.Collections;
using System.Collections.Generic;
using Godot;

public class TerrainChunk 
{
    public ColorGenerator colorGenerator;
    public ShapeGenerator shapeGenerator;
    public ShapeGenerator.ShapeSettings settings;
    public MeshInstance3D landMesh;
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
    int overlap = 1;

    public TerrainChunk(
        TerrainFace face,
        ColorGenerator colorGenerator,
        ShapeGenerator shapeGenerator, 
        ShapeGenerator.ShapeSettings settings,
        MeshInstance3D landMesh,
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

        if (chunkX == 0 && chunkY == 0) {
            dX = chunkResolution;
            dY = chunkResolution;
        } else if (chunkX == 0 || chunkY == 0) {
            dX = chunkResolution + overlap;
            dY = chunkResolution;
        } else {
            dX = chunkResolution + overlap;
            dY = chunkResolution + overlap;
        }

        // if (chunkX != face.resolution - face.chunkResolution && chunkY != face.resolution - face.chunkResolution) {
        //     dX += overlap;
        //     dY += overlap;
        // } else if (chunkX != face.resolution - face.chunkResolution || chunkY != face.resolution - face.chunkResolution) {
        //     dX += overlap;
        // }

        verts = new Vector3[dX * dY];
        normals = new Vector3[dX * dY];
        colors = new Color[dX * dY];
        tris = new int[(dX - 1) * (dY - 1) * 6];
    }

    public void ConstructMesh()
    {
        int triIndex = 0;
		var landSurfaceArray = new Godot.Collections.Array();
		landSurfaceArray.Resize((int)Mesh.ArrayType.Max);

        var startY = chunkY * chunkResolution;
        var startX = chunkX * chunkResolution;
        var endY = startY + chunkResolution;
        var endX = startX + chunkResolution;

        var arrays = face.LandMeshes[0].Mesh.SurfaceGetArrays(0);
        var arrayVerts = arrays[(int)Mesh.ArrayType.Vertex].AsVector3Array();
        var arrayNorms = arrays[(int)Mesh.ArrayType.Normal].AsVector3Array();
        var arrayColors = arrays[(int)Mesh.ArrayType.Color].AsColorArray();

        for (int y = startY; y < endY; y++)
        {
            for (int x = startX; x < endX; x++)
            {
                int currentChunkX = x - startX;
                int currentChunkY = y - startY;
                int i = currentChunkX + (currentChunkY * chunkResolution);

                // // var percentX = x / (float)(face.resolution - 1);
                // // var percentY = y / (float)(face.resolution - 1);
                // float percentX = (currentChunkX / (float)(chunkResolution - 1f) / face.numChunks) + (chunkX * chunkResolution / (face.resolution - 1f));
                // float percentY = (currentChunkY / (float)(chunkResolution - 1f) / face.numChunks) + (chunkY * chunkResolution / (face.resolution - 1f));

                // // GD.Print(percentX + " " + percentY);
                // Vector3 pointOnUnitCube = face.Up + (percentX - .5f) * 2 * axisA + (percentY - .5f) * 2 * axisB;
                // Vector3 pointOnUnitSphere = Utils.CubeToSphere(pointOnUnitCube);

                // Elevation elevation = face.Elevations[x,y];
                int arrayIndex = x + (y * face.resolution);
                verts[i] = arrayVerts[arrayIndex];
                normals[i] = arrayNorms[arrayIndex];
                colors[i] = arrayColors[arrayIndex];

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
        (landMesh.Mesh as ArrayMesh).AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, landSurfaceArray);

        GD.Print("generated meshes for face " + face.Face + " and chunk " + chunkX + "," + chunkY);
    }
}