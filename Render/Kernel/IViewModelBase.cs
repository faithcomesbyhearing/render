namespace Render.Kernel
{
    public interface IViewModelBase : IDisposable
    {
        FlowDirection FlowDirection { get; }
    }
}