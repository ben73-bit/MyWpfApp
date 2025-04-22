// WpfMvvmApp/Views/ContractView.xaml.cs
using System.Diagnostics; // Aggiunto per Debug.WriteLine
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

        // Gestore per deselezionare lezioni - LOGICA INTERNA COMMENTATA PER DEBUG
        private void UserControl_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // Aggiunto log per vedere se viene chiamato, ma la logica è commentata
            Debug.WriteLine($"--- UserControl_PreviewMouseDown Called (Source: {e.OriginalSource.GetType().Name}) ---");

            /* // <<== COMMENTATO DA QUI
            var originalSource = e.OriginalSource as DependencyObject;
            if (originalSource == null) return;

            // Trova LessonView e poi la ListView interna
            LessonView? lessonView = FindVisualChild<LessonView>(this);
            ListView? lessonsListView = (lessonView != null) ? FindVisualChild<ListView>(lessonView, "LessonsListView") : null;

            if (lessonsListView == null)
            {
                 Debug.WriteLine("    PreviewMouseDown: LessonsListView not found."); // DEBUG
                 return;
            }

            bool clickIsInsideLessonsListView = IsDescendantOf(originalSource, lessonsListView);

            if (!clickIsInsideLessonsListView)
            {
                Debug.WriteLine("    PreviewMouseDown: Click detected OUTSIDE lessons list. Unselecting items."); // DEBUG
                lessonsListView.UnselectAll();
            }
            else
            {
                 Debug.WriteLine("    PreviewMouseDown: Click detected INSIDE lessons list."); // DEBUG
            }
            */ // <<== A QUI
        }

        // Gestore per il click sull'header della GridView dei Contratti (invariato)
        private void ContractsListView_HeaderClick(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is GridViewColumnHeader headerClicked)
            {
                if (headerClicked.Tag is string propertyName)
                {
                    if (this.DataContext is MainViewModel viewModel)
                    {
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
                     if (string.IsNullOrEmpty(childName) || (child is FrameworkElement frameworkElement && frameworkElement.Name == childName))
                     { foundChild = childType; break; }
                     else
                     { foundChild = FindVisualChild<T>(child, childName); if (foundChild != null) break; }
                 }
             }
             return foundChild;
        }

        private static bool IsDescendantOf(DependencyObject? element, DependencyObject? ancestor)
        {
             if (element == null || ancestor == null) return false;
             DependencyObject? parent = VisualTreeHelper.GetParent(element);
             while (parent != null) { if (parent == ancestor) { return true; } parent = VisualTreeHelper.GetParent(parent); }
             return false;
        }
    }
}