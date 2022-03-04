using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Recognizers.Text;
using Microsoft.Recognizers.Text.DateTime;

namespace DialogBot.Helpers
{
    public static class AiRecognizer
    {
        public static (DateTime, TimeSpan) RecognizeDateTimeRange(string source, out string rawString)
        {
            var aiResults = DateTimeRecognizer.RecognizeDateTime(source, Culture.English);
            if (aiResults.Count == 0)
                throw new Exception("Error: Couldn't recognize any time ranges in that source string.");

            /* Example contents of the below dictionary:
                [0]: {[timex, 2018-11-11T06:15]}
                [1]: {[type, datetime]}
                [2]: {[value, 2018-11-11 06:15:00]}
            */

            rawString = aiResults[0].Text;
            var aiResult = UnwindResult(aiResults[0]);
            foreach (var kvp in aiResult)
            {
                Console.WriteLine($"{kvp.Key}: {kvp.Value}");
            }
            var type = aiResult["type"];

            if (type != "datetimerange")
                throw new Exception($"Error: An invalid type of {type} was encountered ('datetimerange' expected).");

            return (
                DateTime.Parse(aiResult["start"]),
                DateTime.Parse(aiResult["end"]) - DateTime.Parse(aiResult["start"])
            );
        }

        public static DateTime RecognizeDateTime(string source, out string rawString)
        {
            var aiResults = DateTimeRecognizer.RecognizeDateTime(source, Culture.English);
            if (aiResults.Count == 0)
                throw new Exception("Error: Couldn't recognize any dates or times in that source string.");

            /* Example contents of the below dictionary:
                [0]: {[timex, 2018-11-11T06:15]}
                [1]: {[type, datetime]}
                [2]: {[value, 2018-11-11 06:15:00]}
            */

            rawString = aiResults[0].Text;
            var aiResult = UnwindResult(aiResults[0]);
            var type     = aiResult["type"];
            if (!new[]
                {
                    "datetime",
                    "date",
                    "time",
                    "datetimerange",
                    "daterange",
                    "timerange"
                }.Contains(type))
                throw new Exception($"Error: An invalid type of {type} was encountered ('datetime' expected).");

            var result = Regex.IsMatch(type, @"range$") ? aiResult["start"] : aiResult["value"];
            return DateTime.Parse(result);
        }

        private static Dictionary<string, string> UnwindResult(ModelResult modelResult) => (modelResult.Resolution["values"] as List<Dictionary<string, string>>)?[0];
    }
}
