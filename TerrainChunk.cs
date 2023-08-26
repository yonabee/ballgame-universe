
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public class TerrainChunk 
{
    public ColorGenerator colorGenerator;
    public ShapeGenerator shapeGenerator;
    public ShapeGenerator.ShapeSettings settings;
    public TerrainFace face; 
    public MeshInstance3D[] Meshes;
    public int ActiveMesh = -1;
    public List<int> SkippedChunks = new List<int>();
    Vector3 axisA;
    Vector3 axisB;
    Vector3[] faceVerts;
    Vector3[] faceNormals;
    Color[] faceColors;
    int chunkX;
    int chunkY;
    int chunkSize;
    int chunkStep;
    public int ChunkDimension;
    bool endChunk;
    // List<Action> meshActions = new List<Action>();

    public TerrainChunk(
        TerrainFace face,
        ColorGenerator colorGenerator,
        ShapeGenerator shapeGenerator, 
        ShapeGenerator.ShapeSettings settings,
        int chunkX,
        int chunkY,
        bool endChunk
    ) {
        this.settings = settings;
        this.shapeGenerator = shapeGenerator;
        this.colorGenerator = colorGenerator;
        this.face = face;
        this.endChunk = endChunk;

        axisA = new Vector3(face.Up.Y, face.Up.Z, face.Up.X);
        axisB = face.Up.Cross(axisA);

        var arrays = face.LandMesh.Mesh.SurfaceGetArrays(0);
        faceVerts = arrays[(int)Mesh.ArrayType.Vertex].AsVector3Array();
        faceNormals = arrays[(int)Mesh.ArrayType.Normal].AsVector3Array();
        faceColors = arrays[(int)Mesh.ArrayType.Color].AsColorArray();

        // The chunk size must always be evenly divisible by 3.
        chunkSize = face.resolution / TerrainFace.ChunkDimension;
        this.chunkX = chunkX * chunkSize;
        this.chunkY = chunkY * chunkSize;
        chunkStep = chunkSize / 3;

        ChunkDimension = endChunk ? 5 : 3;
        Meshes = new MeshInstance3D[ChunkDimension * ChunkDimension];
    }

    public async Task ConstructMeshes()
    {
        SkippedChunks.Clear();
        await _ConstructMesh(2,2);
        // for (int y = 0; y < ChunkDimension; y++) {
        //     for (int x = 0; x < ChunkDimension; x++) {
        //         if (x == 2 && y == 2) {
        //             continue;
        //         }
        //         await _ConstructMesh(x,y);
        //     }
        // }

        GD.Print("processed " + ChunkDimension + " by " + ChunkDimension + " meshes for chunk at " + chunkX + "," + chunkY);
    }

    public void Show() {

        // _RunNextMeshAction();
        // Is player within chunk?

        // Check where within chunk.

        for (int i = 0; i < Meshes.Length; i++) {
            if (Meshes[i] == null || Meshes[i].Mesh == null) {
                continue;
            }
            if (i == ActiveMesh) {
                Meshes[i].CallDeferred(Node3D.MethodName.Show);
                Meshes[i].SetDeferred(Node.PropertyName.ProcessMode, (int)Node.ProcessModeEnum.Inherit);
            } else {
                Meshes[i].CallDeferred(Node3D.MethodName.Hide);
                Meshes[i].SetDeferred(Node.PropertyName.ProcessMode, (int)Node.ProcessModeEnum.Disabled);
            }
        }
    }

    public void Hide() {
        for (int i = 0; i < Meshes.Length; i++) {
            if (Meshes[i] != null) {
                Meshes[i].CallDeferred(Node3D.MethodName.Hide);
                Meshes[i].SetDeferred(Node.PropertyName.ProcessMode, (int)Node.ProcessModeEnum.Disabled);
            }
        }
    }

    async Task _ConstructMesh(int x, int y) {
        int i = x + (y * ChunkDimension);
        if (x <= 1 && y <=1) {
            SkippedChunks.Add(i);
            return;
        }
        if (endChunk && (x >= ChunkDimension - 2 || y >= ChunkDimension - 2)) {
            SkippedChunks.Add(i);
            return;
        } 
        Meshes[i] = new MeshInstance3D();

        var task = Task.Run(() => _ConstructChunkMesh(x, y, Meshes[i]));
        await task;
        
        Universe.Planet.CallDeferred(Node.MethodName.AddChild, Meshes[i]);
        Universe.Planet.CallDeferred("OnChunkMeshCompleted", Meshes[i], x == 2 && y == 2);
        if (x == 2 && y == 2) {
            ActiveMesh = i;
        }
    }

    void _ConstructChunkMesh(int xIndex, int yIndex, MeshInstance3D mesh) {
        var offsetY = (((yIndex + 1) * chunkStep) - chunkSize) + chunkY;
        var offsetX = (((xIndex + 1) * chunkStep) - chunkSize) + chunkX;
        var startY = Mathf.Max(offsetY, 0);
        var startX = Mathf.Max(offsetX, 0);
        var endY = Mathf.Min(offsetY + chunkSize, face.resolution);
        var endX = Mathf.Min(offsetX + chunkSize, face.resolution);

        int dX = chunkSize;
        int dY = chunkSize;

        var verts = new Vector3[dX * dY];
        var normals = new Vector3[dX * dY];
        var colors = new Color[dX * dY];
        var tris = new int[(dX - 1) * (dY - 1) * 6];

        int triIndex = 0;
		var landSurfaceArray = new Godot.Collections.Array();
		landSurfaceArray.Resize((int)Mesh.ArrayType.Max);


        for (int y = startY; y < endY; y++)
        {
            for (int x = startX; x < endX; x++)
            {
                int currentChunkX = x - startX;
                int currentChunkY = y - startY;
                int i = currentChunkX + (currentChunkY * chunkSize);
                int arrayIndex = x + (y * face.resolution);

                verts[i] = faceVerts[arrayIndex];
                normals[i] = faceNormals[arrayIndex];
                colors[i] = faceColors[arrayIndex];

                if (x != endX - 1 && y != endY - 1)
                {
                    tris[triIndex + 2] = i;
                    tris[triIndex + 1] = i + chunkSize + 1; 
                    tris[triIndex] = i + chunkSize;

                    tris[triIndex + 5] = i;
                    tris[triIndex + 4] = i + 1;
                    tris[triIndex + 3] = i + chunkSize + 1;
                    triIndex += 6;
                }
            }
        }

		landSurfaceArray[(int)Mesh.ArrayType.Vertex] = verts;
        landSurfaceArray[(int)Mesh.ArrayType.Normal] = normals;
		landSurfaceArray[(int)Mesh.ArrayType.Color] = colors;
		landSurfaceArray[(int)Mesh.ArrayType.Index] = tris;
        mesh.Mesh = new ArrayMesh();
        (mesh.Mesh as ArrayMesh).AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, landSurfaceArray);
    }

    // void _RunNextMeshAction() {
    //     if (meshActions.Count > 0) {
    //         var action = meshActions[0];
    //         action();
    //         meshActions.RemoveAt(0);
    //         // if (meshActions.Count == 0) {
    //         //     Universe.Planet.GetTree().ProcessFrame -= _RunNextMeshAction;
    //         // }
    //     }
    // }
}