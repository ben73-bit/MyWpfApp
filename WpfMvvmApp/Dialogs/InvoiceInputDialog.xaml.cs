// WpfMvvmApp/Dialogs/InvoiceInputDialog.xaml.cs
using System;
using System.Windows;
using WpfMvvmApp.Properties; // Assicurati che questa using sia presente

namespace WpfMvvmApp.Dialogs
{
    public partial class InvoiceInputDialog : Window
    {
        public string InvoiceNumber { get; private set; } = "";
        public DateTime? InvoiceDate { get; private set; }
        public DateTime DefaultInvoiceDate { get; set; } = DateTime.Today;

        public InvoiceInputDialog()
        {
            InitializeComponent();
            this.DataContext = this;
            Loaded += (sender, args) => InvoiceNumberTextBox.Focus();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            string inputNumber = InvoiceNumberTextBox.Text.Trim();
            DateTime? selectedDate = InvoiceDatePicker.SelectedDate;

            // CORRETTO: Usa la classe statica Resources
            if (string.IsNullOrWhiteSpace(inputNumber))
            {
                MessageBox.Show(
                    // Accedi tramite la classe statica
                    Properties.Resources.MsgBox_InvoiceNumberEmpty_Text ?? "Invoice number cannot be empty.",
                    Properties.Resources.MsgBox_Title_ValidationError ?? "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                InvoiceNumberTextBox.Focus();
                InvoiceNumberTextBox.SelectAll();
                return;
            }

            if (!selectedDate.HasValue)
            {
                MessageBox.Show(
                    // Accedi tramite la classe statica
                    Properties.Resources.MsgBox_InvoiceDateNotSelected_Text ?? "Please select an invoice date.",
                    Properties.Resources.MsgBox_Title_ValidationError ?? "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                InvoiceDatePicker.Focus();
                return;
            }

            this.InvoiceNumber = inputNumber;
            this.InvoiceDate = selectedDate.Value;
            this.DialogResult = true;
        }
    }
}