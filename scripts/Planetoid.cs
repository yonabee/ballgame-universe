using System;
using System.Collections.Generic;
using Godot;

public partial class Planetoid : RigidBody3D, HeavenlyBody
{
    public static readonly float Dampening = 0.8f;
    public Vector3 initialVelocity;
    public float Radius { get; set; }
    public int Faces { get; set; }
    public int Layers { get; set; }
    public Vector3 CurrentVelocity { get; set; }
    public RandomNumberGenerator Random;
    public int Seed { get; set; }
    public float Gravity { get; set; }
    public bool OutOfBounds { get; set; }
    public Vector3 CurrentRotation { get; set; }

    private ulong _colliderId;
    private int _collisionWaitFrames = 0;

    public enum MaterialType
    {
        Standard,
        Metallic,
        Glass,
        SolidGlass,
        BlackHole
    }

    public override void _Ready()
    {
        Configure();
        GeneratePlanet();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_collisionWaitFrames > 0)
        {
            _collisionWaitFrames--;
        }
    }

    public void UpdatePosition(float timeStep)
    {
        // Do nothing and let _IntegrateForces handle it.
    }

    public void UpdateVelocity(List<HeavenlyBody> allBodies, Vector3 universeOrigin, float timeStep)
    {
        var globalCameraPos = Universe.PlayerCam.ToGlobal(Universe.PlayerCam.Transform.Origin);
        var playerDistance = Transform.Origin.DistanceTo(globalCameraPos);
        // GD.Print(
        //     Transform.Origin
        //         + "----"
        //         + ToGlobal(Universe.PlayerCam.Transform.Origin)
        //         + " --- "
        //         + playerDistance
        // );
        if (playerDistance < 300f + Radius)
        {
            var direction = ToLocal(globalCameraPos).Normalized();
            if (Universe.LastPlayerHit != this || Universe.HitTimer == 0)
            {
                Universe.LastPlayerHit = this;
                Universe.HitTimer = 25;
                //CurrentVelocity = CurrentVelocity.Bounce(direction);
                CurrentRotation = CurrentRotation.Bounce(direction);
                CurrentVelocity += -direction * timeStep * 10000f;
            }
            Translate(-direction * (Radius + 300f - playerDistance));
            // GD.Print("Hit mass of " + Mass);
        }
        var distance = Transform.Origin.DistanceTo(universeOrigin);
        if (distance > Universe.Radius)
        {
            if (!OutOfBounds)
            {
                Universe.OutOfBounds++;
                OutOfBounds = true;
                CurrentVelocity /= 10;
            }
        }
        else
        {
            if (OutOfBounds)
            {
                Universe.OutOfBounds--;
                OutOfBounds = false;
            }
        }
        foreach (var node in allBodies)
        {
            var nodeDistance = node.Transform.Origin.DistanceTo(Transform.Origin);
            if (node != this && (node.Mass * 0.8f) > Mass && nodeDistance < 20 * node.Radius)
            {
                if (nodeDistance > node.Radius)
                {
                    Utils.ApplyBodyToVelocity(this, node, node.Mass, node.Radius, timeStep);
                }
                else
                {
                    Utils.ApplyBodyToVelocity(this, node, node.Mass * 100f, 0, timeStep, true);
                }
            }
        }

        if (OutOfBounds)
        {
            Utils.ApplyBodyToVelocity(this, Universe.Planet, 1000000000, 0, timeStep);
        }
        else if (distance > Radius + (Universe.Planet.Radius * 2f))
        {
            Utils.ApplyBodyToVelocity(
                this,
                Universe.Planet,
                Universe.Planet.Mass,
                Universe.Planet.Radius,
                timeStep
            );
        }
        else
        {
            Utils.ApplyBodyToVelocity(
                this,
                Universe.Planet,
                Universe.Planet.Mass,
                Universe.Planet.Radius,
                timeStep,
                true
            );
        }
    }

    public virtual void Configure()
    {
        Faces = 1;
        Layers = 1;
        CustomIntegrator = true;
        CurrentVelocity = initialVelocity;
        Mass = Gravity * Radius * Radius / Universe.Gravity * 10000;
        Random = Universe.Random;
        CollisionLayer = 1;
        SetCollisionMaskValue(1, true);
        SetCollisionMaskValue(2, true);
        MaxContactsReported = 1;
        ContactMonitor = true;
    }

    public void GeneratePlanet()
    {
        Initialize();
        GenerateMesh();
    }

    public void IntegrateForces(PhysicsDirectBodyState3D state)
    {
        if (state.GetContactCount() > 0)
        {
            var id = state.GetContactColliderId(0);
            if (_colliderId == id && _collisionWaitFrames > 0)
            {
                return;
            }
            _colliderId = id;
            _collisionWaitFrames = 25;
            var collider = state.GetContactColliderObject(0);
            var colliderName = collider.GetType().ToString();
            var normal = state.GetContactLocalNormal(0);

            if (colliderName == "Spheroid" || colliderName == "MicroSpheroid")
            {
                var planetoid = collider as Planetoid;
                // GD.Print(
                //     "Mass of "
                //         + Mass.ToString()
                //         + " colliding with "
                //         + planetoid.Mass.ToString()
                //         + " of velocity "
                //         + planetoid.CurrentVelocity
                // );
                CurrentVelocity = Utils.Collide(this, planetoid, -normal);
            }
            else
            {
                CurrentVelocity = CurrentVelocity.Bounce(normal) * Dampening;
            }

            CurrentRotation = CurrentRotation.Bounce(normal);
        }
        state.LinearVelocity = CurrentVelocity;
        state.AngularVelocity = CurrentRotation;
    }

    public StandardMaterial3D GetMaterial(MaterialType type)
    {
        StandardMaterial3D material;
        if (type == MaterialType.BlackHole)
        {
            material = new StandardMaterial3D
            {
                EmissionEnabled = true,
                EmissionEnergyMultiplier = 200f / Radius,
                VertexColorUseAsAlbedo = true,
                Roughness = 0f,
                Metallic = 1f,
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                RefractionEnabled = true,
                RimEnabled = true,
                RimTint = 0.5f,
                SpecularMode = BaseMaterial3D.SpecularModeEnum.Toon,
            };
        }
        else if (type == MaterialType.Glass)
        {
            material = new StandardMaterial3D
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
        }
        else if (type == MaterialType.SolidGlass)
        {
            material = new StandardMaterial3D
            {
                EmissionEnabled = true,
                VertexColorUseAsAlbedo = true,
                ClearcoatEnabled = true,
                ClearcoatRoughness = 0.5f,
                Roughness = 0.3f,
                Metallic = 0.8f,
                RimEnabled = true,
                RimTint = 1f,
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                CullMode = BaseMaterial3D.CullModeEnum.Disabled
            };
        }
        else if (type == MaterialType.Metallic)
        {
            material = new StandardMaterial3D
            {
                EmissionEnabled = true,
                EmissionEnergyMultiplier = 200f / Radius,
                ClearcoatEnabled = true,
                ClearcoatRoughness = 0.5f,
                VertexColorUseAsAlbedo = true,
                Roughness = 0.2f,
                Metallic = 1f,
            };
        }
        else
        {
            material = new StandardMaterial3D
            {
                EmissionEnabled = true,
                EmissionEnergyMultiplier = 200f / Radius,
                VertexColorUseAsAlbedo = true,
                ClearcoatEnabled = true,
                ClearcoatRoughness = 1.0f,
                Roughness = 1.0f,
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                RefractionEnabled = true,
                SpecularMode = BaseMaterial3D.SpecularModeEnum.Toon,
            };
        }
        return material;
    }

    public virtual void Initialize() { }

    public virtual void GenerateMesh() { }
}
