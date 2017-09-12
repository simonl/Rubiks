namespace Graphics
{
    public enum Axis
    {
        X,
        Y,
        Z,
    }

    public static class Axes
    {
        public static Axis Cycle(this Axis axis)
        {
            return (Axis) (((int) axis + 1) % 3);
        }
    }
}