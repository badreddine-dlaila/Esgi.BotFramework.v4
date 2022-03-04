using DialogBot.Models;
using DialogBot.Services;
using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DialogBot.Helpers;
using Microsoft.Bot.Builder;

namespace DialogBot.Dialogs;

/// <summary>
///     🧩 The glue dialog that tights GreetingDialog and BugReportDialog
/// </summary>
public class MainDialog : ComponentDialog
{
    private readonly BotStateService _botStateService;
    private readonly BotServices     _botServices;

    public MainDialog(BotStateService botStateService, BotServices botService) : base(nameof(MainDialog))
    {
        _botStateService = botStateService ?? throw new ArgumentNullException(nameof(botStateService));
        _botServices     = botService ?? throw new ArgumentNullException(nameof(botService));

        InitializeWaterfallDialog();
    }

    private void InitializeWaterfallDialog()
    {
        // Waterfall steps
        var waterfallSteps = new WaterfallStep[] { InitialStep, FinalStep };

        // Add named dialogs
        AddDialog(new GreetingDialog($"{nameof(MainDialog)}.greeting", _botStateService));   // <-- greeting subDialog
        AddDialog(new BugReportDialog($"{nameof(MainDialog)}.bugReport", _botStateService)); // <-- bugReport subDialog
        AddDialog(new BugTypeDialog($"{nameof(MainDialog)}.bugType", _botServices));
        AddDialog(new WaterfallDialog($"{nameof(MainDialog)}.mainFlow", waterfallSteps));

        // Set the starting dialog
        InitialDialogId = $"{nameof(MainDialog)}.mainFlow";
    }

    private async Task<DialogTurnResult> InitialStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        try
        {
            // First, we use the dispatch model to determine which cognitive service (LUIS, QnA) to use.
            var recognizerResult = await _botServices.Dispatch.RecognizeAsync<LuisModel>(stepContext.Context, cancellationToken);

            // Top intent tell us which cognitive service to use
            var (intent, _) = recognizerResult.TopIntent();
            switch (intent)
            {
                case LuisModel.Intent.GreetingIntent:
                    return await stepContext.BeginDialogAsync($"{nameof(MainDialog)}.greeting", null, cancellationToken);
                case LuisModel.Intent.NewBugReportIntent:
                    var userProfile = new UserProfile();
                    var bugReport   = recognizerResult.Entities.BugReport_ML?.FirstOrDefault();
                    if (bugReport is not null)
                    {
                        var description = bugReport.Description?.FirstOrDefault();
                        if (description != null)
                        {
                            // Retrieve Description Text
                            userProfile.Description = bugReport._instance.Description?.FirstOrDefault() is not null
                                ? bugReport._instance.Description.FirstOrDefault()?.Text
                                : userProfile.PhoneNumber;

                            // Retrieve Bug Text
                            var bugOuter = description.Bug?.FirstOrDefault();
                            if (bugOuter != null)
                                userProfile.Bug = bugOuter?.FirstOrDefault() != null ? bugOuter?.FirstOrDefault() : userProfile.Bug;
                        }

                        // Retrieve Phone Number Text
                        userProfile.PhoneNumber = bugReport.PhoneNumber?.FirstOrDefault() is not null
                            ? bugReport.PhoneNumber?.FirstOrDefault()
                            : userProfile.PhoneNumber;

                        // Retrieve Callback Time
                        userProfile.CallbackTime = bugReport.CallbackTime?.FirstOrDefault() != null ? AiRecognizer.RecognizeDateTime(bugReport.CallbackTime?.FirstOrDefault(), out _) : userProfile.CallbackTime;
                    }
                    return await stepContext.BeginDialogAsync($"{nameof(MainDialog)}.bugReport", userProfile, cancellationToken);
                case LuisModel.Intent.None:
                    break;
                case LuisModel.Intent.QueryBugTypeIntent:
                    return await stepContext.BeginDialogAsync($"{nameof(MainDialog)}.bugType", null, cancellationToken);
                default:
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("I'm sorry I don't know what you mean."), cancellationToken);
                    break;
            }
        }
        catch (Exception e)
        {
            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Oh Gosh !! I've crashed ({e.Message}) 🔥"), cancellationToken);
            throw;
        }

        return await stepContext.NextAsync(null, cancellationToken);
    }

    private static async Task<DialogTurnResult> FinalStep(WaterfallStepContext stepContext, CancellationToken cancellationToken) => await stepContext.EndDialogAsync(null, cancellationToken);
}