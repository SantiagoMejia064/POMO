using System;
using System.Globalization;

namespace Pomo.Shared.Utilities
{
    /// <summary>
    /// Keeps the date persisted by the application independent from the format
    /// displayed to the user. UI fields use dd/MM/yyyy while data is stored as
    /// an ISO-8601 date-time with its local offset.
    /// </summary>
    public static class DateUtils
    {
        public const string UiDateFormat = "dd/MM/yyyy";

        public static bool TryParseUiDate(string value, out DateTimeOffset date)
        {
            date = default;

            if (!DateTime.TryParseExact(
                    value,
                    UiDateFormat,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTime parsedDate))
            {
                return false;
            }

            DateTime localDateTime = DateTime.SpecifyKind(parsedDate, DateTimeKind.Unspecified);
            TimeSpan offset = TimeZoneInfo.Local.GetUtcOffset(localDateTime);
            date = new DateTimeOffset(localDateTime, offset);
            return true;
        }

        public static bool TryParseStoredDate(string value, out DateTimeOffset date)
        {
            date = default;

            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            if (DateTimeOffset.TryParse(
                    value,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AllowWhiteSpaces,
                    out date))
            {
                return true;
            }

            // Supports the date-only values created before ISO storage was introduced.
            return TryParseUiDate(value, out date);
        }

        public static bool TryNormalizeToIso(string value, out string isoDate)
        {
            isoDate = null;

            if (!TryParseStoredDate(value, out DateTimeOffset date))
            {
                return false;
            }

            isoDate = date.ToString("O", CultureInfo.InvariantCulture);
            return true;
        }

        public static string FormatForUi(string isoDate)
        {
            return TryParseStoredDate(isoDate, out DateTimeOffset date)
                ? date.ToString(UiDateFormat, CultureInfo.InvariantCulture)
                : string.Empty;
        }
    }
}
