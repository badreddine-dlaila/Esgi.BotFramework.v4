using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Extensions.Configuration;
using LuisPredictionOptions = Microsoft.Bot.Builder.AI.LuisV3.LuisPredictionOptions;

namespace DialogBot.Services
{
    public class BotServices
    {
        public LuisRecognizer Dispatch { get; set; }

        public BotServices(IConfiguration configuration)
        {
            // Read the settings for cognitive services (LUIS, QnA) from appsettings.json

            var applicationId   = configuration.GetValue<string>("LuisAppId");
            var endpointKey     = configuration.GetValue<string>("LuisAPIKey");
            var endpoint        = $"https://{configuration.GetValue<string>("LuisAPIHostName")}.api.cognitive.microsoft.com";
            var luisApplication = new LuisApplication(applicationId, endpointKey, endpoint);

            var recognizerOptions = new LuisRecognizerOptionsV3(luisApplication)
            {
                PredictionOptions = new LuisPredictionOptions
                {
                    IncludeAllIntents   = true,
                    IncludeInstanceData = true,
                    Slot                = configuration["LuisSlot"]
                }
            };

            Dispatch = new LuisRecognizer(recognizerOptions);
        }
    }
}
