using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;

namespace DialogBot.Helpers
{
    public static class DialogExtensions
    {
        public static async Task Run(this Dialog dialog, ITurnContext turnContext, IStatePropertyAccessor<DialogState> accessor, CancellationToken cancellationToken)
        {
            var dialogSet = new DialogSet(accessor);
            dialogSet.Add(dialog);

            // create dialogContext to interact with dialogSet
            // dialog context include : current TurnContext, Parent dialog & dialog (provides a method for preserving information within a dialog)
            var dialogContext = await dialogSet.CreateContextAsync(turnContext, cancellationToken);
            var result        = await dialogContext.ContinueDialogAsync(cancellationToken);

            if (result.Status == DialogTurnStatus.Empty)
                // dialogContext allows to start a dialog by id or continue the current dialog depending on the use case
                // Essentially provides a way that we can create & inject any dialog 
                await dialogContext.BeginDialogAsync(dialog.Id, null, cancellationToken);
        }
    }
}
