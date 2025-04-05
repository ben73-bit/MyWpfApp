// WpfMvvmApp/Services/ICalServiceImplementation.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ical.Net; // Usiamo la libreria Ical.Net
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using WpfMvvmApp.Models; // Per Lesson e Contract

namespace WpfMvvmApp.Services
{
    public class ICalServiceImplementation : ICalService
    {
        public bool ExportLessons(IEnumerable<Lesson> lessons, string filePath, string? contractInfo = null)
        {
            if (lessons == null || !lessons.Any()) return false;

            try
            {
                var calendar = new Calendar();
                calendar.Method = CalendarMethods.Publish;
                if (!string.IsNullOrWhiteSpace(contractInfo))
                {
                    calendar.Properties.Add(new CalendarProperty("X-WR-CALNAME", $"Lessons: {contractInfo}"));
                    calendar.Properties.Add(new CalendarProperty("X-WR-CALDESC", $"Exported lessons for contract: {contractInfo}"));
                }

                foreach (var lesson in lessons)
                {
                    if (lesson.Duration <= TimeSpan.Zero) continue;

                    var calendarEvent = new CalendarEvent
                    {
                        Uid = lesson.Uid ?? Guid.NewGuid().ToString(),
                        Summary = lesson.Summary ?? $"Lesson: {lesson.Contract?.Company ?? "N/A"}",
                        Description = lesson.Description ?? $"Duration: {lesson.Duration:hh\\:mm}",
                        Location = lesson.Location,
                        DtStart = new CalDateTime(lesson.StartDateTime),
                        Duration = lesson.Duration,
                        // Status = lesson.IsConfirmed ? EventStatus.Confirmed : EventStatus.Tentative, // Opzionale
                        DtStamp = new CalDateTime(DateTime.UtcNow),
                        LastModified = new CalDateTime(DateTime.UtcNow)
                    };
                    calendar.Events.Add(calendarEvent);
                }

                var serializer = new CalendarSerializer();
                string icalString = serializer.SerializeToString(calendar);
                File.WriteAllText(filePath, icalString);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error exporting iCal: {ex.Message}");
                return false;
            }
        }

        public List<Lesson> ImportLessons(string filePath)
        {
            var importedLessons = new List<Lesson>();
            if (!File.Exists(filePath)) return importedLessons;

            try
            {
                string icalContent = File.ReadAllText(filePath);
                var calendar = Calendar.Load(icalContent);

                foreach (var component in calendar.Children)
                {
                    if (component is CalendarEvent calendarEvent)
                    {
                        if (calendarEvent.DtStart == null) continue;

                        DateTime start = calendarEvent.DtStart.AsUtc;
                        TimeSpan duration;

                        if (calendarEvent.DtEnd != null)
                        {
                            if (calendarEvent.DtEnd.AsUtc > start) { duration = calendarEvent.DtEnd.AsUtc - start; }
                            else { continue; } // Salta evento con date non valide
                        }
                        // *** MODIFICA QUI: Rimosso il controllo 'calendarEvent.Duration != null' ***
                        // Controlliamo solo se la durata è effettivamente positiva.
                        // La proprietà Duration di Ical.Net è TimeSpan (non nullable),
                        // quindi il controllo != null era ridondante.
                        else if (calendarEvent.Duration > TimeSpan.Zero)
                        {
                            duration = calendarEvent.Duration;
                        }
                        else
                        {
                            continue; // Nessuna durata valida
                        }

                        var newLesson = new Lesson
                        {
                            StartDateTime = start.ToLocalTime(),
                            Duration = duration,
                            Uid = calendarEvent.Uid ?? Guid.NewGuid().ToString(),
                            Summary = calendarEvent.Summary,
                            Description = calendarEvent.Description,
                            Location = calendarEvent.Location,
                            IsConfirmed = false
                        };
                        importedLessons.Add(newLesson);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error importing iCal: {ex.Message}");
            }

            return importedLessons;
        }
    }
}