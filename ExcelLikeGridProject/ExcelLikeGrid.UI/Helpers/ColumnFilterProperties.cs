using System.Windows;

namespace ExcelLikeGrid.UI.Helpers
{
    public static class ColumnFilterProperties
    {
        public static readonly DependencyProperty HasActiveFilterProperty =
            DependencyProperty.RegisterAttached(
                "HasActiveFilter",
                typeof(bool),
                typeof(ColumnFilterProperties),
                new FrameworkPropertyMetadata(false));

        public static void SetHasActiveFilter(DependencyObject element, bool value)
        {
            element.SetValue(HasActiveFilterProperty, value);
        }

        public static bool GetHasActiveFilter(DependencyObject element)
        {
            return (bool)element.GetValue(HasActiveFilterProperty);
        }
    }
}