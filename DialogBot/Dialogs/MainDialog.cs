using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DialogBot.Services;
using Microsoft.Bot.Builder.Dialogs;

namespace DialogBot.Dialogs
{
    /// <summary>
    /// 🧩 The glue dialog that tights GreetingDialog and BugReportDialog 
    /// </summary>
    public class MainDialog : ComponentDialog
    {
        private readonly BotStateService _botStateService;

        public MainDialog (BotStateService botStateService) : base(nameof(MainDialog))
        {
            _botStateService = botStateService ?? throw new ArgumentNullException(nameof(botStateService));

            InitializeWaterfallDialog();
        }

        private void InitializeWaterfallDialog()
        {
            // Waterfall steps
            var waterfallSteps = new WaterfallStep[] { InitialStep, FinalStep };

            // Add named dialogs
            AddDialog(new GreetingDialog($"{nameof(MainDialog)}.greeting", _botStateService));      // <-- greeting subDialog
            AddDialog(new BugReportDialog($"{nameof(MainDialog)}.bugReport", _botStateService));    // <-- bugReport subDialog
            AddDialog(new WaterfallDialog($"{nameof(MainDialog)}.mainFlow", waterfallSteps));

            // Set the starting dialog
            InitialDialogId = $"{nameof(MainDialog)}.mainFlow";
        }

        private static async Task<DialogTurnResult> InitialStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (Regex.Match(stepContext.Context.Activity.Text.ToLower(), "hi").Success)
                return await stepContext.BeginDialogAsync($"{nameof(MainDialog)}.greeting", null, cancellationToken);

            return await stepContext.BeginDialogAsync($"{nameof(MainDialog)}.bugReport", null, cancellationToken);
        }

        private static async Task<DialogTurnResult> FinalStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.EndDialogAsync(null, cancellationToken);
        }
    }
}