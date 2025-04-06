// WpfMvvmApp/Dialogs/InvoiceInputDialog.xaml.cs
using System.Windows;

// Assicurati che questo namespace corrisponda!
namespace WpfMvvmApp.Dialogs
{
    /// <summary>
    /// Finestra di dialogo per inserire un numero di fattura.
    /// </summary>
    // *** AGGIUNTA keyword 'partial' ***
    public partial class InvoiceInputDialog : Window
    {
        public string InvoiceNumber { get; private set; } = "";

        public InvoiceInputDialog()
        {
            // Ora InitializeComponent è riconosciuto
            InitializeComponent();

            Loaded += (sender, args) =>
            {
                // Ora InvoiceNumberTextBox è riconosciuto
                InvoiceNumberTextBox.Focus();
                InvoiceNumberTextBox.SelectAll();
            };
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            // Ora InvoiceNumberTextBox è riconosciuto
            string inputText = InvoiceNumberTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(inputText))
            {
                MessageBox.Show("Invoice number cannot be empty.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                InvoiceNumberTextBox.Focus();
                InvoiceNumberTextBox.SelectAll();
                return;
            }

            this.InvoiceNumber = inputText;
            this.DialogResult = true;
        }
    }
}