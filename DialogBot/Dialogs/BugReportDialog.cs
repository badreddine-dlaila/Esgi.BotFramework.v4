using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using AdaptiveCards.Templating;
using DialogBot.Models;
using DialogBot.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace DialogBot.Dialogs
{
    public class BugReportDialog : ComponentDialog
    {
        private readonly BotStateService _botStateService;

        public BugReportDialog(string dialogId, BotStateService botStateService) : base(dialogId)
        {
            _botStateService = botStateService ?? throw new ArgumentNullException(nameof(botStateService));

            InitializeWaterfallDialog();
        }

        private void InitializeWaterfallDialog()
        {
            // Create waterfall steps
            var waterFallSteps = new WaterfallStep[]
            {
                TitleStep,          // TextPrompt
                DescriptionStep,    // TextPrompt
                CallbackStep,       // DateTimePrompt 🚨
                PhoneNumberStep,    // TextPrompt https://regex101.com/r/0kpBet/1 🚨
                BugStep,            // ChoicePrompt 🚨
                OptionalDataStep,   // ChoicePrompt 🚨
                SummaryStep         // TextPrompt
            };

            // Add named dialogs$
            AddDialog(new WaterfallDialog($"{nameof(BugReportDialog)}.mainFlow", waterFallSteps));
            AddDialog(new TextPrompt($"{nameof(BugReportDialog)}.title"));
            AddDialog(new TextPrompt($"{nameof(BugReportDialog)}.description"));
            AddDialog(new DateTimePrompt($"{nameof(BugReportDialog)}.callbackTime", ValidateCallbackTime));
            AddDialog(new TextPrompt($"{nameof(BugReportDialog)}.phoneNumber", ValidatePhoneNumber));
            AddDialog(new ChoicePrompt($"{nameof(BugReportDialog)}.bug"));
            AddDialog(new TextPrompt($"{nameof(BugReportDialog)}.optionalData"));

            // Set the starting dialog
            InitialDialogId = $"{nameof(BugReportDialog)}.mainFlow";
        }

        private static async Task<DialogTurnResult> TitleStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var promptOptions = new PromptOptions { Prompt = MessageFactory.Text("Enter a Title") };
            return await stepContext.PromptAsync($"{nameof(BugReportDialog)}.title", promptOptions, cancellationToken);
        }

        private static async Task<DialogTurnResult> DescriptionStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Save description value from previous step (FoundChoice)
            stepContext.Values["title"] = (string)stepContext.Result;

            var promptOptions = new PromptOptions { Prompt = MessageFactory.Text("Enter a description of your report") };
            return await stepContext.PromptAsync($"{nameof(BugReportDialog)}.description", promptOptions, cancellationToken);
        }

        private static async Task<DialogTurnResult> CallbackStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Save description value from previous step to the object stepContext.
            // Step context is a bag that is alive for the entirety of your waterfall flow.
            // It's kind of a place to store information for the time period of that flow only.
            stepContext.Values["description"] = (string)stepContext.Result;

            var promptOptions = new PromptOptions
            {
                Prompt = MessageFactory.Text("Please specify a callback time (8AM - 6PM)"),
                RetryPrompt = MessageFactory.Text("The value entered must be between 8AM and 6PM", inputHint: "10:00")
            };
            return await stepContext.PromptAsync($"{nameof(BugReportDialog)}.callbackTime", promptOptions, cancellationToken);
        }

        private static async Task<DialogTurnResult> PhoneNumberStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Some date‑time magic to take that data from the previous step
            stepContext.Values["callbackTime"] = Convert.ToDateTime(((List<DateTimeResolution>)stepContext.Result).FirstOrDefault()?.Value);

            var promptOptions = new PromptOptions
            {
                Prompt = MessageFactory.Text("Please enter a phone number that we can call you back at"),
                RetryPrompt = MessageFactory.Text("Please enter a valid phone number", inputHint: "0654621418")
            };
            return await stepContext.PromptAsync($"{nameof(BugReportDialog)}.phoneNumber", promptOptions, cancellationToken);
        }

        private static async Task<DialogTurnResult> BugStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["phoneNumber"] = (string)stepContext.Result;
            var promptOptions = new PromptOptions
            {
                Prompt = MessageFactory.Text("Please enter the bug type"),
                Choices = ChoiceFactory.ToChoices(new List<string>
                {
                    "Security",
                    "Crash",
                    "Performance",
                    "Critical Bug",
                    "Test"
                })
            };
            return await stepContext.PromptAsync($"{nameof(BugReportDialog)}.bug", promptOptions, cancellationToken);
        }

        private static async Task<DialogTurnResult> OptionalDataStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Save description value from previous step (FoundChoice)
            stepContext.Values["bug"] = ((FoundChoice)stepContext.Result).Value;

            using var streamReader = new StreamReader(@"Dialogs\Cards\OptionalData.json");
            var cardJson = await streamReader.ReadToEndAsync();
            var cardAttachment = new Attachment
            {
                ContentType = AdaptiveCard.ContentType,
                Content = JsonConvert.DeserializeObject(cardJson),
            };
            var message = MessageFactory.Text("");
            message.Attachments = new List<Attachment>() { cardAttachment };
            await stepContext.Context.SendActivityAsync(message, cancellationToken);

            // Create the text prompt
            var promptOptions = new PromptOptions { Prompt = new Activity { Type = ActivityTypes.Message } };

            // Display a Text Prompt and wait for input
            return await stepContext.PromptAsync($"{nameof(BugReportDialog)}.optionalData", promptOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> SummaryStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Save description value from previous step 
            var dynamicResult = (dynamic)JsonConvert.DeserializeObject(stepContext.Result.ToString() ?? string.Empty);
            stepContext.Values["correlationId"] = dynamicResult?.correlationId?.ToString();
            if (PropertyExist(dynamicResult, "observationDate"))
                stepContext.Values["observationDate"] = Convert.ToDateTime(dynamicResult?.observationDate);

            // Get the current profile from user state
            var userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile(), cancellationToken);

            // Save all the collected data to user profile
            var bugReport = new BugReport
            {
                Title = (string)stepContext.Values["title"],
                Description = (string)stepContext.Values["description"],
                CallbackTime = (DateTime)stepContext.Values["callbackTime"],
                PhoneNumber = (string)stepContext.Values["phoneNumber"],
                Bug = (string)stepContext.Values["bug"],
                CorrelationId = (string)stepContext.Values["correlationId"] ?? $"(New) {Guid.NewGuid()}",
                ObservationDate = stepContext.Values.ContainsKey("observationDate")
                    ? (DateTime)stepContext.Values["observationDate"]
                    : DateTime.UtcNow
            };
            var bugReportId = userProfile.PushBugReport(bugReport);

            // Save profile in user state
            await _botStateService.UserProfileAccessor.SetAsync(stepContext.Context, userProfile, cancellationToken);

            var cardJson = await GetSummaryCardJson(userProfile, bugReportId);
            var adaptiveCardAttachment = new Attachment
            {
                ContentType = AdaptiveCard.ContentType,
                Content = JsonConvert.DeserializeObject(cardJson)
            };

            await stepContext.Context.SendActivityAsync(MessageFactory.Attachment(adaptiveCardAttachment), cancellationToken);

            // WaterfallStep always finishes with the end of the waterfall or with another dialog, here it is the end
            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }

        private static async Task<string> GetSummaryCardJson(UserProfile userProfile, Guid bugReportId)
        {
            var actualBugReport = userProfile.BugReports.First(report => report.Id == bugReportId);
            // Create a Template instance from the template payload
            using var streamReader = new StreamReader(@"Dialogs\Cards\SummaryCard.json");
            var summaryCardTemplateJson = await streamReader.ReadToEndAsync();
            var template = new AdaptiveCardTemplate(summaryCardTemplateJson);

            // Serializable object as template data
            var cardData = new
            {
                title = actualBugReport.Title,
                description = actualBugReport.Description,
                properties = new[]
                {
                    new
                    {
                        key = "Callback Time",
                        value = $"{actualBugReport.CallbackTime.ToUniversalTime():s}Z"
                    },
                    new
                    {
                        key = "Phone Number" ,
                        value = actualBugReport.PhoneNumber
                    },
                    new
                    {
                        key = "Bug",
                        value = actualBugReport.Bug
                    },
                    new
                    {
                        key = "Observation Date",
                        value = $"{actualBugReport.ObservationDate.ToUniversalTime():s}Z"
                    },
                    new
                    {
                        key = "Correlation ID",
                        value = actualBugReport.CorrelationId
                    }
                },
                reports = userProfile.BugReports.Select(report =>
                    new
                    {
                        correlation_id = report.CorrelationId,
                        creation_date = $"{report.DateTime.ToUniversalTime():s}Z",
                        bug = report.Bug,
                        custom_fields = new []
                        {
                            new
                            {
                                customfield_id = "16367000000277001",
                                value = $"Created by **{userProfile.Name}** with callback number  : **{report.PhoneNumber}**"
                            },
                            new
                            {
                                customfield_id = "16367000000277001",
                                value = $"Severity **{report.Bug}** reported at {report.ObservationDate:R}"
                            }
                        }
                    }),
                creator = new
                {
                    name = userProfile.Name,
                    profileImage = "https://www.gravatar.com/avatar/ddc3ed4d9e5b92112753d7c2659bc822?d=retro"
                },
                createdUtc = DateTime.UtcNow,
                viewUrl = "https://github.com/hankhank10/fakeface",
            };

            // "Expand" the template - this generates the final Adaptive Card payload
            var cardJson = template.Expand(cardData);
            return cardJson;
        }

        // Validators
        private static Task<bool> ValidateCallbackTime(PromptValidatorContext<IList<DateTimeResolution>> promptContext, CancellationToken cancellationToken)
        {
            if (!promptContext.Recognized.Succeeded)
                return Task.FromResult(false);

            var resolution = promptContext.Recognized.Value.First();
            var selectedDateTime = Convert.ToDateTime(resolution.Value);
            var isValid = selectedDateTime.TimeOfDay >= new TimeSpan(8, 0, 0) && selectedDateTime.TimeOfDay <= new TimeSpan(18, 0, 0);

            return Task.FromResult(isValid);
        }

        private static Task<bool> ValidatePhoneNumber(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            if (!promptContext.Recognized.Succeeded)
                return Task.FromResult(false);

            var pattern = new Regex(@"^(?:(?:\+|00)33|0)\s*[1-9](?:[\s.-]*\d{2}){4}$");
            return Task.FromResult(pattern.Match(promptContext.Recognized.Value).Success);
        }

        public static bool PropertyExist(dynamic obj, string property)
        {
            return ((Type)obj.GetType()).GetProperties().Any(p => p.Name.Equals(property));
        }
    }
}