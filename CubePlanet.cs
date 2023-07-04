using Godot;
using System.Collections.Generic;

public partial class CubePlanet : Planetoid
{
    public int resolution = 10;
    public int numFaces = 6;
    public enum FaceRenderMask { All, Top, Bottom, Left, Right, Front, Back }
    public FaceRenderMask faceRenderMask;

    public ShapeGenerator.ShapeSettings shapeSettings;
    public ColorSettings colorSettings;

    ShapeGenerator shapeGenerator = new ShapeGenerator();
    ColorGenerator colorGenerator = new ColorGenerator();

    PhysicsBody3D[] landObjects;
    PhysicsBody3D[] oceanObjects;

    ArrayMesh[] landMeshes;
    ArrayMesh[] oceanMeshes;

    StandardMaterial3D[] landRenderers;
    StandardMaterial3D[] oceanRenderers;

    CollisionShape3D[] landColliders;
    CollisionShape3D[] oceanColliders;

    TerrainFace[] terrainFaces;
    Vector3[] directions = { Vector3.Up, Vector3.Down, Vector3.Left, Vector3.Right, Vector3.Forward, Vector3.Back };

    public override void Initialize()
    {
        base.Initialize();

        if (shapeSettings == null) {
            shapeSettings = new ShapeGenerator.ShapeSettings();
            shapeSettings.mass = Mass;
            shapeSettings.radius = radius;
            var noise = new NoiseSettings();
            noise.center = ToGlobal(Transform.Origin);
            noise.filterType = NoiseSettings.FilterType.Simple;
            NoiseSettings[] noiseSettings = { noise };
            shapeSettings.noiseSettings = noiseSettings;
        }

        if (colorSettings == null) {
            colorSettings = new ColorSettings();
            colorSettings.oceanColor = new Gradient();
            colorSettings.oceanColor.SetColor(0, new Color("#000080"));
            colorSettings.oceanColor.SetColor(1, new Color("#6cf"));
        }
    }   
}