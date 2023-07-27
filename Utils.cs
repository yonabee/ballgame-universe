
    using Godot;
    
    public static class Utils {

        public enum Face { All, Top, Bottom, Left, Right, Front, Back }

        public static string[] Crayons = new[] {
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

        public static Vector3 CubeToSphere(Vector3 point)
        {
            return point.Normalized();
        }

        public static Vector3 SphereToCube(Vector3 point)
        {
            Vector3 norm = point.Normalized();
            float max = Mathf.Max(
                Mathf.Max(Mathf.Abs(norm.X), Mathf.Abs(norm.Y)), 
                Mathf.Abs(norm.Z)
            );
            return new Vector3(norm.X/max, norm.Y/max, norm.Z/max);
        }

        public static Face GetFace(Vector3 point)
        {
            float max = Mathf.Max(
                Mathf.Max(Mathf.Abs(point.X), Mathf.Abs(point.Y)), 
                Mathf.Abs(point.Z)
            );

            if (max == point.Y) {
                return Face.Top;
            }
            else if (-max == point.Y) {
                return Face.Bottom;
            }
            else if (max == point.X) {
                return Face.Right;
            }
            else if (-max == point.X) {
                return Face.Left;
            }
            else if (-max == point.Z) {
                return Face.Front;
            }
            else {
                return Face.Back;
            }
        }
    }