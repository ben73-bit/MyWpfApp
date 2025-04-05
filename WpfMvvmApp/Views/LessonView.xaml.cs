// WpfMvvmApp/Views/LessonView.xaml.cs
using System.Windows.Controls;
// Rimosse le using non più necessarie per lo scroll (Input, Media) se non usate altrove

namespace WpfMvvmApp.Views
{
    public partial class LessonView : UserControl
    {
        public LessonView()
        {
            InitializeComponent();
        }

        // RIMOSSO il metodo ListView_PreviewMouseWheel
        // RIMOSSO il metodo FindVisualChild (ora è nel Behavior)
    }
}