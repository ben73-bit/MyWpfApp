// WpfMvvmApp/Views/ContractView.xaml.cs
using System.Windows; // Necessario per DependencyObject, RoutedEventArgs
using System.Windows.Controls;
using System.Windows.Input; // Necessario per MouseButtonEventArgs
using System.Windows.Media; // Necessario per VisualTreeHelper

namespace WpfMvvmApp.Views
{
    public partial class ContractView : UserControl
    {
        public ContractView()
        {
            InitializeComponent();
        }

        // NUOVO: Gestore per PreviewMouseDown sull'UserControl
        private void UserControl_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // Ottieni l'elemento su cui è stato fatto clic
            var originalSource = e.OriginalSource as DependencyObject;
            if (originalSource == null) return;

            // Cerca la ListView delle lezioni
            ListView? lessonsListView = FindVisualChild<ListView>(this, "LessonsListView");

            // Se la ListView non è stata trovata (ad es., nessun contratto selezionato), esci
            if (lessonsListView == null) return;

            // Controlla se il clic è avvenuto DENTRO la ListView delle lezioni
            bool clickIsInsideLessonsListView = IsDescendantOf(originalSource, lessonsListView);

            // Se il clic NON è avvenuto dentro la ListView, deseleziona tutto
            if (!clickIsInsideLessonsListView)
            {
                lessonsListView.UnselectAll();
                // Alternativa: lessonsListView.SelectedIndex = -1;
            }
        }

        // --- Helper per navigare l'Albero Visuale ---

        /// <summary>
        /// Trova il primo figlio visuale del tipo specificato, opzionalmente con un nome specifico.
        /// </summary>
        private static T? FindVisualChild<T>(DependencyObject parent, string? childName = null) where T : DependencyObject
        {
            if (parent == null) return null;

            T? foundChild = null;

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                // Se non è il tipo giusto, cerca ricorsivamente
                if (!(child is T childType))
                {
                    foundChild = FindVisualChild<T>(child, childName);
                    // Se trovato ricorsivamente, esci
                    if (foundChild != null) break;
                }
                // Se è il tipo giusto
                else
                {
                    // Se non serve un nome specifico o se il nome corrisponde
                    if (string.IsNullOrEmpty(childName) ||
                        (child is FrameworkElement frameworkElement && frameworkElement.Name == childName))
                    {
                        foundChild = childType;
                        break; // Trovato, esci
                    }
                    // Se il nome non corrisponde, cerca ricorsivamente nei figli di questo nodo
                    else
                    {
                         foundChild = FindVisualChild<T>(child, childName);
                         if (foundChild != null) break;
                    }
                }
            }
            return foundChild;
        }

        /// <summary>
        /// Verifica se un elemento è un discendente (diretto o indiretto) di un altro elemento nell'albero visuale.
        /// </summary>
        private static bool IsDescendantOf(DependencyObject? element, DependencyObject? ancestor)
        {
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