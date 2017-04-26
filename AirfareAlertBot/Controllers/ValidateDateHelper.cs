using Microsoft.Bot.Builder.FormFlow;
using System;
using System.Collections.Generic;

namespace AirfareAlertBot.Controllers
{
    // This helper class is used to validate trip dates
    public class ValidateDateHelper
    {
        // Parses and converts a string date to a DateTime date
        public static DateTime ToDateTime(string date)
        {
            return DateTime.Parse(date);
        }

        // Determines the number of days between two dates
        private static int DaysBetween(DateTime d1, DateTime d2)
        {
            TimeSpan span = d2.Subtract(d1);
            return (int)span.TotalDays;
        }

        // Formats the date to a more human readable way ;)
        public static string FormatDate(string dt)
        {
            string res = dt;
            List<string> nParts = new List<string>();

            string tmp = dt.Replace("/", "-").Replace(" ", "-").Replace(".", "-");
            string[] parts = tmp.Split('-');

            if (parts?.Length > 0)
            {
                int j = 1;
                foreach (string str in parts)
                {
                    if (str != "-")
                    {
                        string t = string.Empty;

                        if (j == 2)
                            t = str.Substring(0, 3);
                        else
                            t = str;

                        nParts.Add(t);
                    }

                    j++;
                }

                if (nParts.Count > 0)
                {
                    int i = 1;
                    List<string> pParts = new List<string>();

                    foreach (string p in nParts)
                    {
                        if (p.Length < 2 && (i == 1))
                            pParts.Add(p.PadLeft(2, '0'));
                        else if (p.Length == 2 && (i == 3))
                        {
                            DateTime n = ToDateTime(dt);
                            pParts.Add(n.Year.ToString());
                        }
                        else
                            pParts.Add(p);

                        i++;
                    }

                    res = string.Join("-", pParts.ToArray()).ToUpper();
                }
            }

            return res;
        }

        // Checks if a date is in the future
        public static ValidateResult IsFutureDate(DateTime dt, string value)
        {
            ValidateResult result = new ValidateResult { IsValid = false, Value = string.Empty };

            if (DaysBetween(DateTime.Now, dt) >= 0)
                result = new ValidateResult { IsValid = true, Value = value };
            else
                result.Feedback = GatherErrors.cStrGatherStatePastDate;

            return result;
        }

        // Main method responsible for validating trip dates
        public static ValidateResult ValidateGoAndReturnDates(DateTime go, DateTime comeback, string value)
        {
            ValidateResult result = new ValidateResult { IsValid = false, Value = string.Empty };
            
            result = new ValidateResult { IsValid = false, Value = string.Empty };

            if (DaysBetween(go, comeback) >= 0)
                result = new ValidateResult { IsValid = true, Value = value };
            else
                result.Feedback = GatherErrors.cStrGatherStateFutureDate;

            return result;
        }
    }
}