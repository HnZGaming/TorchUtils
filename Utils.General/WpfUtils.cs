using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace Utils.General
{
    internal static class WpfUtils
    {
        public static IEnumerable<DependencyObject> GetDeepChildren(this DependencyObject root)
        {
            var count = VisualTreeHelper.GetChildrenCount(root);
            for (var i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                yield return child;

                foreach (var grandChild in child.GetDeepChildren())
                {
                    yield return grandChild;
                }
            }
        }
    }
}