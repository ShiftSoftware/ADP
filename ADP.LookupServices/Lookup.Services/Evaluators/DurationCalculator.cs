using ShiftSoftware.ADP.Models.Enums;
using System;

namespace ShiftSoftware.ADP.Lookup.Services.Evaluators;

internal static class DurationCalculator
{
    internal static DateTime AddInterval(DateTime date, int? intervalValue, DurationType? durationType) =>
        durationType switch
        {
            DurationType.Seconds => date.AddSeconds(intervalValue.GetValueOrDefault()),
            DurationType.Minutes => date.AddMinutes(intervalValue.GetValueOrDefault()),
            DurationType.Hours => date.AddHours(intervalValue.GetValueOrDefault()),
            DurationType.Days => date.AddDays(intervalValue.GetValueOrDefault()),
            DurationType.Weeks => date.AddDays(7 * intervalValue.GetValueOrDefault()),
            DurationType.Months => date.AddMonths(intervalValue.GetValueOrDefault()),
            DurationType.Years => date.AddYears(intervalValue.GetValueOrDefault()),
            _ => date,
        };
}
