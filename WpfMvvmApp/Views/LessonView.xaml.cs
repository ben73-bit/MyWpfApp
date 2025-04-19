// WpfMvvmApp/Views/LessonView.xaml.cs
using System.Windows;
using System.Windows.Controls; // Aggiungi questo using

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

        // NUOVO METODO: Gestore evento GotFocus per selezionare tutto il testo
        private void TextBox_GotFocus_SelectAll(object sender, RoutedEventArgs e)
        {
            // Controlla se il sender è un TextBox
            if (sender is TextBox textBox)
            {
                // Seleziona tutto il testo all'interno del TextBox
                // Usiamo Dispatcher.BeginInvoke per assicurarci che l'azione
                // venga eseguita dopo che il focus è stato effettivamente impostato
                // e il controllo è pronto. In molti casi SelectAll() funziona anche
                // direttamente, ma questo approccio è più robusto.
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    textBox.SelectAll();
                }), System.Windows.Threading.DispatcherPriority.Input);
            }
        }
    }
}