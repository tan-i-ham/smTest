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
        Recipe[] topRecipe = new Recipe[5];
        Product[] ProductInfo = new Product[1];
        Resume[] ProductResume = new Resume[100];
  
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
        
            if (activity.Type == ActivityTypes.Message)
            {
                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                Activity reply = activity.CreateReply();
                
                //user傳一張照片
                if (activity.Attachments?.Count > 0 && activity.Attachments.First().ContentType.StartsWith("image"))//IF NULL 不會往下,有東西才繼續run
                {
                // uriName 是decode qr code 完後的網址
                   string uriName = decodeQRCode(reply, activity.Attachments.First().ContentUrl);
                // receivedText 是爬蟲過後的訊息
                   reply.Attachments =  await PassScrapTextAsync(reply,uriName);
                   //reply.Text = receivedText + uriName;
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
                        GenericTemplate(reply);
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

                        //string rt = await PassScrapTextAsync(activity.Text);
                        //reply.Text = "!!!!" + rt + "!!!!";

                        //reply.Text = $"echo:{activity.Text}";

                        //用 luis 去偵測使用者的意思
                        await ProcessLUIS(activity,activity.Text);


                    }
                }


                else
                {
                    GenericTemplate(reply);

                    //reply.Text = "@@@@";
                }
                await connector.Conversations.ReplyToActivityAsync(reply);
            }
            
            
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        private async Task ProcessLUIS(Activity activity, string text)
        {
            await Conversation.SendAsync(activity, () => new Dialogs.LuisDialog());

        }


        private async Task<IList<Attachment>> PassScrapTextAsync(Activity context,string uriName)
        {
            Uri uriResult;
            bool result = Uri.TryCreate(uriName, UriKind.Absolute, out uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);


            List<Attachment> att = new List<Attachment>();
            if (result == true)
            {

                int cnt = await ProductionRecordAsync (uriName, ProductResume);
                // string alltext = "作業日期\t\t作業種類\t\t作業內容\n\n============================\n\n";
              
                if (cnt != -1) {
                    for (int i = 0; i < cnt; ++i) {
                        ThumbnailCard tc = new ThumbnailCard()
                        {
                            Title = ProductResume[i].Date,
                            Subtitle = ProductResume[i].Type + "\t" + ProductResume[i].Content ,
                            
                        };
                    
                        att.Add(tc.ToAttachment());
    
                    }
                } else {
                    context.Text = "failed!!";
                }

                return att;
            }
            else
            {
                return att;

                //context.Text = result.ToString() + " 不是網址!";
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

        private void GenericTemplate(Activity reply)
        {
            List<Attachment> att = new List<Attachment>();
            att.Add(new HeroCard() //建立fb ui格式的api
            {
                Title = "查詢選項",
                Subtitle = "選任一項執行功能",
                Images = new List<CardImage>() { new CardImage("https://cdn.ready-market.com/1/9816a644//Templates/pic/vegetable.jpg?v=0d7a3372") },
                Buttons = new List<CardAction>()
                {
                    new CardAction(){ Title = "詳細生產履歷", Type=ActionTypes.OpenUrl, Value= "http://taft.coa.gov.tw/" },
                    new CardAction(){Title = "產地2", Type= ActionTypes.ImBack, Value= $"南投" },

                }
            }.ToAttachment());

            reply.Attachments = att;
            
        }

        // 擷取產品履歷資料
        private async Task<int> ProductionRecordAsync(string url, Resume[] resume)
        {
            var doc = await Task.Factory.StartNew(() => client.Load(url));

            var dateNodes = doc.DocumentNode.SelectNodes("//*[@id=\"tableSort\"]//tr/td[1]");
            var typeNodes = doc.DocumentNode.SelectNodes("//*[@id=\"tableSort\"]//tr/td[2]");
            var contentNodes = doc.DocumentNode.SelectNodes("//*[@id=\"tableSort\"]//tr/td[3]");
            var refNodes = doc.DocumentNode.SelectNodes("//*[@id=\"tableSort\"]//tr//td[4]");

            if (dateNodes == null || typeNodes == null || contentNodes == null) {
                return -1;
            }

            var innerDate = dateNodes.Select(node => node.InnerText).ToList();
            var innerTypes = typeNodes.Select(node => node.InnerText).ToList();
            var innerContent = contentNodes.Select(node => node.InnerText).ToList();
            var innerRef = refNodes.Select(node => node.InnerText).ToList();

            int cnt = innerDate.Count();

            for (int i = 0; i < innerDate.Count(); ++i) {
                resume[i].Date = innerDate[i];
                resume[i].Type = innerTypes[i];
                resume[i].Content = innerContent[i];
                resume[i].Ref = innerRef[i];
            }

            return cnt;
        }
    }
    
}