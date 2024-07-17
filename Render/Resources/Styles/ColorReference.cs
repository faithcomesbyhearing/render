namespace Render.Resources.Styles
{
    public class ColorReference
    {
        public Color Color { get; set; }

        public static implicit operator Color(ColorReference colorReference) => colorReference.Color;
    }
}