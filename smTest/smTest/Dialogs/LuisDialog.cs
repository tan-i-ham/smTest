using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace smTest.Dialogs
{
    [LuisModel("6dff0659-ba69-4964-8d8c-c31a9967f959", "88c80043fc3e46bb951e7a95d1210738")]
    [Serializable]
    public class LuisDialog : LuisDialog<object>
    {
        [LuisIntent("Card")]
        public async Task Card(IDialogContext context, LuisResult result)
        {
            var reply = context.MakeMessage();
            reply.Attachments = new List<Attachment>();
            List<CardImage> images = new List<CardImage>();
            CardImage ci = new CardImage("https://tw.images.search.yahoo.com/search/images;_ylt=A8tUwZe5U6lZqRcAlQlr1gt.;_ylu=X3oDMTE0NW5ydGk3BGNvbG8DdHcxBHBvcwMxBHZ0aWQDQjQwNDFfMQRzZWMDcGl2cw--?p=AZURE&fr2=piv-web&fr=yset_ie_syc_oracle#id=4&iurl=http%3A%2F%2Faiscaler.com%2Fwp-content%2Fuploads%2F2014%2F07%2Fmicrosoft-azure-logo.jpg&action=click");
            images.Add(ci);
            CardAction ca = new CardAction()
            {
                Title = "Visit Support",
                Type = "openUrl",
                Value = "http://www.intelligentlabs.co.uk"
            };
            ThumbnailCard tc = new ThumbnailCard()
            {
                Title = "Need help?",
                Subtitle = "Go to our main site support.",
                Images = images,
                Tap = ca
            };
            reply.Attachments.Add(tc.ToAttachment());
            await context.PostAsync(reply);
            context.Wait(MessageReceived);

        }

        [LuisIntent("TeamCount")]
        public async Task SayHi(IDialogContext context, LuisResult result)
        {
            var time = DateTime.Now.ToString("HH:mm:ss tt");
            await context.PostAsync(time);



            List<string> Options = new List<string>();
            Options.Add("輸入追朔碼");
            Options.Add("上傳QR code");
            Options.Add("去產銷履歷網站");
            PromptOptions<string> Selections = new PromptOptions<string>("請選擇三個其中一個", "請再選一次", "最後一次", Options, 2);
            PromptDialog.Choice<string>(context, ChooseSlection, Selections);
            //context.Wait(MessageReceived);
        }
        public async Task ChooseSlection(IDialogContext context, IAwaitable<string> result)
        {
            string selection = await result;

            if (selection == "輸入追朔碼")
            {
                await context.PostAsync($"追溯瑪");
            }
            else if (selection == "上傳QR code")
            {
                await context.PostAsync($"Qr code");
            }
            else if (selection == "去產銷履歷網站")
            {
                await context.PostAsync($"履歷");
            }
            context.Wait(MessageReceived);
        }


        [LuisIntent("Select")]

        public async Task reply(IDialogContext context, LuisResult result)
        {
            var time = DateTime.Now.ToString("HH:mm:ss tt");
            if (result.Query == "輸入追朔碼")
            {
                await context.PostAsync($"Qr");
            }
            else if (result.Query == "上傳QR code")
            {
                await context.PostAsync($"code");
            }
            else if (result.Query == "去產銷履歷網站")
            {
                await context.PostAsync($"2");
            }
            context.Wait(MessageReceived);
        }

        [LuisIntent("")]

        public async Task nothing(IDialogContext context, LuisResult result)
        {
            var time = DateTime.Now.ToString("HH:mm:ss tt");
            await context.PostAsync(time);
            if (result.Query == "輸入追朔碼")
            {
                await context.PostAsync($"Qr");
            }
            else if (result.Query == "上傳QR code")
            {
                await context.PostAsync($"code");
            }
            else if (result.Query == "去產銷履歷網站")
            {
                var reply = context.MakeMessage();
                reply.Attachments = new List<Attachment>();
                List<CardImage> images = new List<CardImage>();
                CardImage ci = new CardImage("http://intelligentlabs.co.uk/images/IntelligentLabs-White-Small.png");
                images.Add(ci);
                CardAction ca = new CardAction()
                {
                    Title = "Visit Support",
                    Type = "openUrl",
                    Value = "http://www.intelligentlabs.co.uk"
                };
                ThumbnailCard tc = new ThumbnailCard()
                {
                    Title = "Need help?",
                    Subtitle = "Go to our main site support.",
                    Images = images,
                    Tap = ca
                };
                reply.Attachments.Add(tc.ToAttachment());
                await context.PostAsync(reply);
            }
            else
                await context.PostAsync($"I don't know what you say");
            context.Wait(MessageReceived);
        }
    }
}