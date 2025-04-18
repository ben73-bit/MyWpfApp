// WpfMvvmApp/Properties/Resources.cs
using System.Resources;
using System.Globalization;
using System.Reflection;
using System.Threading;
using System.ComponentModel;
using System.Runtime.CompilerServices; // Necessario per EditorBrowsable

// Assicurati che il namespace sia corretto!
namespace WpfMvvmApp.Properties
{
    /// <summary>
    ///   Classe di risorse fortemente tipizzata manuale per cercare stringhe localizzate.
    /// </summary>
    public class Resources
    {
        private static ResourceManager? resourceMan;
        private static CultureInfo? resourceCulture;

        /// <summary>
        ///   Restituisce l'istanza di ResourceManager memorizzata nella cache utilizzata da questa classe.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public static ResourceManager ResourceManager
        {
            // *** CORRETTO: Aggiunto blocco get ***
            get
            {
                if (resourceMan == null)
                {
                    // Assicurati che "WpfMvvmApp.Properties.Resources" corrisponda
                    ResourceManager temp = new ResourceManager("WpfMvvmApp.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }

        /// <summary>
        ///   Esegue l'override della proprietà CurrentUICulture del thread corrente per tutte le
        ///   ricerche di risorse eseguite utilizzando questa classe di risorse fortemente tipizzata.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public static CultureInfo Culture
        {
             // *** CORRETTO: Aggiunto blocco get e set ***
            get
            {
                return resourceCulture ?? Thread.CurrentThread.CurrentUICulture;
            }
            set
            {
                resourceCulture = value;
                // Potremmo aggiungere qui la notifica PropertyChanged se Resources implementasse INotifyPropertyChanged
                // per aggiornamenti dinamici della lingua.
            }
        }

        // Metodo helper per ottenere una stringa
        private static string? GetString(string name)
        {
            // *** CORRETTO: Usa le proprietà ResourceManager e Culture ***
            return ResourceManager.GetString(name, Culture);
        }

        // --- AGGIUNGI QUI LE PROPRIETÀ STATICHE PER OGNI STRINGA ---
        public static string? AppName => GetString("AppName");
        public static string? AddContractButton_Content => GetString("AddContractButton_Content");
        public static string? RemoveContractButton_Content => GetString("RemoveContractButton_Content");
        public static string? SaveContractButton_Content => GetString("SaveContractButton_Content");
        public static string? ContractsTitle => GetString("ContractsTitle");
        public static string? LessonsTitle => GetString("LessonsTitle");
        public static string? BillSelectedButton_Content => GetString("BillSelectedButton_Content");
        public static string? ImportIcsButton_Content => GetString("ImportIcsButton_Content");
        public static string? ExportIcsButton_Content => GetString("ExportIcsButton_Content");
        public static string? LessonInput_StartLabel => GetString("LessonInput_StartLabel");
        public static string? LessonInput_TimeSeparatorLabel => GetString("LessonInput_TimeSeparatorLabel");
        public static string? LessonInput_TimeToolTip => GetString("LessonInput_TimeToolTip");
        public static string? LessonInput_DurationLabel => GetString("LessonInput_DurationLabel");
        public static string? LessonInput_DurationToolTip => GetString("LessonInput_DurationToolTip");
        public static string? LessonInput_SummaryLabel => GetString("LessonInput_SummaryLabel");
        public static string? LessonInput_SummaryToolTip => GetString("LessonInput_SummaryToolTip");
        public static string? LessonList_DateHeader => GetString("LessonList_DateHeader");
        public static string? LessonList_StartHeader => GetString("LessonList_StartHeader");
        public static string? LessonList_DurationHeader => GetString("LessonList_DurationHeader");
        public static string? LessonList_EndHeader => GetString("LessonList_EndHeader");
        public static string? LessonList_SummaryHeader => GetString("LessonList_SummaryHeader");
        public static string? LessonList_AmountHeader => GetString("LessonList_AmountHeader");
        public static string? LessonList_ConfirmedHeader => GetString("LessonList_ConfirmedHeader");
        public static string? LessonList_BilledHeader => GetString("LessonList_BilledHeader");
        public static string? LessonList_InvoiceNumberHeader => GetString("LessonList_InvoiceNumberHeader");
        public static string? LessonList_InvoiceDateHeader => GetString("LessonList_InvoiceDateHeader");
        public static string? LessonList_ActionsHeader => GetString("LessonList_ActionsHeader");
        public static string? LessonActions_EditButton_Content => GetString("LessonActions_EditButton_Content");
        public static string? LessonActions_RemoveButton_Content => GetString("LessonActions_RemoveButton_Content");
        public static string? LessonActions_CancelButton_Content => GetString("LessonActions_CancelButton_Content");
        public static string? Button_AddLesson => GetString("Button_AddLesson");
        public static string? Button_UpdateLesson => GetString("Button_UpdateLesson");
        // --- Fine Proprietà Stringhe ---

        // Costruttore privato
        private Resources() { }
    }
}