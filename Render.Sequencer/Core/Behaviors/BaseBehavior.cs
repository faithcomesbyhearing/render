using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Render.Sequencer.Core.Behaviors;

public abstract class BaseBehavior<TView> : Behavior<TView> where TView : VisualElement
{
    private static readonly MethodInfo? _getContextMethod = typeof(BindableObject)
        .GetRuntimeMethods()
        .FirstOrDefault(m => m.Name is "GetContext");

    private static readonly FieldInfo? _bindingField = _getContextMethod?
        .ReturnType
        .GetRuntimeField("Binding");

    private BindingBase? _defaultBindingContextBinding;

    /// <summary>
    /// View used by the Behavior
    /// </summary>
    protected TView? View { get; private set; }

    [MemberNotNullWhen(true, nameof(_defaultBindingContextBinding))]
    internal bool TrySetBindingContext(Binding binding)
    {
        if (!IsBound(BindingContextProperty))
        {
            SetBinding(BindingContextProperty, _defaultBindingContextBinding = binding);
            return true;
        }

        return false;
    }

    internal bool TryRemoveBindingContext()
    {
        if (_defaultBindingContextBinding is null)
        {
            return false;
        }

        RemoveBinding(BindingContextProperty);
        _defaultBindingContextBinding = null;

        return true;
    }

    protected virtual void OnViewPropertyChanged(TView sender, PropertyChangedEventArgs e) { }

    [MemberNotNull(nameof(View))]
    protected override void OnAttachedTo(TView bindable)
    {
        base.OnAttachedTo(bindable);

        View = bindable;
        bindable.PropertyChanged += OnViewPropertyChanged;

        TrySetBindingContext(new Binding
        {
            Path = BindingContextProperty.PropertyName,
            Source = bindable
        });
    }

    protected override void OnDetachingFrom(TView bindable)
    {
        base.OnDetachingFrom(bindable);

        TryRemoveBindingContext();

        bindable.PropertyChanged -= OnViewPropertyChanged;

        View = null;
    }

    /// <summary>
    /// Virtual method that executes when a binding context is set
    /// </summary>
    [MemberNotNullWhen(true, nameof(_bindingField), nameof(_getContextMethod))]
    protected bool IsBound(BindableProperty property, BindingBase? defaultBinding = null)
    {
        var context = _getContextMethod?.Invoke(this, new object[] { property });

        return context is not null
            && _bindingField?.GetValue(context) is BindingBase binding
            && binding != defaultBinding;
    }

    private void OnViewPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not TView view)
        {
            throw new ArgumentException($"Behavior can only be attached to {typeof(TView)}");
        }

        try
        {
            OnViewPropertyChanged(view, e);
        }
        catch (Exception exception) 
        {
            Debug.WriteLine(exception);
        }
    }
}