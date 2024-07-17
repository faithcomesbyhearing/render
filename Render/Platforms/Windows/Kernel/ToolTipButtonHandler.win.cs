using Microsoft.Maui.Handlers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Render.Kernel.CustomRenderer;
using Button = Microsoft.UI.Xaml.Controls.Button;
using Color = Windows.UI.Color;
using HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment;
using SolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;


namespace Render.Platforms.Kernel;

public class ToolTipButtonHandler : ButtonHandler
{
    private TeachingTip _toolTip;
    private ToolTipButton _toolTipButton;

    static ToolTipButtonHandler()
    {
        Mapper.AppendToMapping(nameof(ToolTipButton.IsToolTipOpened), IsOpenedChanged);
        Mapper.AppendToMapping(nameof(ToolTipButton.ToolTipText), IsContentChanged);
    }

    private static void IsOpenedChanged(IButtonHandler toolTipButtonHandler, IButton toolTipButton)
    {
        if (toolTipButton is ToolTipButton tipButton && toolTipButtonHandler is ToolTipButtonHandler tipButtonHandler)
        {
            tipButtonHandler.UpdateToolTipStatus(tipButton);
        }
    }
    
    private static void IsContentChanged(IButtonHandler toolTipButtonHandler, IButton toolTipButton)
    {
        if (toolTipButton is ToolTipButton tipButton && toolTipButtonHandler is ToolTipButtonHandler tipButtonHandler)
        {
            tipButtonHandler.SetContent(tipButton);
        }
    }

    private void UpdateToolTipStatus(ToolTipButton toolTipButton)
    {
        _toolTipButton = toolTipButton;
        _toolTip.IsOpen = toolTipButton.IsToolTipOpened;
    }
    
    private void SetContent(ToolTipButton toolTipButton)
    {
        if(toolTipButton.ToolTipText is null) return;
        
        var textBox = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Text = toolTipButton.ToolTipText.Text,
            Foreground = new SolidColorBrush(Color.FromArgb(255, 95, 105, 109)),
            FontSize = toolTipButton.ToolTipText.FontSize,
            FontFamily = new FontFamily(toolTipButton.ToolTipText.FontFamily)
        };
        
        _toolTip.Content = textBox;
    }
    
    protected override void ConnectHandler(Button platformView)
    {
        base.ConnectHandler(platformView);
        
        _toolTip = new TeachingTip
        {
            Target = platformView,
            PreferredPlacement = TeachingTipPlacementMode.TopLeft,
            HorizontalAlignment = HorizontalAlignment.Left,
            IsLightDismissEnabled = true,
        };

        _toolTip.Closed += (sender, args) =>
        {
            _toolTipButton.IsToolTipOpened = _toolTip.IsOpen;
        };
        
        platformView.Resources.Add("ToolTip", _toolTip);
        
    }
}