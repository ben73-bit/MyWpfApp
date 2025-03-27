using System.ComponentModel;
using System.Runtime.CompilerServices;
using WpfMvvmApp.Models; // Aggiungi questa direttiva

namespace WpfMvvmApp.ViewModels
{
    public class LessonViewModel : INotifyPropertyChanged
    {
        //Aggiunta di un event handler
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}