// WpfMvvmApp/Views/ContractView.xaml.cs
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WpfMvvmApp.ViewModels; // NECESSARIO per MainViewModel

namespace WpfMvvmApp.Views
{
    public partial class ContractView : UserControl
    {
        public ContractView()
        {
            InitializeComponent();
        }

        // Gestore per deselezionare lezioni (esistente)
        private void UserControl_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var originalSource = e.OriginalSource as DependencyObject;
            if (originalSource == null) return;

            // Trova LessonView e poi la ListView interna
            LessonView? lessonView = FindVisualChild<LessonView>(this);
            ListView? lessonsListView = (lessonView != null) ? FindVisualChild<ListView>(lessonView, "LessonsListView") : null;

            if (lessonsListView == null) return;

            bool clickIsInsideLessonsListView = IsDescendantOf(originalSource, lessonsListView);

            if (!clickIsInsideLessonsListView)
            {
                lessonsListView.UnselectAll();
            }
        }

        // NUOVO: Gestore per il click sull'header della GridView dei Contratti
        private void ContractsListView_HeaderClick(object sender, RoutedEventArgs e)
        {
            // Verifica che l'evento sia originato da un header di colonna
            if (e.OriginalSource is GridViewColumnHeader headerClicked)
            {
                // Assicurati che l'header abbia un Tag (che contiene il nome della proprietà)
                if (headerClicked.Tag is string propertyName)
                {
                    // Ottieni il MainViewModel associato a questa View (DataContext dell'UserControl)
                    if (this.DataContext is MainViewModel viewModel)
                    {
                        // Esegui il comando di ordinamento nel MainViewModel, passando il nome della proprietà
                        if (viewModel.SortContractsCommand.CanExecute(propertyName))
                        {
                            viewModel.SortContractsCommand.Execute(propertyName);
                        }
                    }
                }
            }
        }


        // --- Helper per navigare l'Albero Visuale (invariati) ---
        private static T? FindVisualChild<T>(DependencyObject parent, string? childName = null) where T : DependencyObject
        {
            // ... (codice helper esistente) ...
             if (parent == null) return null;

            T? foundChild = null;

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (!(child is T childType))
                {
                    foundChild = FindVisualChild<T>(child, childName);
                    if (foundChild != null) break;
                }
                else
                {
                    if (string.IsNullOrEmpty(childName) ||
                        (child is FrameworkElement frameworkElement && frameworkElement.Name == childName))
                    {
                        foundChild = childType;
                        break;
                    }
                    else
                    {
                         foundChild = FindVisualChild<T>(child, childName);
                         if (foundChild != null) break;
                    }
                }
            }
            return foundChild;
        }

        private static bool IsDescendantOf(DependencyObject? element, DependencyObject? ancestor)
        {
            // ... (codice helper esistente) ...
             if (element == null || ancestor == null) return false;

            DependencyObject? parent = VisualTreeHelper.GetParent(element);
            while (parent != null)
            {
                if (parent == ancestor)
                {
                    return true;
                }
                parent = VisualTreeHelper.GetParent(parent);
            }
            return false;
        }
    }
}