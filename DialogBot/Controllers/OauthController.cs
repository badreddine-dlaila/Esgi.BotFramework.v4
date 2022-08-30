using System.Collections.Concurrent;
using System.Net;
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
            _adapter                = adapter;
            _conversationReferences = conversationReferences;
        }

        [HttpGet("callback")]
        public async Task<IActionResult> Callback(string code)
        {
            foreach (var conversationReference in _conversationReferences.Values)
            {
                async Task BotCallbackHandler(ITurnContext context, CancellationToken token) => await context.SendActivityAsync(code, cancellationToken: token);
                await ((BotAdapter)_adapter).ContinueConversationAsync(conversationReference.Bot.Id, conversationReference, BotCallbackHandler, default);
            }

            // Let the caller know proactive messages have been sent
            return new ContentResult
            {
                Content     = $"<html><body><h1>{code}</h1></body></html>",
                ContentType = "text/html",
                StatusCode  = (int)HttpStatusCode.OK
            };
        }
    }
}
