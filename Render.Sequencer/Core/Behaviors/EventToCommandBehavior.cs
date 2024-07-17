using System.Globalization;
using System.Reflection;
using System.Windows.Input;

namespace Render.Sequencer.Core.Behaviors;

public class EventToCommandBehavior : BaseBehavior<VisualElement>
{
    public static readonly BindableProperty EventNameProperty = BindableProperty.Create(
        propertyName: nameof(EventName),
        returnType: typeof(string),
        declaringType: typeof(EventToCommandBehavior),
        propertyChanged: OnEventNamePropertyChanged);

    public static readonly BindableProperty CommandProperty = BindableProperty.Create(
        propertyName: nameof(Command),
        returnType: typeof(ICommand),
        declaringType: typeof(EventToCommandBehavior));

    public static readonly BindableProperty CommandParameterProperty = BindableProperty.Create(
        propertyName: nameof(CommandParameter),
        returnType: typeof(object),
        declaringType: typeof(EventToCommandBehavior));

    public static readonly BindableProperty EventArgsConverterProperty = BindableProperty.Create(
        propertyName: nameof(EventArgsConverter),
        returnType: typeof(IValueConverter),
        declaringType: typeof(EventToCommandBehavior));

    private static void OnEventNamePropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        ((EventToCommandBehavior)bindable).RegisterEvent();
    }

    private readonly MethodInfo _eventHandlerMethodInfo = typeof(EventToCommandBehavior)
        .GetTypeInfo()
        .GetDeclaredMethod(nameof(OnTriggerHandled)) ?? throw new InvalidOperationException($"Cannot find method {nameof(OnTriggerHandled)}");

    private Delegate? _eventHandler;
    private EventInfo? _eventInfo;

    public string? EventName
    {
        get => (string?)GetValue(EventNameProperty);
        set => SetValue(EventNameProperty, value);
    }

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public IValueConverter? EventArgsConverter
    {
        get => (IValueConverter?)GetValue(EventArgsConverterProperty);
        set => SetValue(EventArgsConverterProperty, value);
    }

    protected override void OnAttachedTo(VisualElement bindable)
    {
        base.OnAttachedTo(bindable);

        RegisterEvent();
    }

    protected override void OnDetachingFrom(VisualElement bindable)
    {
        UnregisterEvent();

        base.OnDetachingFrom(bindable);
    }


    private void RegisterEvent()
    {
        UnregisterEvent();

        var eventName = EventName;
        if (View is null || string.IsNullOrWhiteSpace(eventName))
        {
            return;
        }

        _eventInfo = View.GetType().GetRuntimeEvent(eventName) ??
            throw new ArgumentException($"{nameof(EventToCommandBehavior)}: Couldn't resolve the event.", nameof(EventName));

        ArgumentNullException.ThrowIfNull(_eventInfo.EventHandlerType);
        ArgumentNullException.ThrowIfNull(_eventHandlerMethodInfo);

        _eventHandler = _eventHandlerMethodInfo.CreateDelegate(_eventInfo.EventHandlerType, this) ??
            throw new ArgumentException($"{nameof(EventToCommandBehavior)}: Couldn't create event handler.", nameof(EventName));

        _eventInfo.AddEventHandler(View, _eventHandler);
    }

    private void UnregisterEvent()
    {
        if (_eventInfo is not null && _eventHandler is not null)
        {
            _eventInfo.RemoveEventHandler(View, _eventHandler);
        }

        _eventInfo = null;
        _eventHandler = null;
    }

    /// <summary>
    /// Virtual method that executes when a Command is invoked
    /// </summary>
    [Microsoft.Maui.Controls.Internals.Preserve(Conditional = true)]
    protected virtual void OnTriggerHandled(object? sender = null, object? eventArgs = null)
    {
        var parameter = CommandParameter ?? EventArgsConverter?.Convert(eventArgs, typeof(object), null, CultureInfo.InvariantCulture);
        var command = Command;

        if (command?.CanExecute(parameter) ?? false)
        {
            command.Execute(parameter);
        }
    }
}