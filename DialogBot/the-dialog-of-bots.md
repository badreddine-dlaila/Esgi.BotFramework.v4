## Dialogs

> Install-Package Microsoft.Bot.Builder.Dialogs -Version 4.15.0> 

```csharp
// Create Bots/DialogBot.cs

 public class DialogBot<T> : ActivityHandler where T : Dialog
    {
        private readonly StateService _stateService;
        private readonly T _dialog;
        private readonly ILogger<DialogBot<T>> _logger;

        public DialogBot(StateService stateService, T dialog, ILogger<DialogBot<T>> logger)
        {
            _stateService = stateService ?? throw new ArgumentException(nameof(stateService));
            _dialog = dialog ?? throw new ArgumentException(nameof(dialog)); ;
            _logger = logger ?? throw new ArgumentException(nameof(logger)); ;
        }
    }

// Talk about Generics in c#

// Override 
// OnTurnAsync --> Base should be called ⚡
// OnMessageActivityAsync 
```

```csharp
// Create Dialog Extension ⚡
public static async Task Run(this Dialog dialog, ITurnContext turnContext, IStatePropertyAccessor<DialogState> accessor, CancellationToken cancellationToken)
{
    var dialogSet = new DialogSet(accessor);
    // ...
    
    // create dialogContext to interacte with dialogSet
    // dialog context include : current TurnContext, Parent dialog & dialog (provides a method for preserving information within a dialog)
    var dialogContext = await dialogSet.CreateContextAsync(turnContext, cancellationToken);
    
    // ...
    
    // dialogContext allows to start a dialog by id or contiunue the current dialog depending on the use case
    // Essentially provides a way that we can create & inject any dialog 
     await dialogContext.BeginDialogAsync(dialog.Id, null, cancellationToken);
}

// Add DialogState in StateService
// Configure Loggin Middleware in program.cs
.ConfigureLogging(builder =>
                {
                    builder.AddDebug();
                    builder.AddConsole();
                })
```

```csharp
// refactor existing dialogs in /Dialogs/GreetingDialog.cs 
public class GreetingDialog : ComponentDialog
{ 
     public GreetingDialog(string dialogId, BotStateService botStateService) : base(dialogId)
    {
          // ..
          InitWaterfallDialog();
    }

    // Simple pattern for simple usage
    private void InitWaterfallDialog()
        {
            // Create waterfall steps
            // Add named dialogs
            // set the starting dialog
        }
}

// Another dialog more intresting (BugReport Dialog 🪲)

// Create waterfall steps
var waterfallSteps = new List<WaterfallStep>
{
    DescriptionStep, // TextPrompt
    CallbackStep,    // DateTimePrompt ❤️
    PhoneNumberStep, // TextPrompt // https://regex101.com/r/0kpBet/1
    BugStep,         // ChoicePrompt ❤️
    SummaryStep      // TextPrompt
};
// ..
AddDialog(new DateTimePrompt($"{nameof(BugReportDialog)}.callbackTime", ValidateCallbackTime ❤️));
AddDialog(new TextPrompt($"{nameof(BugReportDialog)}.phoneNumber", ValidatePhoneNumber ❤️));
// ..
```

```csharp
// The glue 🧩
public class MainDialog : ComponentDialog
{
    private readonly BotStateService _botStateService;
    public MainDialog(BotStateService botStateService)
    {
        _botStateService = botStateService ?? throw new ArgumentException(nameof(botStateService));
        InitWaterfall();
    }
    private void InitWaterfall()
    {
        // Create waterfall steps
        var waterfallSteps = new List<WaterfallStep>
        {
            InitialStep,
            FinalStep
        };
        // Add named dialogs 
        AddDialog(new GreetingDialog($"{nameof(MainDialog)}.greeting", _botStateService));  // <-- greeting subDialog
        AddDialog(new BugReportDialog($"{nameof(MainDialog)}.bugReport", _botStateService)); // <-- bugReport subDialog
        AddDialog(new WaterfallDialog($"{nameof(MainDialog)}.mainFlow", waterfallSteps));
        // set the starting dialog
        InitialDialogId = $"{nameof(GreetingDialog)}.mainFlow";
    }
}

// --> What else to make it work (+1) 🤑
// Go ahead to startup.cs and do your business

```


