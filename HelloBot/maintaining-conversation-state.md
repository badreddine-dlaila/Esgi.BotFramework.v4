### Flashback

```ps
# EchoBot
EchoBot : ActivityHandler --> : IBot
--> OnTurnAsync 

--> OnMessageActivityAsync
* Run demo Hello 

--> OnMembersAddedAsync
If statement (what for?)
From vs Recipient
The bot itself gets added (To no greet when bot gets added)

--> Startup (DI)
--> BotController
```

### Greeting Demo
```ps
# Purpose : More complex 
#           Juste greet the user by its name
#           Bot shoud remember user's name --> need state to save username
# Requirement : StatePropertyAccesor + StateProperty 
# Things we can may be store : name, id preferences, personal infomation, ... 
```

```csharp
// Bots/GreetingBot.cs
 public class GreetingBot : ActivityHandler
    {
        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                }
            }
        }
    }
```

```csharp
// New Models/UserProfile.cs
public string Name { get; set; }

// New Services/StateService.cs
// Wich bucket ? UserState/ConversationState/ProvateConversationState

public class StateService
{
    // state variables
    public UserState UserState { get; }
    // IDs
    // Identifies user profile data inside UserState bucket
    public static string UserProfileId => $"{nameof(StateService)}.UserProfile";
    public IStatePropertyAccessor<UserProfile> UserProfileAccessor { get; set; } // <-- Look At IStatePropertyAccessor

    public StateService(UserState userState)
        {
            UserState = userState ?? throw new ArgumentException(nameof(userState));
            // Tell the bucket about the variable UserProfile, identfied by UserProfileId 
            UserProfileAccessor = userState.CreateProperty<UserProfile>(UserProfileId);
        }
}
```

```csharp
private async Task GetName(ITurnContext turnContext, CancellationToken cancellationToken)
{
    var userProfile = await _stateService.UserProfileAccessor.GetAsync(turnContext, () => new UserProfile(), cancellationToken);
    var conversationData = await _stateService.ConversationDataAccessor.GetAsync(turnContext, () => new ConversationData(), cancellationToken);
    
    if (!string.IsNullOrEmpty(userProfile.Name))
    {
        var activity = MessageFactory.Text($"Hi {userProfile.Name}. How can I do for you today 🤠 ?");
        await turnContext.SendActivityAsync(activity, cancellationToken);
    }
    else
    {
        if (conversationData.PromptedUserForName)
        {
            // Set the name to what the user provided
            // Acknowledge that we got their name
            // Reset the flag to allow the bot to go though the cycle again 
        }
        else
        {
            // Prompt the user for their name
            // Set the flag to true so we don't prompt in the next run
        }

        // Save any changes that might have occurred during the run
    }
}

// Startup.cs
services.AddTransient<IBot, GreetingBot>();
services.AddSingleton<IStorage, MemoryStorage>();
services.AddSingleton<UserState>(); // <-- check implementation for IStorage injection
services.AddSingleton<ConversationState>();
services.AddSingleton<StateService>();

// Play with emulator 
// Restart bot

// Azure Account creation
// https://signup.azure.com/studentverification?offerType=1&correlationId=f35177daf55b405bbbaee80aec132e76
// MemoryStorage --> BlobStorage (Nuget : Microsoft.Bot.Builder.Azure.Blobs)
var connectionString = Configuration.GetValue<string>("BlobStorageConnectionString");
const string container = "mystatedata";
var blobsStorage = new BlobsStorage(connectionString, container);
services.AddSingleton<IStorage>("BlobStorageConnectionString", container);
```


