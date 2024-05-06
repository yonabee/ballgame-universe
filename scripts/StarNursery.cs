using System;
using Godot;

public enum BodyType
{
    Moonlet,
    Moon,
    BlackHole
}

public class BodySettings
{
    public BodyType Type;
    public float Size;
    public int Seed;
    public Vector3 Velocity;
    public float Distance;
    public Color[] Colors;
    public float Transparency;
}

public static class StarNursery
{
    public static Vector3 PreviousPosition = Vector3.Zero;

    public static HeavenlyBody CreateHeavenlyBody(BodySettings settings)
    {
        switch (settings.Type)
        {
            case BodyType.Moonlet:
                var moonlet = new MicroSpheroid { Seed = settings.Seed, Radius = settings.Size };
                moonlet.rings = Mathf.FloorToInt(moonlet.Radius);
                moonlet.radialSegments = moonlet.rings;
                moonlet.Gravity = moonlet.Radius / 5f;
                moonlet.initialVelocity = settings.Velocity;

                moonlet.Translate(Utils.RandomPointOnUnitSphere() * settings.Distance);

                moonlet.crayons = settings.Colors;
                for (int j = 0; j < moonlet.crayons.Length; j++)
                {
                    moonlet.crayons[j].A = settings.Transparency;
                }
                return moonlet;

            case BodyType.Moon:
                var moon = new Spheroid { Seed = settings.Seed, Radius = settings.Size };
                moon.rings = Mathf.FloorToInt(moon.Radius);
                moon.radialSegments = moon.rings;
                moon.Gravity = moon.Radius / 10f;
                moon.initialVelocity = settings.Velocity;

                moon.Translate(Utils.RandomPointOnUnitSphere() * settings.Distance);

                moon.Crayons = settings.Colors;
                var chance = Universe.Random.Randf();
                if (chance < 0.2f)
                {
                    chance = Universe.Random.Randf();
                    Color[] crayons;
                    // classic rainbow
                    if (chance < 0.15f)
                    {
                        crayons = new[]
                        {
                            new Color("#E50000"),
                            new Color("#FF8D00"),
                            new Color("#FFEE00"),
                            new Color("#028121"),
                            new Color("#004CFF"),
                            new Color("#770088")
                        };
                        // progress rainbow
                    }
                    else if (chance < 0.3f)
                    {
                        crayons = new[]
                        {
                            new Color("#FFFFFF"),
                            new Color("#FFAFC7"),
                            new Color("#73D7EE"),
                            new Color("#613915"),
                            new Color("#000000"),
                            new Color("#E50000"),
                            new Color("#FF8D00"),
                            new Color("#FFEE00"),
                            new Color("#028121"),
                            new Color("#004CFF"),
                            new Color("#770088")
                        };
                        // transgender
                    }
                    else if (chance < 0.45f)
                    {
                        crayons = new[]
                        {
                            new Color("#5BCFFB"),
                            new Color("#F5ABB9"),
                            new Color("#FFFFFF"),
                            new Color("#F5ABB9"),
                            new Color("#5BCFFB")
                        };
                        // lesbian
                    }
                    else if (chance < 0.6f)
                    {
                        crayons = new[]
                        {
                            new Color("#D62800"),
                            new Color("#FF9B56"),
                            new Color("#FFFFFF"),
                            new Color("#D462A6"),
                            new Color("#A40062"),
                        };

                        // bisexual
                    }
                    else if (chance < 0.7f)
                    {
                        crayons = new[]
                        {
                            new Color("#D60270"),
                            new Color("#9B4F96"),
                            new Color("#0038A8")
                        };
                        // pansexual
                    }
                    else if (chance < 0.8f)
                    {
                        crayons = new[]
                        {
                            new Color("#FF1C8D"),
                            new Color("#FFD700"),
                            new Color("#1AB3FF")
                        };
                        // nonbinary
                    }
                    else if (chance < 0.9f)
                    {
                        crayons = new[]
                        {
                            new Color("#FCF431"),
                            new Color("#FCFCFC"),
                            new Color("#9D59D2"),
                            new Color("#282828")
                        };
                        // genderfluid
                    }
                    else
                    {
                        crayons = new[]
                        {
                            new Color("#FE76A2"),
                            new Color("#FFFFFF"),
                            new Color("#BF12D7"),
                            new Color("#000000"),
                            new Color("#303CBE")
                        };
                    }

                    var offset = Universe.Random.RandiRange(0, crayons.Length - 1);
                    moon.Crayons = new Color[crayons.Length];
                    for (var idx = offset; idx < crayons.Length + offset; idx++)
                    {
                        moon.Crayons[idx % crayons.Length] = crayons[idx % crayons.Length];
                        moon.Crayons[idx % crayons.Length].A = settings.Transparency;
                    }
                }
                else
                {
                    for (int j = 0; j < moon.Crayons.Length; j++)
                    {
                        moon.Crayons[j].A = settings.Transparency;
                    }
                }
                return moon;

            case BodyType.BlackHole:
                var gg = new GasGiant
                {
                    Gravity = settings.Size,
                    Seed = settings.Seed,
                    EventHorizon = settings.Size / 5f,
                    Radius = 0.0f,
                    OmniRange = settings.Size,
                    OmniAttenuation = 0.2f,
                    LightColor = settings.Colors[0],
                    //ShadowEnabled = true,
                    LightSize = settings.Size / 15f,
                    //ShadowBias = 0.1f,
                    //ShadowNormalBias = 1f,
                    //ShadowBlur = 5f,
                    RotationSpeed = Universe.Random.RandfRange(0.1f, 0.3f)
                };
                gg.rings = Mathf.FloorToInt(gg.EventHorizon);
                gg.radialSegments = gg.rings;
                var doubleChance = Universe.Random.Randf();
                if (PreviousPosition == Vector3.Zero || doubleChance > 0.075f)
                {
                    PreviousPosition = Utils.RandomPointOnUnitSphere() * settings.Distance;
                }
                gg.Translate(PreviousPosition);
                return gg;
        }
        return null;
    }
}
