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
            get
            {
                return resourceCulture ?? Thread.CurrentThread.CurrentUICulture;
            }
            set
            {
                resourceCulture = value;
            }
        }

        // Metodo helper per ottenere una stringa
        private static string? GetString(string name)
        {
            return ResourceManager.GetString(name, Culture);
        }

        // --- Proprietà Statiche per le Risorse ---

        // Generali App
        public static string? AppName => GetString("AppName");

        // Pulsanti Contratto
        public static string? AddContractButton_Content => GetString("AddContractButton_Content");
        public static string? RemoveContractButton_Content => GetString("RemoveContractButton_Content");
        public static string? SaveContractButton_Content => GetString("SaveContractButton_Content");
        public static string? ContractsTitle => GetString("ContractsTitle");

        // Pulsanti/Titoli Lezione
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

        // Messaggi Validazione
        public static string? Validation_FieldRequired => GetString("Validation_FieldRequired");
        public static string? Validation_PositiveNumber => GetString("Validation_PositiveNumber");
        public static string? Validation_MinimumValue => GetString("Validation_MinimumValue");
        public static string? Validation_CannotBeNegative => GetString("Validation_CannotBeNegative");
        public static string? Validation_EndDateAfterStartDate => GetString("Validation_EndDateAfterStartDate");
        public static string? Validation_TimeSpanPositive => GetString("Validation_TimeSpanPositive");
        public static string? Validation_InvalidTimeFormat => GetString("Validation_InvalidTimeFormat"); // Aggiunto

        // Titoli MessageBox
        public static string? MsgBox_Title_ConfirmRemoval => GetString("MsgBox_Title_ConfirmRemoval");
        public static string? MsgBox_Title_ConfirmContractDeletion => GetString("MsgBox_Title_ConfirmContractDeletion");
        public static string? MsgBox_Title_NoContractSelected => GetString("MsgBox_Title_NoContractSelected");
        public static string? MsgBox_Title_ImportResult => GetString("MsgBox_Title_ImportResult");
        public static string? MsgBox_Title_ImportComplete => GetString("MsgBox_Title_ImportComplete");
        public static string? MsgBox_Title_ImportError => GetString("MsgBox_Title_ImportError");
        public static string? MsgBox_Title_Export => GetString("MsgBox_Title_Export");
        public static string? MsgBox_Title_ExportComplete => GetString("MsgBox_Title_ExportComplete");
        public static string? MsgBox_Title_ExportError => GetString("MsgBox_Title_ExportError");
        public static string? MsgBox_Title_Billing => GetString("MsgBox_Title_Billing");
        public static string? MsgBox_Title_BillingComplete => GetString("MsgBox_Title_BillingComplete");
        public static string? MsgBox_Title_ValidationError => GetString("MsgBox_Title_ValidationError");
        public static string? MsgBox_Title_InternalError => GetString("MsgBox_Title_InternalError");
        public static string? MsgBox_Title_SaveError => GetString("MsgBox_Title_SaveError");
        public static string? MsgBox_Title_SaveContract => GetString("MsgBox_Title_SaveContract");

        // Messaggi MessageBox
        public static string? MsgBox_ConfirmRemoveLesson_Text => GetString("MsgBox_ConfirmRemoveLesson_Text");
        public static string? MsgBox_ConfirmRemoveSelectedLesson_Text_Singular => GetString("MsgBox_ConfirmRemoveSelectedLesson_Text_Singular");
        public static string? MsgBox_ConfirmRemoveSelectedLessons_Text_Plural => GetString("MsgBox_ConfirmRemoveSelectedLessons_Text_Plural");
        public static string? MsgBox_ConfirmContractDeletion_Text => GetString("MsgBox_ConfirmContractDeletion_Text");
        public static string? MsgBox_SelectContractBeforeImport_Text => GetString("MsgBox_SelectContractBeforeImport_Text");
        public static string? MsgBox_NoLessonsToImport_Text => GetString("MsgBox_NoLessonsToImport_Text");
        public static string? MsgBox_ImportSuccessful_Text => GetString("MsgBox_ImportSuccessful_Text");
        public static string? MsgBox_ImportError_Text => GetString("MsgBox_ImportError_Text");
        public static string? MsgBox_SelectContractBeforeExport_Text => GetString("MsgBox_SelectContractBeforeExport_Text");
        public static string? MsgBox_NoLessonsToExport_Text => GetString("MsgBox_NoLessonsToExport_Text");
        public static string? MsgBox_ExportSuccessful_Text => GetString("MsgBox_ExportSuccessful_Text");
        public static string? MsgBox_ExportError_Text => GetString("MsgBox_ExportError_Text");
        public static string? MsgBox_ExportFileError_Text => GetString("MsgBox_ExportFileError_Text");
        public static string? MsgBox_NoLessonsToBill_Text => GetString("MsgBox_NoLessonsToBill_Text");
        public static string? MsgBox_InvoiceNumberEmpty_Text => GetString("MsgBox_InvoiceNumberEmpty_Text");
        public static string? MsgBox_InvoiceDateNotSelected_Text => GetString("MsgBox_InvoiceDateNotSelected_Text");
        public static string? MsgBox_InvoiceDateError_Text => GetString("MsgBox_InvoiceDateError_Text");
        public static string? MsgBox_BillingSuccessful_Text => GetString("MsgBox_BillingSuccessful_Text");
        public static string? MsgBox_SaveError_Text => GetString("MsgBox_SaveError_Text");
        public static string? MsgBox_SaveErrorUnknown_Text => GetString("MsgBox_SaveErrorUnknown_Text");
        public static string? MsgBox_SaveErrorCritical_Text => GetString("MsgBox_SaveErrorCritical_Text");
        public static string? MsgBox_SaveContractNoted_Text => GetString("MsgBox_SaveContractNoted_Text");
        // NUOVE: Stringhe ContractView
        public static string? ContractDetails_Header => GetString("ContractDetails_Header");
        public static string? ContractDetails_CompanyLabel => GetString("ContractDetails_CompanyLabel");
        public static string? ContractDetails_NumberLabel => GetString("ContractDetails_NumberLabel");
        public static string? ContractDetails_RateLabel => GetString("ContractDetails_RateLabel");
        public static string? ContractDetails_TotalHoursLabel => GetString("ContractDetails_TotalHoursLabel");
        public static string? ContractDetails_BilledHoursLabel => GetString("ContractDetails_BilledHoursLabel");
        public static string? ContractDetails_StartDateLabel => GetString("ContractDetails_StartDateLabel");
        public static string? ContractDetails_EndDateLabel => GetString("ContractDetails_EndDateLabel");
        public static string? SummaryBox_Title => GetString("SummaryBox_Title");
        public static string? SummaryBox_HoursTitle => GetString("SummaryBox_HoursTitle");
        public static string? SummaryBox_InsertedLabel => GetString("SummaryBox_InsertedLabel");
        public static string? SummaryBox_ConfirmedLabel => GetString("SummaryBox_ConfirmedLabel");
        public static string? SummaryBox_BilledLabel => GetString("SummaryBox_BilledLabel");
        public static string? SummaryBox_AmountsTitle => GetString("SummaryBox_AmountsTitle");
        public static string? SummaryBox_PotentialLabel => GetString("SummaryBox_PotentialLabel");
        public static string? SummaryBox_ReadyToBillLabel => GetString("SummaryBox_ReadyToBillLabel");
        public static string? SummaryBox_AlreadyBilledLabel => GetString("SummaryBox_AlreadyBilledLabel");
        public static string? SummaryBox_RemainingHoursLabel => GetString("SummaryBox_RemainingHoursLabel");
        // NUOVE: Intestazioni Lista Contratti
        public static string? ContractList_CompanyHeader => GetString("ContractList_CompanyHeader");
        public static string? ContractList_NumberHeader => GetString("ContractList_NumberHeader");

        public static string? MsgBox_NoNewLessonsImported_Text { get; internal set; }

        // --- Fine Proprietà Stringhe ---

        // Costruttore privato
        private Resources() { }
    }
}