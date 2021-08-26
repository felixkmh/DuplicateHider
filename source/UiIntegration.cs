using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace DuplicateHider
{
    class UiIntegration
    {
        public static IEnumerable<FrameworkElement> FindVisualChildren(DependencyObject parent, string name = null)
        {
            Stack<DependencyObject> stack = new Stack<DependencyObject>();
            stack.Push(parent);
            while (stack.Count > 0)
            {
                var current = stack.Pop();
                var count = VisualTreeHelper.GetChildrenCount(current);
                for (int i = 0; i < count; i++)
                {
                    var child = VisualTreeHelper.GetChild(current, i);
                    if (child is FrameworkElement element)
                    {
                        if (string.IsNullOrEmpty(name) || element.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                        {
                            yield return element;
                        }
                    }
                    stack.Push(child);
                }
            }
        }

        public static IEnumerable<SearchType> FindVisualChildren<SearchType>(DependencyObject parent, string name = null)
            where SearchType : FrameworkElement
        {
            return FindVisualChildren(parent, name).OfType<SearchType>();
        }
    }
}
