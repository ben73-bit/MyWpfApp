// WpfMvvmApp/Dialogs/InvoiceInputDialog.xaml.cs
using System; // Necessario per DateTime
using System.Windows;

namespace WpfMvvmApp.Dialogs
{
    public partial class InvoiceInputDialog : Window
    {
        // Proprietà pubbliche per accedere ai valori inseriti
        public string InvoiceNumber { get; private set; } = "";
        // NUOVO: Proprietà per la data (nullable perché DatePicker può non avere data selezionata)
        public DateTime? InvoiceDate { get; private set; }

        // Proprietà per impostare una data di default (opzionale)
        // Non implementiamo INotifyPropertyChanged qui per semplicità,
        // ma impostiamo il valore nel costruttore.
        public DateTime DefaultInvoiceDate { get; set; } = DateTime.Today; // Default a oggi

        public InvoiceInputDialog()
        {
            InitializeComponent();
            // Imposta il DataContext della finestra su se stessa
            // per permettere il binding di DefaultInvoiceDate
            this.DataContext = this;

            Loaded += (sender, args) => InvoiceNumberTextBox.Focus();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            string inputNumber = InvoiceNumberTextBox.Text.Trim();
            DateTime? selectedDate = InvoiceDatePicker.SelectedDate; // Legge la data dal DatePicker

            // Validazione Numero
            if (string.IsNullOrWhiteSpace(inputNumber))
            {
                MessageBox.Show("Invoice number cannot be empty.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                InvoiceNumberTextBox.Focus();
                InvoiceNumberTextBox.SelectAll();
                return;
            }

            // NUOVO: Validazione Data
            if (!selectedDate.HasValue)
            {
                MessageBox.Show("Please select an invoice date.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                InvoiceDatePicker.Focus(); // Mette focus sul DatePicker
                return;
            }

            // Se validi: salva i valori e chiudi con DialogResult = true
            this.InvoiceNumber = inputNumber;
            this.InvoiceDate = selectedDate.Value; // Assegna il valore della data
            this.DialogResult = true;
        }
    }
}