using Render.Sequencer.Views.Flags.Base;
using Render.Sequencer.Core.Utils.Extensions;

namespace Render.Sequencer.Views.Flags;

/// <summary>
/// Rectangular flag with icon and value
/// </summary>
public partial class MarkerFlagView : BaseFlagView
{
    // Need to manage position if marker is close to border 
    public const double HalfOfMarkerWidth = 21;
    public const double DefaultWidth = 42;

    public MarkerFlagView()
	{
		InitializeComponent();
	}
}