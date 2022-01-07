using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Schema;

namespace DialogBot.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OauthController : ControllerBase
    {
        private readonly IBotFrameworkHttpAdapter                            _adapter;
        private readonly ConcurrentDictionary<string, ConversationReference> _conversationReferences;

        public OauthController(IBotFrameworkHttpAdapter adapter, ConcurrentDictionary<string, ConversationReference> conversationReferences)
        {
            _adapter = adapter;
            _conversationReferences = conversationReferences;
        }

        [HttpGet("callback")]
        public async Task<string> Callback(string code)
        {
            foreach (var conversationReference in _conversationReferences.Values)
            {
                Task BotCallbackHandler(ITurnContext context, CancellationToken token) => context.SendActivityAsync(code, cancellationToken: token);
                await ((BotAdapter)_adapter).ContinueConversationAsync(string.Empty, conversationReference, BotCallbackHandler, default);
            }

            return await Task.FromResult(code);
        }
    }
}