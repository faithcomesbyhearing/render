namespace Render.Resources
{
    public static class ElementExtensions
    {
        public static Point GetScreenCoords(this VisualElement view, bool includeSystemBar = false)
        {
            var result = new Point(view.X, view.Y);
            while (view.Parent is VisualElement parent)
            {
                result = result.Offset(parent.X, parent.Y);
                view = parent;
            }
            return includeSystemBar ? new Point(result.X, result.Y) : result;
        }

        public static T FindParentPage<T>(this Element view)
            where T : Page
        {
            return view.FindParentElement<T>();
        }

        public static T FindParentElement<T>(this Element view)
            where T : Element
        {

            if (view is T element)
            {
                return element;
            }

            if (view.Parent == null)
            {
                return null;
            }

            return view.Parent.FindParentElement<T>();
        }
    }
}