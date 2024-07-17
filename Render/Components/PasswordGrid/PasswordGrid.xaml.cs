using DynamicData;
using Microsoft.Maui.Controls.Shapes;
using ReactiveUI;
using Render.Kernel.TouchActions;
using Render.Resources;
using Render.Resources.Styles;

namespace Render.Components.PasswordGrid
{
    public partial class PasswordGrid
    {
        private const int TouchOffset = 20;

        private static List<Grid> _gridSpots;
        
        private static Color _fillColor =((ColorReference)ResourceExtensions.GetResourceValue("PasswordGridDot")) ?? new ColorReference();

        private Brush _dotBrush = new SolidColorBrush(_fillColor);

        public PasswordGrid()
        {
            InitializeComponent();
            this.WhenActivated(d =>
            {
                d(this.WhenAnyValue(x => x.ViewModel.IncorrectPattern).Subscribe(SetGridColor));
                d(this.WhenAnyValue(x => x.ViewModel.Password)
                    .Subscribe(HighlightGridSpots));
            });

            SizeChanged += OnSizeChanged;
        }

        private void OnSizeChanged(object sender, EventArgs e)
        {
            HighlightGridSpots(ViewModel?.Password);
        }

        private void TouchEffect_OnTouchAction(object sender, TouchActionEventArgs args)
        {
            if (args.IsInContact)
            {
                var position = args.Location;
                var point = Grid.Children.FirstOrDefault(x =>
                    x.Frame.Center.X >= position.X - TouchOffset &&
                    x.Frame.Center.X <= position.X + TouchOffset &&
                    x.Frame.Center.Y >= position.Y - TouchOffset &&
                    x.Frame.Center.Y <= position.Y + TouchOffset);
                if (point == null)
                    return;
                var hex = point.AutomationId;
                if (ViewModel != null)
                {
                    if (ViewModel.IncorrectPattern)
                    {
                        ViewModel.ResetPassword();   
                    }
                    ViewModel.ResetValidation();
                    ViewModel?.AddToPassword(hex);
                }

                LogInfo("Password Grid Item Selected");
            }
        }

        private void HighlightGridSpots(string password)
        {
            if (password == null) return;
            var previousLines = AbsoluteLayout.Children.Where(x => x is Line);
            AbsoluteLayout.Children.RemoveMany(previousLines);
            _gridSpots = new List<Grid>();
            for (var i = 0; i < password.Length; i++)
            {
                var spotString = password.Substring(i, 2);
                var spot = (Grid)Grid.Children.FirstOrDefault(x => x.AutomationId == spotString);
                if (spot != null)
                {
                    _gridSpots.Add(spot);
                }

                i++;
            }
            SetLineColor(_fillColor);
        }

        private void SetGridColor(bool incorrectPattern)
        {
            if (incorrectPattern)
            {
                _fillColor = ((ColorReference)ResourceExtensions.GetResourceValue("Error")) ??
                                      new ColorReference();
                _dotBrush = new SolidColorBrush(_fillColor);
                SetGridSpotsColor(_dotBrush);
                SetLineColor(_fillColor);
            }
            else
            {
                _fillColor = ((ColorReference)ResourceExtensions.GetResourceValue("PasswordGridDot")) ??
                                      new ColorReference();
                _dotBrush = new SolidColorBrush(_fillColor);
                SetGridSpotsColor(_dotBrush);
            }
        }

        private void SetGridSpotsColor(Brush dotBrush)
        {
            Ellipse1.Fill = dotBrush;
            Ellipse2.Fill = dotBrush;
            Ellipse3.Fill = dotBrush;
            Ellipse4.Fill = dotBrush;
            Ellipse5.Fill = dotBrush;
            Ellipse6.Fill = dotBrush;
            Ellipse7.Fill = dotBrush;
            Ellipse8.Fill = dotBrush;
            Ellipse9.Fill = dotBrush;
            Ellipse10.Fill = dotBrush;
            Ellipse11.Fill = dotBrush;
            Ellipse12.Fill = dotBrush;
            Ellipse13.Fill = dotBrush;
            Ellipse14.Fill = dotBrush;
            Ellipse15.Fill = dotBrush;
            Ellipse16.Fill = dotBrush;
        }
        private void SetLineColor(Color lineColor)
        {
            for (var i = 0; i < _gridSpots.Count - 1; i++)
            {
                var thisSpot = _gridSpots[i];
                var nextSpot = _gridSpots[i + 1];
                var startLoc = thisSpot.Bounds.Center;
                var endLoc = nextSpot.Bounds.Center;
                var line = new Line
                {
                    Stroke = new SolidColorBrush(lineColor),
                    StrokeThickness = 2,
                    X1 = startLoc.X,
                    Y1 = startLoc.Y,
                    X2 = endLoc.X,
                    Y2 = endLoc.Y
                };
                AbsoluteLayout.Children.Add(line);
            }
        }
    }
}