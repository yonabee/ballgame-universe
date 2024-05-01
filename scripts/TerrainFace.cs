using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public class TerrainFace
{
    public ColorGenerator colorGenerator;
    public ShapeGenerator shapeGenerator;
    public ShapeGenerator.ShapeSettings settings;
    public MeshInstance3D[] LandMeshes;
    public MeshInstance3D OceanMesh;
    public MeshInstance3D ColliderMesh;
    public Elevation[,] Elevations;
    public Vector3 Up;
    public Utils.Face Face;
    public static int ChunkDimension = 1;
    public int resolution;
    Vector3 axisA;
    Vector3 axisB;
    Vector3[] verts;
    Vector3[] normals;
    Vector3[] oceanVerts;
    Vector3[] oceanNormals;
    Color[] colors;
    Color[] oceanColors;
    int[] tris;
    List<Action> colliderTasks = new List<Action>();

    public TerrainFace(
        ColorGenerator colorGenerator,
        ShapeGenerator shapeGenerator,
        ShapeGenerator.ShapeSettings settings,
        int resolution,
        Vector3 localUp,
        Utils.Face face
    )
    {
        this.settings = settings;
        this.shapeGenerator = shapeGenerator;
        this.colorGenerator = colorGenerator;
        this.resolution = resolution;
        this.Up = localUp;
        this.Face = face;

        LandMeshes = new MeshInstance3D[3];

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

    public void Elevate(LOD lod)
    {
        int scale = _GetScale(lod);
        int res = resolution / scale;
        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                int scaledX = x == res - 1 ? resolution - 1 : x * scale;
                int scaledY = y == res - 1 ? resolution - 1 : y * scale;
                Vector2 percent = new Vector2(x, y) / (res - 1);
                Vector3 pointOnUnitCube =
                    Up + (percent.X - .5f) * 2 * axisA + (percent.Y - .5f) * 2 * axisB;
                Vector3 pointOnUnitSphere = Utils.CubeToSphere(pointOnUnitCube);
                Elevations[scaledX, scaledY] = shapeGenerator.DetermineElevation(pointOnUnitSphere);
            }
        }

        colorGenerator.UpdateElevation(shapeGenerator.elevationMinMax);
    }

    public async Task MakeColliders()
    {
        var tasks = colliderTasks.Select(async action =>
        {
            await Task.Run(action);
            Universe.Progress.Value += 4.2;
        });
        await Task.WhenAll(tasks);
        colliderTasks.Clear();
    }

    public async Task ConstructMesh(LOD lod, Dictionary<Vector3, Vector3> seams)
    {
        await Task.Run(() =>
        {
            int triIndex = 0;

            var landSurfaceArray = new Godot.Collections.Array();
            landSurfaceArray.Resize((int)Mesh.ArrayType.Max);
            var oceanSurfaceArray = new Godot.Collections.Array();
            oceanSurfaceArray.Resize((int)Mesh.ArrayType.Max);

            int scale = _GetScale(lod);
            int landMeshIndex = 0;
            float far = 12000f;
            float near = 5000f;
            if (lod == LOD.Orbit)
            {
                landMeshIndex = 1;
                far = 5000f;
                near = 1500f;
            }
            else if (lod == LOD.NearOrbit)
            {
                landMeshIndex = 2;
                far = 1500f;
                near = 1f;
            }

            int i = 0;
            int y = 0;
            int x = 0;
            int res = resolution / scale;

            for (y = 0; y < res; y++)
            {
                for (x = 0; x < res; x++)
                {
                    i = x + y * res;

                    Vector2 percent = new Vector2(x, y) / (res - 1);

                    Vector3 pointOnUnitCube =
                        Up + (percent.X - .5f) * 2 * axisA + (percent.Y - .5f) * 2 * axisB;

                    Vector3 pointOnUnitSphere = Utils.CubeToSphere(pointOnUnitCube);

                    Elevation elevation = Elevations[
                        x == res - 1 ? resolution - 1 : x * scale,
                        y == res - 1 ? resolution - 1 : y * scale
                    ];

                    verts[i] = pointOnUnitSphere * elevation.scaled;
                    if (percent.X == 0 || percent.X == 1 || percent.Y == 0 || percent.Y == 1)
                    {
                        if (seams.TryGetValue(pointOnUnitCube, out Vector3 seamValue))
                        {
                            verts[i] = seamValue;
                        }
                        else
                        {
                            seams.Add(pointOnUnitCube, verts[i]);
                        }
                    }
                    normals[i] = verts[i].Normalized();
                    oceanVerts[i] = pointOnUnitSphere * settings.radius;
                    oceanNormals[i] = oceanVerts[i].Normalized();
                    colors[i] = colorGenerator.BiomeColourFromPoint(
                        pointOnUnitSphere,
                        elevation.unscaled
                    );
                    oceanColors[i] = colorGenerator.OceanColorFromPoint(pointOnUnitSphere);

                    if (x > 0 && y > 0)
                    {
                        tris[triIndex] = i - 1;
                        tris[triIndex + 1] = i - res;
                        tris[triIndex + 2] = i - res - 1;

                        tris[triIndex + 3] = i - 1;
                        tris[triIndex + 4] = i;
                        tris[triIndex + 5] = i - res;

                        triIndex += 6;
                    }
                }
            }

            landSurfaceArray[(int)Mesh.ArrayType.Vertex] = scale == 1 ? verts : verts[0..(i + 1)];
            landSurfaceArray[(int)Mesh.ArrayType.Normal] =
                scale == 1 ? normals : normals[0..(i + 1)];
            landSurfaceArray[(int)Mesh.ArrayType.Color] = scale == 1 ? colors : colors[0..(i + 1)];
            landSurfaceArray[(int)Mesh.ArrayType.Index] =
                scale == 1 ? tris : tris[0..(triIndex + 3)];

            if (lod != LOD.Collision)
            {
                LandMeshes[landMeshIndex] = new MeshInstance3D
                {
                    Mesh = new ArrayMesh(),
                    VisibilityRangeBegin = near,
                    VisibilityRangeBeginMargin = 1f,
                    VisibilityRangeEnd = far,
                    VisibilityRangeEndMargin = 1f
                };
                (LandMeshes[landMeshIndex].Mesh as ArrayMesh).AddSurfaceFromArrays(
                    Mesh.PrimitiveType.Triangles,
                    landSurfaceArray
                );
                LandMeshes[landMeshIndex]
                    .Mesh.CallDeferred(
                        Mesh.MethodName.SurfaceSetMaterial,
                        0,
                        Universe.Planet.LandRenderer
                    );
                LandMeshes[landMeshIndex].Visible = false;
                Universe.Planet.CallDeferred(Node.MethodName.AddChild, LandMeshes[landMeshIndex]);
            }
            else if (Universe.ConstructPlanetColliders)
            {
                ColliderMesh = new MeshInstance3D { Mesh = new ArrayMesh() };
                (ColliderMesh.Mesh as ArrayMesh).AddSurfaceFromArrays(
                    Mesh.PrimitiveType.Triangles,
                    landSurfaceArray
                );
                ColliderMesh.Visible = false;
                Universe.Planet.CallDeferred(Node.MethodName.AddChild, ColliderMesh);
                colliderTasks.Add(
                    () =>
                        ColliderMesh.CallDeferred(MeshInstance3D.MethodName.CreateTrimeshCollision)
                );
            }

            if (lod == LOD.Space)
            {
                oceanSurfaceArray[(int)Mesh.ArrayType.Vertex] = oceanVerts;
                oceanSurfaceArray[(int)Mesh.ArrayType.Normal] = oceanNormals;
                oceanSurfaceArray[(int)Mesh.ArrayType.Color] = oceanColors;
                oceanSurfaceArray[(int)Mesh.ArrayType.Index] = tris;

                OceanMesh = new MeshInstance3D { Mesh = new ArrayMesh() };
                (OceanMesh.Mesh as ArrayMesh).AddSurfaceFromArrays(
                    Mesh.PrimitiveType.Triangles,
                    oceanSurfaceArray
                );
                OceanMesh.Mesh.CallDeferred(
                    Mesh.MethodName.SurfaceSetMaterial,
                    0,
                    Universe.Planet.OceanRenderer
                );
                // if (Universe.ConstructPlanetColliders) {
                //     colliderTasks.Add(() => OceanMesh.CallDeferred(MeshInstance3D.MethodName.CreateTrimeshCollision));
                // }
                OceanMesh.Visible = false;
                Universe.Planet.CallDeferred(Node.MethodName.AddChild, OceanMesh);
            }
        });
    }

    public void Show()
    {
        for (int i = 0; i < LandMeshes.Length; i++)
        {
            if (LandMeshes[i] != null)
            {
                LandMeshes[i].CallDeferred(Node3D.MethodName.Show);
                LandMeshes[i].ProcessMode = Node.ProcessModeEnum.Inherit;
            }
        }
        if (OceanMesh != null)
        {
            OceanMesh.CallDeferred(Node3D.MethodName.Show);
            OceanMesh.ProcessMode = Node.ProcessModeEnum.Inherit;
        }
    }

    public void Hide()
    {
        for (int i = 0; i < LandMeshes.Length; i++)
        {
            if (LandMeshes[i] != null)
            {
                LandMeshes[i].CallDeferred(Node3D.MethodName.Hide);
                LandMeshes[i].ProcessMode = Node.ProcessModeEnum.Disabled;
            }
        }
        if (OceanMesh != null)
        {
            OceanMesh.CallDeferred(Node3D.MethodName.Hide);
            OceanMesh.ProcessMode = Node.ProcessModeEnum.Disabled;
        }
    }

    int _GetScale(LOD lod)
    {
        if (lod == LOD.Orbit)
        {
            return 3;
        }
        if (lod == LOD.NearOrbit)
        {
            return 1;
        }
        // if (lod == LOD.Collision) {
        //     return 20;
        // }
        return 10;
    }
}
