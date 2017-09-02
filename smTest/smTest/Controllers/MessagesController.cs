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

using Newtonsoft.Json.Linq;

using System.Net.Http.Headers;
using Autofac;


namespace smTest
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
       
        HtmlWeb client = new HtmlWeb();
        //private object txtDecoderType;
        
       
        //////////////
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
        
            if (activity.Type == ActivityTypes.Message)
            {
                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                Activity reply = activity.CreateReply();
                //await Conversation.SendAsync(activity, () => new Dialogs.RootDialog());
                
                //user傳一張照片
                if (activity.Attachments?.Count > 0 && activity.Attachments.First().ContentType.StartsWith("image"))//IF NULL 不會往下,有東西才繼續run
                {
                   string uriName = decodeQRCode(reply, activity.Attachments.First().ContentUrl);
                   string rt = await PassScrapTextAsync(uriName);
                   reply.Text = rt+ uriName;

                   
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
                    //quick menu
                    else if (activity.Text == "我可以幹嘛")
                    {
                        reply.Text = "請選擇按鈕 from mac";
                        reply.SuggestedActions = new SuggestedActions()
                        {
                            Actions = new List<CardAction>()
                            {
                                new CardAction(){Title="輸入追朔碼", Type=ActionTypes.ImBack, Value="輸入追朔碼2"},
                                new CardAction(){Title="上傳QR code", Type=ActionTypes.ImBack, Value="上傳QR code2"},
                                new CardAction(){Title="去產銷履歷網站", Type=ActionTypes.OpenUrl, Value="http://taft.coa.gov.tw/"},
                            }
                        };
                        
                    }
                    else if (activity.Text == "try")
                    {
                        GeneTemplate(reply);
                    }

                    else if (activity.Text == "上傳QR code")
                    {
                        reply.Text = "請上傳QR code圖片";
                        //上傳圖片之後跑的東西

                        if (activity.Attachments?.Count > 0 && activity.Attachments.First().ContentType.StartsWith("image"))//IF NULL 不會往下,有東西才繼續run
                        {
                            //user傳一張照片
                            decodeQRCode(reply, activity.Attachments.First().ContentUrl);
   
                        }
                    }

              

                    else if (fbData.message.quick_reply != null)
                    {
                        reply.Text = $"your choice is {fbData.message.quick_reply.payload}";
                    }


                    else
                    {
                        //如果傳網址 一樣爬的到

                        string rt = await PassScrapTextAsync(activity.Text);
                        reply.Text = "!!!!" + rt + "!!!!";
                        
                        //reply.Text = $"echo:{activity.Text}";
                        
                    }
                }


                else
                {
                    GeneTemplate(reply);

                    //reply.Text = "@@@@";
                }
                await connector.Conversations.ReplyToActivityAsync(reply);
            }
            
            
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        private async Task<String> PassScrapTextAsync(string uriName)
        {

            Uri uriResult;
            bool result = Uri.TryCreate(uriName, UriKind.Absolute, out uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
            if (result == true)
            {

                var infos = await DetailsFromPage(uriName);
                string alltext = "作業日期\t\t作業種類\t\t作業內容\n\n============================\n\n";

                foreach (var info in infos)
                {
                    alltext += String.Join(", ", info.Date);
                    alltext += "\t\t";
                    alltext += String.Join(", ", info.Type);
                    alltext += "\t\t";
                    alltext += String.Join(", ", info.Content);
                    alltext += "\n\n";

                    //table.Rows.Add(info.Date, info.Type, info.Content, info.Ref);
                }
                return alltext;
            }
            else
            {
                return result.ToString() + " 不是網址!";
            }
        }

      
        //讀取QrCode
        private string decodeQRCode(Activity reply, string url)
        {
            // create a barcode reader instance
            IBarcodeReader reader = new BarcodeReader();
            // load a bitmap
            var barcodeBitmap = ImageFromWeb(url);
            // detect and decode the barcode inside the bitmap
            var result = reader.Decode(barcodeBitmap);
            // do something with the result
            if (result != null)
            { 
                return result.Text;
            }
            return result.Text;
        }

        //讀取圖片
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

        private void GeneTemplate(Activity reply)
        {
            List<Attachment> att = new List<Attachment>();
            att.Add(new HeroCard() //建立fb ui格式的api
            {
                Title = "查詢選項",
                Subtitle = "Select from below",
                Images = new List<CardImage>() { new CardImage("https://cdn.ready-market.com/1/9816a644//Templates/pic/vegetable.jpg?v=0d7a3372") },
                Buttons = new List<CardAction>()
                {
                    new CardAction(){ Title = "詳細生產履歷", Type=ActionTypes.OpenUrl, Value= "http://taft.coa.gov.tw/" },
                    new CardAction(){Title = "產地2", Type= ActionTypes.ImBack, Value= $"南投" },

                }
            }.ToAttachment());

            reply.Attachments = att;
            
        }

        //爬蟲的function
        private async Task<List<Resume>> DetailsFromPage(string url)
        {
            var doc = await Task.Factory.StartNew(() => client.Load(url));
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
    }
    
}