
    using Godot;
    
    public static class Utils {

        public enum Face { All, Top, Bottom, Left, Right, Front, Back }

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
            else if (max == point.Z) {
                return Face.Front;
            }
            else {
                return Face.Back;
            }
        }
    }