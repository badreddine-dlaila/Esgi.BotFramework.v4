using System;
using System.Collections.Generic;

namespace DialogBot.Models
{
    public class UserProfile
    {
        public string    Name         { get; set; }
        public string    Description  { get; set; }
        public DateTime? CallbackTime { get; set; }
        public string    PhoneNumber  { get; set; }
        public string    Bug          { get; set; }

        public List<BugReport> BugReports { get; set; }

        public Guid PushBugReport(BugReport bugReport)
        {
            BugReports ??= new List<BugReport>();
            BugReports.Add(bugReport);
            return bugReport.Id;
        }
    }

    public class BugReport
    {
        public Guid     Id              { get; } = Guid.NewGuid();
        public DateTime DateTime        { get; } = DateTime.UtcNow;
        public string   Title           { get; set; }
        public string   Description     { get; set; }
        public DateTime CallbackTime    { get; set; }
        public string   PhoneNumber     { get; set; }
        public string   Bug             { get; set; }
        public string   CorrelationId   { get; set; }
        public DateTime ObservationDate { get; set; }
    }
}
