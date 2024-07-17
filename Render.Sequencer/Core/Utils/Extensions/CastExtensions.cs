namespace Render.Sequencer.Core.Utils.Extensions;

internal static class CastExtensions
{
    public static TInterface? As<TInterface>(this object value) where TInterface : class
    {
        return value is TInterface ? (TInterface)value : null;
    }

    public static TInterface Cast<TInterface>(this object value)
    {
        return (TInterface)value;
    }
}