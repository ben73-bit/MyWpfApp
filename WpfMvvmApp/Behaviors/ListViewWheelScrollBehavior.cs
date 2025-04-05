// WpfMvvmApp/Behaviors/ListViewWheelScrollBehavior.cs
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace WpfMvvmApp.Behaviors
{
    public static class ListViewWheelScrollBehavior
    {
        // Definisce la proprietà associata 'IsEnabled'
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(ListViewWheelScrollBehavior),
                new UIPropertyMetadata(false, OnIsEnabledChanged)); // Callback quando il valore cambia

        // Metodo Get per la proprietà associata (necessario per XAML)
        public static bool GetIsEnabled(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsEnabledProperty);
        }

        // Metodo Set per la proprietà associata (necessario per XAML)
        public static void SetIsEnabled(DependencyObject obj, bool value)
        {
            obj.SetValue(IsEnabledProperty, value);
        }

        // Chiamato quando il valore della proprietà associata 'IsEnabled' cambia
        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ListView listView)
            {
                bool wasEnabled = (bool)e.OldValue;
                bool isEnabled = (bool)e.NewValue;

                // Rimuovi il gestore precedente se era abilitato
                if (wasEnabled)
                {
                    listView.PreviewMouseWheel -= OnListViewPreviewMouseWheel;
                }

                // Aggiungi il nuovo gestore se è abilitato
                if (isEnabled)
                {
                    listView.PreviewMouseWheel += OnListViewPreviewMouseWheel;
                }
            }
        }

        // Il gestore effettivo dell'evento PreviewMouseWheel
        private static void OnListViewPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Stessa logica di prima
            if (sender is ListView listView && !e.Handled)
            {
                var scrollViewer = FindVisualChild<ScrollViewer>(listView);
                if (scrollViewer != null && scrollViewer.ScrollableHeight > 0)
                {
                    if (e.Delta > 0) { scrollViewer.LineUp(); }
                    else { scrollViewer.LineDown(); }
                    e.Handled = true; // Marca come gestito
                }
                // Se scrollViewer.ScrollableHeight <= 0, non facciamo nulla e non marchiamo come gestito
            }
        }

        // Helper FindVisualChild (identico a prima)
        private static T? FindVisualChild<T>(DependencyObject? parent) where T : DependencyObject
        {
            if (parent == null) return null;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child is T tChild) { return tChild; }
                else { T? childOfChild = FindVisualChild<T>(child); if (childOfChild != null) return childOfChild; }
            }
            return null;
        }
    }
}