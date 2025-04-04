// App.xaml.cs
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Markup;

namespace WpfMvvmApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Imposta la cultura Italiana (Italia) come default per l'applicazione
            var culture = new CultureInfo("it-IT");

            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;

            // Imposta la lingua predefinita per i binding WPF
            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(culture.IetfLanguageTag)));

            base.OnStartup(e);
        }
    }
}