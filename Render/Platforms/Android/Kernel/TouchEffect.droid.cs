using Android.Views;
using Microsoft.Maui.Controls.Platform;
using Microsoft.Maui.Platform;
using Render.Kernel.TouchActions;
using View = Android.Views.View;

namespace Render.Platforms.Kernel
{
    public class TouchEffect : PlatformEffect
    {
        private static Dictionary<View, TouchEffect> viewDictionary = new ();
        private static Dictionary<int, TouchEffect> idToEffectDictionary = new ();

        private View _view;
        private Element _formsElement;
        private Render.Kernel.TouchActions.TouchEffect _libTouchEffect;
        private bool _capture;
        private Func<double, double> _fromPixels;
        private readonly int[] twoIntArray = new int[2];

        protected override void OnAttached()
        {
            // Get the Android View corresponding to the Element that the effect is attached to
            _view = Control == null ? Container : Control;

            // Get access to the TouchEffect class in the .NET Standard library
            var touchEffect = (Render.Kernel.TouchActions.TouchEffect)Element
                .Effects
                .FirstOrDefault(effect => effect is Render.Kernel.TouchActions.TouchEffect);

            if (touchEffect != null && _view != null)
            {
                viewDictionary.Add(_view, this);

                _formsElement = Element;

                _libTouchEffect = touchEffect;

                // Save fromPixels function
                _fromPixels = _view.Context.FromPixels;

                // Set event handler on View
                _view.Touch += OnTouch;
            }
        }

        protected override void OnDetached()
        {
            if (viewDictionary.ContainsKey(_view))
            {
                viewDictionary.Remove(_view);
                _view.Touch -= OnTouch;
            }
        }

        private void OnTouch(object sender, View.TouchEventArgs args)
        {
            // Two object common to all the events
            View senderView = sender as View;
            MotionEvent motionEvent = args.Event;

            // Get the pointer index
            int pointerIndex = motionEvent.ActionIndex;

            // Get the id that identifies a finger over the course of its progress
            int id = motionEvent.GetPointerId(pointerIndex);


            senderView.GetLocationOnScreen(twoIntArray);
            Point screenPointerCoords = new Point(twoIntArray[0] + motionEvent.GetX(pointerIndex),
                                                  twoIntArray[1] + motionEvent.GetY(pointerIndex));


            // Use ActionMasked here rather than Action to reduce the number of possibilities
            switch (args.Event.ActionMasked)
            {
                case MotionEventActions.Down:
                case MotionEventActions.PointerDown:
                    FireEvent(this, id, TouchActionType.Pressed, screenPointerCoords, true);

                    idToEffectDictionary.Add(id, this);

                    _capture = _libTouchEffect.Capture;
                    break;

                case MotionEventActions.Move:
                    // Multiple Move events are bundled, so handle them in a loop
                    for (pointerIndex = 0; pointerIndex < motionEvent.PointerCount; pointerIndex++)
                    {
                        id = motionEvent.GetPointerId(pointerIndex);

                        if (_capture)
                        {
                            senderView.GetLocationOnScreen(twoIntArray);

                            screenPointerCoords = new Point(twoIntArray[0] + motionEvent.GetX(pointerIndex),
                                                            twoIntArray[1] + motionEvent.GetY(pointerIndex));

                            FireEvent(this, id, TouchActionType.Moved, screenPointerCoords, true);
                        }
                        else
                        {
                            CheckForBoundaryHop(id, screenPointerCoords);

                            if (idToEffectDictionary[id] != null)
                            {
                                FireEvent(idToEffectDictionary[id], id, TouchActionType.Moved, screenPointerCoords, true);
                            }
                        }
                    }
                    break;

                case MotionEventActions.Up:
                case MotionEventActions.Pointer1Up:
                    if (_capture)
                    {
                        FireEvent(this, id, TouchActionType.Released, screenPointerCoords, false);
                    }
                    else
                    {
                        CheckForBoundaryHop(id, screenPointerCoords);

                        if (idToEffectDictionary[id] != null)
                        {
                            FireEvent(idToEffectDictionary[id], id, TouchActionType.Released, screenPointerCoords, false);
                        }
                    }
                    idToEffectDictionary.Remove(id);
                    break;

                case MotionEventActions.Cancel:
                    if (_capture)
                    {
                        FireEvent(this, id, TouchActionType.Cancelled, screenPointerCoords, false);
                    }
                    else
                    {
                        if (idToEffectDictionary[id] != null)
                        {
                            FireEvent(idToEffectDictionary[id], id, TouchActionType.Cancelled, screenPointerCoords, false);
                        }
                    }
                    idToEffectDictionary.Remove(id);
                    break;
            }
        }

        private void CheckForBoundaryHop(int id, Point pointerLocation)
        {
            TouchEffect touchEffectHit = null;

            foreach (View view in viewDictionary.Keys)
            {
                // Get the view rectangle
                try
                {
                    view.GetLocationOnScreen(twoIntArray);
                }
                catch // System.ObjectDisposedException: Cannot access a disposed object.
                {
                    continue;
                }

                var viewRect = new Rect(twoIntArray[0], twoIntArray[1], view.Width, view.Height);
                if (viewRect.Contains(pointerLocation))
                {
                    touchEffectHit = viewDictionary[view];
                }
            }

            if (touchEffectHit != idToEffectDictionary[id])
            {
                if (idToEffectDictionary[id] != null)
                {
                    FireEvent(idToEffectDictionary[id], id, TouchActionType.Exited, pointerLocation, true);
                }
                if (touchEffectHit != null)
                {
                    FireEvent(touchEffectHit, id, TouchActionType.Entered, pointerLocation, true);
                }
                idToEffectDictionary[id] = touchEffectHit;
            }
        }

        private void FireEvent(TouchEffect touchEffect, int id, TouchActionType actionType, Point pointerLocation, bool isInContact)
        {
            // Get the method to call for firing events
            Action<Element, TouchActionEventArgs> onTouchAction = touchEffect._libTouchEffect.OnTouchAction;

            // Get the location of the pointer within the view
            touchEffect._view.GetLocationOnScreen(twoIntArray);
            double x = pointerLocation.X - twoIntArray[0];
            double y = pointerLocation.Y - twoIntArray[1];
            Point point = new Point(_fromPixels(x), _fromPixels(y));

            // Call the method
            onTouchAction(touchEffect._formsElement, new TouchActionEventArgs(id, actionType, point, isInContact));
        }
    }
}