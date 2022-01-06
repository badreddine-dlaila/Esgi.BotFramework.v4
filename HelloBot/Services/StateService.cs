using System;
using HelloBot.Models;
using Microsoft.Bot.Builder;

namespace HelloBot.Services
{
    public class StateService
    {
        // State variables
        public UserState         UserState         { get; set; }
        public ConversationState ConversationState { get; set; }

        // IDs
        // Identifies user profile data inside UserState bucket
        public static string UserProfileId      => $"{nameof(StateService)}.{nameof(UserProfile)}";
        public static string ConversationDataId => $"{nameof(StateService)}.{nameof(ConversationData)}";

        // Accessors
        public IStatePropertyAccessor<UserProfile>      UserProfileAccessor      { get; set; }
        public IStatePropertyAccessor<ConversationData> ConversationDataAccessor { get; set; }

        public StateService(UserState userState, ConversationState conversationState)
        {
            UserState = userState ?? throw new ArgumentException(nameof(userState));
            ConversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));

            InitializeAccessors();
        }

        private void InitializeAccessors()
        {
            // Tell the bucket about the variable UserProfile, identified by UserProfileId
            UserProfileAccessor = UserState.CreateProperty<UserProfile>(UserProfileId);

            // Tell the bucket about the variable ConversationData, identified by ConversationDataId
            ConversationDataAccessor = ConversationState.CreateProperty<ConversationData>(ConversationDataId);
        }
    }
}