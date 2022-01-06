using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HelloBot.Models;
using HelloBot.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace HelloBot.Bots
{
    public class GreetingBot : ActivityHandler
    {
        private readonly StateService _stateService;

        public GreetingBot(StateService stateService)
        {
            _stateService = stateService ?? throw new ArgumentNullException(nameof(stateService));
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            await GetName(turnContext, cancellationToken);
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await GetName(turnContext, cancellationToken);
                }
            }
        }

        private async Task GetName(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            // Retrieve or instantiate UserProfile from UserState   
            var userProfile = await _stateService.UserProfileAccessor.GetAsync(turnContext, () => new UserProfile(), cancellationToken);
            var conversationData = await _stateService.ConversationDataAccessor.GetAsync(turnContext, () => new ConversationData(), cancellationToken);

            if (!string.IsNullOrEmpty(userProfile.Name))
            {
                var activity = MessageFactory.Text($"Hi {userProfile.Name}. What can I do for you today 🤠 ?");
                await turnContext.SendActivityAsync(activity, cancellationToken);
            }
            else
            {
                if (conversationData.PromptedUserForName)
                {
                    // Set the name to what the user provided
                    userProfile.Name = turnContext.Activity.Text?.Trim();

                    // Acknowledge that we got their name
                    await turnContext.SendActivityAsync(MessageFactory.Text($"Thanks {userProfile.Name}. How can I Help you today 🤠 ?"), cancellationToken);

                    // Reset the flag to allow the bot to go though the cycle again 
                    conversationData.PromptedUserForName = false;
                }
                else
                {
                    // Prompt the user for their name
                    await turnContext.SendActivityAsync(MessageFactory.Text("What is your name ?"), cancellationToken);

                    // Set the flag to true so we don't prompt in the next run
                    conversationData.PromptedUserForName = true;
                }

                // Save any changes that might have occurred during the run
                await _stateService.UserProfileAccessor.SetAsync(turnContext, userProfile, cancellationToken);
                await _stateService.ConversationDataAccessor.SetAsync(turnContext, conversationData, cancellationToken);

                await _stateService.UserState.SaveChangesAsync(turnContext, cancellationToken: cancellationToken);
                await _stateService.ConversationState.SaveChangesAsync(turnContext, cancellationToken: cancellationToken);
            }
        }
    }
}