using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DialogBot.Models;
using DialogBot.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;

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
                DescriptionStep,    // TextPrompt
                CallbackStep,       // DateTimePrompt 🚨
                PhoneNumberStep,    // TextPrompt https://regex101.com/r/0kpBet/1 🚨
                BugStep,            // ChoicePrompt 🚨
                SummaryStep         // TextPrompt
            };

            // Add named dialogs$
            AddDialog(new WaterfallDialog($"{nameof(BugReportDialog)}.mainFlow", waterFallSteps));
            AddDialog(new TextPrompt($"{nameof(BugReportDialog)}.description"));
            AddDialog(new DateTimePrompt($"{nameof(BugReportDialog)}.callbackTime", ValidateCallbackTime));
            AddDialog(new TextPrompt($"{nameof(BugReportDialog)}.phoneNumber", ValidatePhoneNumber));
            AddDialog(new ChoicePrompt($"{nameof(BugReportDialog)}.bug"));

            // Set the starting dialog
            InitialDialogId = $"{nameof(BugReportDialog)}.mainFlow";
        }

        private static async Task<DialogTurnResult> DescriptionStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
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

        private  async Task<DialogTurnResult> SummaryStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Save description value from previous step (FoundChoice)
            stepContext.Values["bug"] = ((FoundChoice)stepContext.Result).Value;

            // Get the current profile from user state
            var userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile(), cancellationToken);

            // Save all the collected data inside user profile
            userProfile.Description = (string)stepContext.Values["description"];
            userProfile.CallbackTime = (DateTime)stepContext.Values["callbackTime"];
            userProfile.PhoneNumber = (string)stepContext.Values["phoneNumber"];
            userProfile.Bug = (string)stepContext.Values["bug"];

            await stepContext.Context.SendActivityAsync(MessageFactory.Text("Bug Report submitted 🚀. Here is a summary:"), cancellationToken);
            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Description: {userProfile.Description}"), cancellationToken);
            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Callback Time: {userProfile.CallbackTime}"), cancellationToken);
            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Phone Number: {userProfile.PhoneNumber}"), cancellationToken);
            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Bug: : {userProfile.Bug}"), cancellationToken);

            // Save profile in user state
            await _botStateService.UserProfileAccessor.SetAsync(stepContext.Context, userProfile, cancellationToken);

            // WaterfallStep always finishes with the end of the waterfall or with another dialog, here it is the end
            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
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
    }
}