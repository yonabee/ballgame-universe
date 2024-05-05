using System;
using Godot;

public static class Utils
{
    public static float InverseSqrt2 = 0.70710676908493042f;

    public enum Face
    {
        All,
        Top,
        Bottom,
        Left,
        Right,
        Front,
        Back
    }

    public static Vector3 CubeToSphere(Vector3 point)
    {
        //return point.Normalized();
        return new Vector3(
            point.X
                * Mathf.Sqrt(
                    1.0f
                        - point.Y * point.Y * 0.5f
                        - point.Z * point.Z * 0.5f
                        + point.Y * point.Y * point.Z * point.Z / 3.0f
                ),
            point.Y
                * Mathf.Sqrt(
                    1.0f
                        - point.Z * point.Z * 0.5f
                        - point.X * point.X * 0.5f
                        + point.Z * point.Z * point.X * point.X / 3.0f
                ),
            point.Z
                * Mathf.Sqrt(
                    1.0f
                        - point.X * point.X * 0.5f
                        - point.Y * point.Y * 0.5f
                        + point.X * point.X * point.Y * point.Y / 3.0f
                )
        );
    }

    public static Vector3 SphereToCube(Vector3 point)
    {
        Vector3 p = point.Normalized();
        Vector3 result = new Vector3();
        float fx = Mathf.Abs(p.X);
        float fy = Mathf.Abs(p.Y);
        float fz = Mathf.Abs(p.Z);

        if (fy >= fx && fy >= fz)
        {
            float a2 = p.X * p.X * 2.0f;
            float b2 = p.Z * p.Z * 2.0f;
            float inner = -a2 + b2 - 3f;
            float innerSqrt = -Mathf.Sqrt((inner * inner) - 12.0f * a2);

            if (p.X == 0f)
            {
                result.X = 0f;
            }
            else
            {
                result.X = Mathf.Sqrt(innerSqrt + a2 - b2 + 3.0f) * InverseSqrt2;
            }

            if (p.Z == 0f)
            {
                result.Z = 0f;
            }
            else
            {
                result.Z = Mathf.Sqrt(innerSqrt - a2 + b2 + 3.0f) * InverseSqrt2;
            }

            if (result.X > 1.0f)
            {
                result.X = 1.0f;
            }
            if (result.Z > 1.0f)
            {
                result.Z = 1.0f;
            }

            if (p.X < 0)
            {
                result.X = -result.X;
            }
            if (p.Z < 0)
            {
                result.Z = -result.Z;
            }

            if (p.Y > 0)
            {
                // top face
                result.Y = 1.0f;
            }
            else
            {
                // bottom face
                result.Y = -1.0f;
            }
        }
        else if (fx >= fy && fx >= fz)
        {
            float a2 = p.Y * p.Y * 2.0f;
            float b2 = p.Z * p.Z * 2.0f;
            float inner = -a2 + b2 - 3f;
            float innerSqrt = -Mathf.Sqrt((inner * inner) - 12.0f * a2);

            if (p.Y == 0f)
            {
                result.Y = 0f;
            }
            else
            {
                result.Y = Mathf.Sqrt(innerSqrt + a2 - b2 + 3.0f) * InverseSqrt2;
            }

            if (p.Z == 0f)
            {
                result.Z = 0f;
            }
            else
            {
                result.Z = Mathf.Sqrt(innerSqrt - a2 + b2 + 3.0f) * InverseSqrt2;
            }

            if (result.Y > 1.0f)
            {
                result.Y = 1.0f;
            }
            if (result.Z > 1.0f)
            {
                result.Z = 1.0f;
            }

            if (p.Y < 0)
            {
                result.Y = -result.Y;
            }
            if (p.Z < 0)
            {
                result.Z = -result.Z;
            }

            if (p.X > 0)
            {
                // right face
                result.X = 1.0f;
            }
            else
            {
                // bottom face
                result.X = -1.0f;
            }
        }
        else
        {
            float a2 = p.X * p.X * 2.0f;
            float b2 = p.Y * p.Y * 2.0f;
            float inner = -a2 + b2 - 3f;
            float innerSqrt = -Mathf.Sqrt((inner * inner) - 12.0f * a2);

            if (p.X == 0f)
            {
                result.X = 0f;
            }
            else
            {
                result.X = Mathf.Sqrt(innerSqrt + a2 - b2 + 3.0f) * InverseSqrt2;
            }

            if (p.Y == 0f)
            {
                result.Y = 0f;
            }
            else
            {
                result.Y = Mathf.Sqrt(innerSqrt - a2 + b2 + 3.0f) * InverseSqrt2;
            }

            if (result.X > 1.0f)
            {
                result.X = 1.0f;
            }
            if (result.Y > 1.0f)
            {
                result.Y = 1.0f;
            }

            if (p.X < 0)
            {
                result.X = -result.X;
            }
            if (p.Y < 0)
            {
                result.Y = -result.Y;
            }

            if (p.Z > 0)
            {
                // front face
                result.Z = 1.0f;
            }
            else
            {
                // back face
                result.Z = -1.0f;
            }
        }

        return result;
    }

    public static Face GetFace(Vector3 point)
    {
        float max = Mathf.Max(
            Mathf.Max(Mathf.Abs(point.X), Mathf.Abs(point.Y)),
            Mathf.Abs(point.Z)
        );

        if (max == point.Y)
        {
            return Face.Top;
        }
        else if (-max == point.Y)
        {
            return Face.Bottom;
        }
        else if (max == point.X)
        {
            return Face.Right;
        }
        else if (-max == point.X)
        {
            return Face.Left;
        }
        else if (-max == point.Z)
        {
            return Face.Front;
        }
        else
        {
            return Face.Back;
        }
    }

    public static Face[] GetFaces(Face face)
    {
        var loc = Universe.CurrentLocation;
        var faces = new Face[4];
        faces[0] = face;
        var halfway = Universe.Planet.Resolution / 2f;
        switch (face)
        {
            case Face.Top:
                if (loc.X <= halfway)
                {
                    faces[1] = Face.Left;
                }
                else
                {
                    faces[1] = Face.Right;
                }
                if (loc.Y <= halfway)
                {
                    faces[2] = Face.Back;
                }
                else
                {
                    faces[2] = Face.Front;
                }
                break;
            case Face.Bottom:
                if (loc.X <= halfway)
                {
                    faces[1] = Face.Right;
                }
                else
                {
                    faces[1] = Face.Left;
                }
                if (loc.Y <= halfway)
                {
                    faces[2] = Face.Back;
                }
                else
                {
                    faces[2] = Face.Front;
                }
                break;
            case Face.Left:
                if (loc.X <= halfway)
                {
                    faces[1] = Face.Bottom;
                }
                else
                {
                    faces[1] = Face.Top;
                }
                if (loc.Y <= halfway)
                {
                    faces[2] = Face.Back;
                }
                else
                {
                    faces[2] = Face.Front;
                }
                break;
            case Face.Right:
                if (loc.X <= halfway)
                {
                    faces[1] = Face.Top;
                }
                else
                {
                    faces[1] = Face.Bottom;
                }
                if (loc.Y <= halfway)
                {
                    faces[2] = Face.Front;
                }
                else
                {
                    faces[2] = Face.Back;
                }
                break;
            case Face.Front:
                if (loc.X <= halfway)
                {
                    faces[1] = Face.Top;
                }
                else
                {
                    faces[1] = Face.Bottom;
                }
                if (loc.Y <= halfway)
                {
                    faces[2] = Face.Left;
                }
                else
                {
                    faces[2] = Face.Right;
                }
                break;
            case Face.Back:
                if (loc.X <= halfway)
                {
                    faces[1] = Face.Bottom;
                }
                else
                {
                    faces[1] = Face.Top;
                }
                if (loc.Y <= halfway)
                {
                    faces[2] = Face.Left;
                }
                else
                {
                    faces[2] = Face.Right;
                }
                break;
        }
        //GD.Print(faces[0] + " " + faces[1] + " " + faces[2]);
        return faces;
    }

    // Returns a number from -amount to +amount excluding 0;
    public static int Offset(int amount)
    {
        var offset = Universe.Random.RandiRange(1, amount * 2) - amount;
        if (offset <= 0)
        {
            offset -= 1;
        }
        return offset;
    }

    public static Vector3 RandomPointOnUnitSphere()
    {
        return new Vector3(
            Universe.Random.Randfn(0, 1),
            Universe.Random.Randfn(0, 1),
            Universe.Random.Randfn(0, 1)
        ).Normalized();
    }

    public static void ApplyBodyToVelocity(
        HeavenlyBody thisBody,
        HeavenlyBody otherBody,
        float mass,
        float radius,
        float timeStep,
        bool inverse = false
    )
    {
        Vector3 distance = otherBody.Transform.Origin - thisBody.Transform.Origin;
        float sqrDist = distance.LengthSquared();
        Vector3 forceDir = distance.Normalized();
        Vector3 force = forceDir * otherBody.Gravity * mass / sqrDist;
        Vector3 acceleration = force.Normalized();
        if (inverse)
        {
            acceleration = -acceleration;
        }
        if (!Mathf.IsNaN(acceleration.Length()))
        {
            if (radius == 0)
            {
                thisBody.CurrentVelocity += acceleration * timeStep * 10f;
            }
            else
            {
                thisBody.CurrentVelocity += acceleration * timeStep;
            }
        }
    }

    // from the mac os colourpicker
    public static string[] Crayons = new[]
    {
        "#fc6fcf",
        "#fc66ff",
        "#c6f",
        "#66f",
        "#6cf",
        "#6ff",
        "#6fc",
        "#6f6",
        "#cf6",
        "#ff6",
        "#fecc66",
        "#fc6666",
        "#fb0207",
        "#fd8008",
        "#ffff0a",
        "#80ff08",
        "#21ff06",
        "#21ff80",
        "#21ffff",
        "#0f80ff",
        "#00f",
        "#8000ff",
        "#fb02ff",
        "#fb0280",
        "#800040",
        "#800080",
        "#400080",
        "#000080",
        "#074080",
        "#108080",
        "#118040",
        "#118002",
        "#408002",
        "#808004",
        "#804003",
        "#804003",
        "#800002",
        "#000",
        "#191919",
        "#333",
        "#4c4c4c",
        "#666",
        "#7f7f7f",
        "#808080",
        "#999",
        "#b3b3b3",
        "#ccc",
        "#e6e6e6",
        "#fff"
    };
}
