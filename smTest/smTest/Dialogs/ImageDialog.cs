using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace smTest.Dialogs
{
    [Serializable]
    public class ImageDialog : IDialog<object>
    {
        protected string payee;
        protected string amount;

        public Task StartAsync(IDialogContext context)
        {
            
            context.PostAsync($"[ImageDialog] I am the image dialog.");
            context.Wait(MessageReceivedStart);


            return Task.CompletedTask;
        }

        public async Task MessageReceivedStart(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            Trace.TraceInformation("ImageDialog.");
            var message = Cards.CreateImBackHeroCard(context.MakeMessage(), $"[MainDialog] I'm banking 🤖{Environment.NewLine}Would you like to?", new string[] { "Check balance", "Make payment" });
            await context.PostAsync(message);
            context.Wait(MessageReceivedOperationChoice);
        }
        public async Task MessageReceivedOperationChoice(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var message = await argument;

            if (message.Text.Equals("check balance", StringComparison.InvariantCultureIgnoreCase))
            {
                // State transition - add 'check balance' Dialog to the stack, when done call AfterChildDialogIsDone callback

                
                this.payee = message.Text;

                await context.PostAsync($"[MakePaymentDialog] {this.payee}, got it{Environment.NewLine}How much should I pay?");
                await context.PostAsync(message);
            }
            else if (message.Text.Equals("make payment", StringComparison.InvariantCultureIgnoreCase))
            {
                // State transition - add 'make payment' Dialog to the stack, when done call AfterChildDialogIsDone callback
                await context.PostAsync(message.Text + "2222");
            }
            else
            {
                // State transition - wait for 'start' message from user (loop back)
                context.Wait(MessageReceivedStart);
            }
        }


    }
}