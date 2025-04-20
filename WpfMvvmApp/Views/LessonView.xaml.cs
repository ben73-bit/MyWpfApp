// WpfMvvmApp/Views/LessonView.xaml.cs
using System; // Necessario per Action
using System.Windows;
using System.Windows.Controls; // Necessario per TextBox, GridViewColumnHeader
using System.Windows.Input; // Necessario per RoutedEventArgs (anche se non usato direttamente)
using WpfMvvmApp.ViewModels; // NECESSARIO per accedere a ContractViewModel

namespace WpfMvvmApp.Views
{
    /// <summary>
    /// Interaction logic for LessonView.xaml
    /// </summary>
    public partial class LessonView : UserControl
    {
        public LessonView()
        {
            InitializeComponent();
        }

        // Gestore evento GotFocus per selezionare tutto il testo (esistente)
        private void TextBox_GotFocus_SelectAll(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    textBox.SelectAll();
                }), System.Windows.Threading.DispatcherPriority.Input);
            }
        }

        // NUOVO: Gestore per il click sull'header della GridView
        private void LessonsListView_HeaderClick(object sender, RoutedEventArgs e)
        {
            // Verifica che l'evento sia originato da un header di colonna
            if (e.OriginalSource is GridViewColumnHeader headerClicked)
            {
                // Assicurati che l'header abbia un Tag (che contiene il nome della proprietà)
                if (headerClicked.Tag is string propertyName)
                {
                    // Ottieni il ViewModel associato a questa View
                    if (this.DataContext is ContractViewModel viewModel)
                    {
                        // Esegui il comando di ordinamento nel ViewModel, passando il nome della proprietà
                        if (viewModel.SortLessonsCommand.CanExecute(propertyName))
                        {
                            viewModel.SortLessonsCommand.Execute(propertyName);
                        }
                    }
                }
            }
        }
    }
}