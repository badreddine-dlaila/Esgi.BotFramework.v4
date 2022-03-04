using DialogBot.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using System;

namespace DialogBot.Services
{
    public class BotStateService
    {
        // State variables
        public UserState         UserState         { get; set; }
        public ConversationState ConversationState { get; set; }
        public DialogState       DialogState       { get; set; }

        // IDs
        // Identifies user profile data inside UserState bucket
        public static string UserProfileId      => $"{nameof(BotStateService)}.{nameof(UserProfile)}";
        public static string ConversationDataId => $"{nameof(BotStateService)}.{nameof(ConversationData)}";
        public static string DialogStateId      => $"{nameof(BotStateService)}.{nameof(DialogState)}";

        // Accessors
        public IStatePropertyAccessor<UserProfile>      UserProfileAccessor      { get; set; }
        public IStatePropertyAccessor<ConversationData> ConversationDataAccessor { get; set; }
        public IStatePropertyAccessor<DialogState>      DialogStateAccessor      { get; set; }

        public BotStateService(UserState userState, ConversationState conversationState)
        {
            UserState = userState ?? throw new ArgumentException(nameof(userState));
            ConversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));

            InitializeAccessors();
        }

        private void InitializeAccessors()
        {
            // Tell the bucket about the variable UserProfile, identified by UserProfileId
            UserProfileAccessor = UserState.CreateProperty<UserProfile>(UserProfileId);

            // Conversation State Accessors
            ConversationDataAccessor = ConversationState.CreateProperty<ConversationData>(ConversationDataId);
            DialogStateAccessor = ConversationState.CreateProperty<DialogState>(DialogStateId);
        }
    }
}