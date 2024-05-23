using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace StartGGgraphicGenerator
{
    public static class TextBoxWatermarkHelper
    {
        public static readonly DependencyProperty WatermarkProperty =
            DependencyProperty.RegisterAttached("Watermark", typeof(string), typeof(TextBoxWatermarkHelper), new PropertyMetadata(default(string), OnWatermarkChanged));

        public static void SetWatermark(DependencyObject element, string value)
        {
            element.SetValue(WatermarkProperty, value);
        }

        public static string GetWatermark(DependencyObject element)
        {
            return (string)element.GetValue(WatermarkProperty);
        }

        private static void OnWatermarkChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox textBox)
            {
                textBox.GotFocus += RemoveWatermark;
                textBox.LostFocus += ShowWatermark;
                ShowWatermark(textBox, null);
            }
        }

        private static void ShowWatermark(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && string.IsNullOrEmpty(textBox.Text))
            {
                textBox.Text = GetWatermark(textBox);
                textBox.Foreground = Brushes.Gray;
            }
        }

        private static void RemoveWatermark(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && textBox.Text == GetWatermark(textBox))
            {
                textBox.Text = string.Empty;
                textBox.Foreground = Brushes.Black;
            }
        }
    }
}
