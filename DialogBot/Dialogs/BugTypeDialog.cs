using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DialogBot.Models;
using DialogBot.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;

namespace DialogBot.Dialogs
{
    public class BugTypeDialog : ComponentDialog
    {
        #region Variables

        private readonly BotServices _botServices;

        #endregion

        public BugTypeDialog(string dialogId, BotServices botServices) : base(dialogId)
        {
            _botServices = botServices ?? throw new ArgumentNullException(nameof(botServices));

            InitializeWaterfallDialog();
        }

        private void InitializeWaterfallDialog()
        {
            // Create Waterfall Steps
            var waterfallSteps = new WaterfallStep[] { InitialStepAsync, FinalStepAsync };

            // Add Named Dialogs
            AddDialog(new WaterfallDialog($"{nameof(BugTypeDialog)}.mainFlow", waterfallSteps));

            // Set the starting Dialog
            InitialDialogId = $"{nameof(BugTypeDialog)}.mainFlow";
        }

        private async Task<DialogTurnResult> InitialStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var result   = await _botServices.Dispatch.RecognizeAsync<LuisModel>(stepContext.Context, cancellationToken);
            var value    = string.Empty;
            var bugOuter = result.Entities.BugTypes_List?.FirstOrDefault();
            if (bugOuter != null)
                value = bugOuter?.FirstOrDefault() != null ? bugOuter?.FirstOrDefault() : value;

            if (Common.BugTypes.Any(s => s.Equals(value, StringComparison.OrdinalIgnoreCase)))
                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Yes! {value} is a Bug Type!"), cancellationToken);
            else
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("No that is not a bug type"), cancellationToken);

            return await stepContext.NextAsync(null, cancellationToken);
        }

        private static async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken) => await stepContext.EndDialogAsync(null, cancellationToken);
    }

    public static class Common
    {
        public static readonly List<string> BugTypes = new()
        {
            "Security",
            "Crash",
            "Power",
            "Performance",
            "Usability",
            "Serious Bug",
            "Other"
        };
    }
}


