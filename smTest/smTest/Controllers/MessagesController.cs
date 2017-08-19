using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Data;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Microsoft.ProjectOxford.Vision;
using System.Drawing;
using ZXing;

namespace smTest
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
       
        HtmlWeb client = new HtmlWeb();
        //private object txtDecoderType;

        private async Task<List<Resume>> DetailsFromPage()
        {
            var doc = await Task.Factory.StartNew(() => client.Load("https://taft.coa.gov.tw/rsm/Code_cp.aspx?ID=1527616&EnTraceCode=10608110970"));
            var dateNodes = doc.DocumentNode.SelectNodes("//*[@id=\"tableSort\"]//tr/td[1]");
            var typeNodes = doc.DocumentNode.SelectNodes("//*[@id=\"tableSort\"]//tr/td[2]");
            var contentNodes = doc.DocumentNode.SelectNodes("//*[@id=\"tableSort\"]//tr/td[3]");
            var refNodes = doc.DocumentNode.SelectNodes("//*[@id=\"tableSort\"]//tr//td[4]");

            if (dateNodes == null || typeNodes == null || contentNodes == null)
            {
                return new List<Resume>();
            }

            var innerDate = dateNodes.Select(node => node.InnerText).ToList();
            var innerTypes = typeNodes.Select(node => node.InnerText).ToList();
            var innerContent = contentNodes.Select(node => node.InnerText).ToList();
            var innerRef = refNodes.Select(node => node.InnerText).ToList();

            List<Resume> toReturn = new List<Resume>();

            for (int i = 0; i < innerDate.Count(); ++i)
            {
                toReturn.Add(new Resume() { Date = innerDate[i], Type = innerTypes[i], Content = innerContent[i], Ref = innerRef[i] });
            }

            return toReturn;
        }
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
        
            if (activity.Type == ActivityTypes.Message)
            {
                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                Activity reply = activity.CreateReply();
                await Conversation.SendAsync(activity, () => new Dialogs.RootDialog());

                if (activity.Attachments?.Count > 0 && activity.Attachments.First().ContentType.StartsWith("image"))//IF NULL 不會往下,有東西才繼續run
                {
                    //user傳一張照片
                    ImageTemplate(reply, activity.Attachments.First().ContentUrl);

                }
                else if (activity.ChannelId == "facebook")
                {
                    //讀fb data
                    var fbData = JsonConvert.DeserializeObject<FBChannelModel>(activity.ChannelData.ToString());
                    if (fbData.postback != null && fbData.postback.payload.StartsWith("Analyze"))
                    {
                        var url = fbData.postback.payload.Split('>')[1];

                        //vision

                        VisionServiceClient client = new VisionServiceClient("786ccca1c75d434dbbffd67a8194942b", "https://westcentralus.api.cognitive.microsoft.com/vision/v1.0");
                        var result = await client.AnalyzeImageAsync(url, new VisualFeature[] { VisualFeature.Description });
                        reply.Text = result.Description.Captions.First().Text;

                    }

                    else if (fbData.message.quick_reply != null)
                    {
                        reply.Text = $"your choice is {fbData.message.quick_reply.payload}";
                    }
                    else if (activity.Text == "t")
                    {
                        var infos = await DetailsFromPage();
                        
                        var a = infos.GetType().ToString();
                        var b = infos[1].Content;
                        reply.Text = "選項quick menu";

                        reply.SuggestedActions = new SuggestedActions()
                        {
                            Actions = new List<CardAction>()
                        {
                            new CardAction(){Title="詳細生產紀錄", Type=ActionTypes.ImBack, Value = b.ToString() },
                            new CardAction(){Title="選項一", Type=ActionTypes.OpenUrl, Value="www.google.com"},
                            new CardAction(){Title="選項二", Type=ActionTypes.OpenUrl, Value="https://statementdog.com"},
                        }
                        };
                    }
                    else if (activity.Text == "scrap")
                    {
                        var infos2 = await DetailsFromPage();
                        

                        string.Join(", ", infos2);
                        string alltext = " ";
                        
                        foreach (var info in infos2)
                        {
                            alltext += ",";
                            alltext += String.Join(", ", info.Type);
                            //alltext += info.Date;

                            //table.Rows.Add(info.Date, info.Type, info.Content, info.Ref);
                        }
                        //alltext = String.Join(", ", infos2[4]);
                        if (alltext != null)
                        {
                            
                            reply.Text = alltext;
                        }
                        else
                        {
                            reply.Text = "webScrap2";
                        }
                        
                    }
                    //quick menu
                    else if (activity.Text == "quick")
                    {
                        reply.Text = "test";
                        reply.SuggestedActions = new SuggestedActions()
                        {
                            Actions = new List<CardAction>()
                        {
                            new CardAction(){Title="USD", Type=ActionTypes.ImBack, Value="這是美金"},
                            new CardAction(){Title="連結", Type=ActionTypes.OpenUrl, Value="www.google.com"},
                            new CardAction(){Title="1565", Type=ActionTypes.OpenUrl, Value="https://statementdog.com"},
                        }
                        };
                    }
                    else
                    {
                        reply.Text = "??????";

                    }
                }
           

                

                else
                {
                    reply.Text = $"echo:{activity.Text}";
                }
                await connector.Conversations.ReplyToActivityAsync(reply);
            }


           
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        private void ImageTemplate(Activity reply, string url)
        {
            /* List<Attachment> att = new List<Attachment>();
            att.Add(new HeroCard() //建立fb ui格式的api
            {
                Title = "Cognitive services",
                Subtitle = "Select from below",
                Images = new List<CardImage>() { new CardImage(url) },
                Buttons = new List<CardAction>()
                {
                    new CardAction(ActionTypes.PostBack, "辨識圖片", value: $"Analyze>{url}"),//帶json payload
                    new CardAction(ActionTypes.PostBack, "decode QR", value: $"Analyze>{url}")
                }
            }.ToAttachment());*/

            // create a barcode reader instance
            IBarcodeReader reader = new BarcodeReader();
            // load a bitmap
            var barcodeBitmap = ImageFromWeb(url);
            // detect and decode the barcode inside the bitmap
            var result = reader.Decode(barcodeBitmap);
            // do something with the result
            if (result != null)
            {
                //txtDecoderType.Text = result.BarcodeFormat.ToString();
                reply.Text = result.Text;
            }

            //reply.Attachments = att;
        }

        private Bitmap ImageFromWeb(string url)
        {
            System.Net.WebRequest request =
                    System.Net.WebRequest.Create(
                    url);
            System.Net.WebResponse response = request.GetResponse();
            System.IO.Stream responseStream =
                response.GetResponseStream();
            Bitmap bitmap2 = new Bitmap(responseStream);
            return bitmap2;
        }

    }
}