using Render.Resources.Localization;

namespace Render.Components.VersionLabel;

public partial class VersionLabel : ContentView
{
	public VersionLabel()
	{
		InitializeComponent();

        var versionLabel = string.Format($"{AppResources.Version} {Kernel.Version.SoftwareVersion}");
        VersionTextLabel.Text = versionLabel;
    }
}