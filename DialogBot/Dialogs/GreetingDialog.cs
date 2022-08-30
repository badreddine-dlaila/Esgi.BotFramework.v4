using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DialogBot.Models;
using DialogBot.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;

namespace DialogBot.Dialogs
{
    public class GreetingDialog : ComponentDialog
    {
        private readonly BotStateService _botStateService;

        public GreetingDialog(string dialogId, BotStateService botStateService) : base(dialogId)
        {
            _botStateService = botStateService ?? throw new ArgumentNullException(nameof(botStateService));

            InitializeWaterfallDialog();
        }

        private void InitializeWaterfallDialog()
        {
            // Create waterfall steps
            // Create waterfall steps (waterfall = back and forth template to utilize for conversation)
            var waterfallSteps = new WaterfallStep[]
            {
                InitialStep, // <-- waterfall step (method order is important)
                GithubSignInStep,
                FinalStep
            };

            // Add named dialogs
            AddDialog(new WaterfallDialog($"{nameof(GreetingDialog)}.mainFlow", waterfallSteps)); // <-- subDialog1
            AddDialog(new TextPrompt($"{nameof(GreetingDialog)}.name"));                          // <-- subDialog2  
            AddDialog(new TextPrompt($"{nameof(GreetingDialog)}.signin"));                        // <-- subDialog3  

            // set the starting dialog
            InitialDialogId = $"{nameof(GreetingDialog)}.mainFlow";
        }

        private async Task<DialogTurnResult> InitialStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Check if we have the user's name in state
            var userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile(), cancellationToken);

            // If we have the user's name, move to the next step of the dialog
            if (!string.IsNullOrEmpty(userProfile.Name))
                return await stepContext.NextAsync(null, cancellationToken);

            // If no user name found, kick start a text prompt dialog
            var promptOptions = new PromptOptions { Prompt = MessageFactory.Text("What is your name 😺 ?") };
            return await stepContext.PromptAsync($"{nameof(GreetingDialog)}.name", promptOptions, cancellationToken);
        }

        private static async Task<DialogTurnResult> GithubSignInStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            const string signinLink = "https://github.com/login/oauth/authorize" +
                                      "?client_id=0e34b593be4881da90ab&scope=user%20repo" +
                                      "&redirect_uri=https://localhost:3979/api/oauth/callback";
            var signinCard = new SigninCard
            {
                Text    = "Please sign-in to Github 🤖",
                Buttons = new List<CardAction> { new(ActionTypes.Signin, "Sign-in", value: signinLink) }
            };

            await stepContext.Context.SendActivityAsync(MessageFactory.Attachment(signinCard.ToAttachment()), cancellationToken);

            var promptOptions = new PromptOptions { Prompt = new Activity { Type = ActivityTypes.Message } };
            return await stepContext.PromptAsync($"{nameof(GreetingDialog)}.signin", promptOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Check if we have the user's name in state
            var userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile(), cancellationToken);

            // Get the previously typed response from the user 
            if (string.IsNullOrEmpty(userProfile.Name))
            {
                // Set the name
                userProfile.Name = (string)stepContext.Result;

                // Save any changes that might have occurred during the run
                await _botStateService.UserProfileAccessor.SetAsync(stepContext.Context, userProfile, cancellationToken);
            }

            var activity = MessageFactory.Text($"How can help you today {userProfile.Name} ?");
            await stepContext.Context.SendActivityAsync(activity, cancellationToken);

            // End dialog
            return await stepContext.EndDialogAsync(null, cancellationToken);
        }
    }
}
