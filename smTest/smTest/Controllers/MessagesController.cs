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

using System.Drawing;
using ZXing;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Bson.Serialization.Attributes;

using System.Diagnostics;
using MongoDB.Driver.Builders;

namespace MongoDBCreate
{
    public class ProductDB
    {
        [BsonId]
        public ObjectId ID { get; set;}
        public string Uriname { get; set; }
        public int op { get; set; }  
    }
}
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

            var connectionString = "mongodb://msp12:msp2017@ds123084.mlab.com:23084/msp";
            var client = new MongoClient(connectionString);
            var db = client.GetDatabase("msp");
            IMongoCollection<MongoDBCreate.ProductDB> collection = db.GetCollection<MongoDBCreate.ProductDB>("productResume");

            if (activity.Type == ActivityTypes.Message)
            {
                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                Activity reply = activity.CreateReply();

                //user傳一張照片
                if (activity.Attachments?.Count > 0 && activity.Attachments.First().ContentType.StartsWith("image"))//IF NULL 不會往下,有東西才繼續run
                {
                    // uriName 是decode qr code 完後的網址
                    var filter = Builders<MongoDBCreate.ProductDB>.Filter.Eq("op", 1);
                    var update = Builders<MongoDBCreate.ProductDB>.Update
                        .Set("Uriname", decodeQRCode(reply, activity.Attachments.First().ContentUrl));
                    var result = await collection.UpdateOneAsync(filter, update);
                    ////
                    Trace.TraceInformation("imgfor");
                    var user = collection.Find(new BsonDocument()).Project(x => new { x.Uriname }).ToList();
                    //var user = collection.Find(new BsonDocument()).ToListAsync();
                    Trace.TraceInformation("imgback");
                    ////

                    string uriName = decodeQRCode(reply, activity.Attachments.First().ContentUrl);
                    
                    foreach (var tmp2 in user)
                    {
                        Trace.TraceInformation(tmp2.Uriname);
                    }
                    reply.Text = uriName ;

                    Trace.TraceInformation("DB.");
                    GenericTemplate(reply);
                    Trace.TraceInformation("General done");

                }

                else if (activity.ChannelId == "facebook")
                {

                    //讀fb data
                    var fbData = JsonConvert.DeserializeObject<FBChannelModel>(activity.ChannelData.ToString());

                    if (activity.Text == "詳細生產履歷")
                    {
                        //var user = collection.Find(r => r.op == 1).Limit(1).ToList();
                        string url = " ";
                        Trace.TraceInformation("detail");

                        Trace.TraceInformation("imgfor");
                        var user = collection.Find(new BsonDocument()).Project(x => new { x.Uriname }).ToList();
                        //var user = collection.Find(new BsonDocument()).ToListAsync();
                        Trace.TraceInformation("imgback");
                        ////

                        foreach (var tmp2 in user)
                        {
                            url=tmp2.Uriname;
                        }
                       
                        reply.Attachments = await PassScrapTextAsync(reply, url);

                        Trace.TraceInformation("detail_done");
                    }

                    else if (activity.Text == "推薦食譜")
                    {
                        Trace.TraceInformation("recipe");
                        string url = " ";

                        Trace.TraceInformation("for");
                        var user = collection.Find(new BsonDocument()).Project(x => new { x.Uriname }).ToList();
                        Trace.TraceInformation("back");
                        ////

                        foreach (var tmp2 in user)
                        {
                            url = tmp2.Uriname;
                        }
                        var futherInfo = await getFurtherInfo(url, topRecipe);
                        //reply.Text = topRecipe[1].dishName;
                        MenuTemplate(reply, topRecipe);
                    }

                    else if (activity.Text == "履歷資訊")
                    {
                        //await Conversation.SendAsync(activity, () => new CarouselCardsDialog());
                        Trace.TraceInformation("reseme");
                        reply.SuggestedActions = new SuggestedActions()
                        {
                            Actions = new List<CardAction>()
                            {
                                new CardAction(){Title="生產者", Type=ActionTypes.ImBack, Value="生產者"},
                                new CardAction(){Title="產地", Type=ActionTypes.ImBack, Value="產地"},
                                new CardAction(){Title="產品名稱", Type=ActionTypes.ImBack, Value="產品名稱"},
                                new CardAction(){Title="生產日期", Type=ActionTypes.ImBack, Value="生產日期"},
                             
                            }
                        };
                    }


                    else if (fbData.message.quick_reply != null)
                    {
                        
                        string url=" ";

                        Trace.TraceInformation("for");
                        var user = collection.Find(new BsonDocument()).Project(x => new { x.Uriname }).ToList();
                        Trace.TraceInformation("back");
                        ////

                        foreach (var tmp2 in user)
                        {
                            url = tmp2.Uriname;
                        }

                        var farmresults = await FarmRecord(url, ProductInfo);
                        reply.Text = ProductInfo[0].Farmer;
                        switch (fbData.message.quick_reply.payload)
                        {
                            case "生產者":
                                reply.Text = ProductInfo[0].Farmer;

                                break;
                            case "產地":
                                reply.Text = ProductInfo[0].Origin;
                                break;
                            case "產品名稱":
                                reply.Text = ProductInfo[0].ProductName;
                                break;
                            case "生產日期":
                                reply.Text = ProductInfo[0].PackedDate;

                                break;
                            default:
                                reply.Text = "failed!!";
                                break;
                        }
                        //reply.Text = $"your choice is {fbData.message.quick_reply.payload}";
                    }

                    
                    else
                    {
                        reply.Text = "點選下列按鈕操作";
                        GenericTemplate(reply);
                        //用 luis 去偵測使用者的意思
                        // await ProcessLUIS(activity, activity.Text);
                        // reply.Text = "LUIS in FB";
                    }
                }
                else
                {
                    reply.Text = "點選下列按鈕操作";
                    GenericTemplate(reply);
                    // await ProcessLUIS(activity, activity.Text);
                    //reply.Text = "LUIS out FB";
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

        private void MenuTemplate(Activity reply, Recipe[] re)
        {

            reply.AttachmentLayout = AttachmentLayoutTypes.Carousel;
            reply.Attachments = GetCardsAttachments(re);
        }

      
        private static IList<Attachment> GetCardsAttachments(Recipe[] re)
        {
            string dish = "點選看食譜";
            Trace.TraceInformation("0903");
            return new List<Attachment>()
            {
                 GetHeroCard(
                    re[0].dishName,
                    new CardImage(url: re[0].dishPhoto ),
                    new CardAction(ActionTypes.OpenUrl,  dish, value: re[0].dishUrl )),
                 GetHeroCard(
                    re[1].dishName,
                    new CardImage(url: re[1].dishPhoto ),
                    new CardAction(ActionTypes.OpenUrl,  dish, value: re[1].dishUrl )),
                 GetHeroCard(
                    re[2].dishName,
                    new CardImage(url: re[2].dishPhoto ),
                    new CardAction(ActionTypes.OpenUrl,  dish, value: re[2].dishUrl )),
                 GetHeroCard(
                    re[3].dishName,
                    new CardImage(url: re[3].dishPhoto ),
                    new CardAction(ActionTypes.OpenUrl,  dish, value: re[3].dishUrl )),
            };
        }

        private static Attachment GetHeroCard(string title, CardImage cardImage, CardAction cardAction)
        {
            var heroCard = new HeroCard
            {
                Title = title,
                Images = new List<CardImage>() { cardImage },
                Buttons = new List<CardAction>() { cardAction },
            };

            return heroCard.ToAttachment();
        }

        private async Task<IList<Attachment>> PassScrapTextAsync(Activity context, string url)
        {
            Uri uriResult;
            bool result = Uri.TryCreate(url, UriKind.Absolute, out uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);


            List<Attachment> att = new List<Attachment>();
            if (result == true)
            {

                int cnt = await ProductionRecord(url, ProductResume);
                // string alltext = "作業日期\t\t作業種類\t\t作業內容\n\n============================\n\n";

                if (cnt != -1)
                {
                    for (int i = 0; i < cnt; ++i)
                    {
                        ThumbnailCard tc = new ThumbnailCard()
                        {
                            Title = ProductResume[i].Date,
                            Subtitle = ProductResume[i].Type + "\t" + ProductResume[i].Content,

                        };

                        att.Add(tc.ToAttachment());

                    }
                }
                else
                {
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
        private void GeneralTemplate(Activity reply)
        {
            List<Attachment> att = new List<Attachment>();
            att.Add(new HeroCard() //建立fb ui格式的api
            {
                Title = "歡迎來到我們的 ChatBot !",
                Subtitle = "請選擇你要使用的服務",
                Images = new List<CardImage>() { new CardImage("https://cdn.ready-market.com/1/9816a644//Templates/pic/vegetable.jpg?v=0d7a3372") },
                Buttons = new List<CardAction>()
                {
                    new CardAction(){ Title = "上傳 QR code", Type=ActionTypes.ImBack, Value= "上傳 QR code" },
                    new CardAction(){Title = "履歷資訊", Type= ActionTypes.ImBack, Value= "履歷資訊" },
                    new CardAction(){Title = "我要去看產銷履歷網站", Type= ActionTypes.ImBack, Value= "https://taft.coa.gov.tw/" },

                }
            }.ToAttachment());
            reply.Attachments = att;

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
                    new CardAction(){Title = "詳細生產履歷", Type=ActionTypes.ImBack, Value= "詳細生產履歷" },
                    new CardAction(){Title = "履歷資訊", Type= ActionTypes.ImBack, Value= "履歷資訊" },
                    new CardAction(){Title = "推薦食譜", Type= ActionTypes.ImBack, Value= "推薦食譜" },

                }
            }.ToAttachment());
            reply.Attachments = att;

        }

        // 擷取產品履歷資料
        private async Task<int> ProductionRecord(string url, Resume[] resume)
        {
            var doc = await Task.Factory.StartNew(() => client.Load(url));

            var dateNodes = doc.DocumentNode.SelectNodes("//*[@id=\"tableSort\"]//tr/td[1]");
            var typeNodes = doc.DocumentNode.SelectNodes("//*[@id=\"tableSort\"]//tr/td[2]");
            var contentNodes = doc.DocumentNode.SelectNodes("//*[@id=\"tableSort\"]//tr/td[3]");
            var refNodes = doc.DocumentNode.SelectNodes("//*[@id=\"tableSort\"]//tr//td[4]");

            if (dateNodes == null || typeNodes == null || contentNodes == null)
            {
                return -1;
            }

            var innerDate = dateNodes.Select(node => node.InnerText).ToList();
            var innerTypes = typeNodes.Select(node => node.InnerText).ToList();
            var innerContent = contentNodes.Select(node => node.InnerText).ToList();
            var innerRef = refNodes.Select(node => node.InnerText).ToList();

            int cnt = innerDate.Count();

            for (int i = 0; i < innerDate.Count(); ++i)
            {
                resume[i].Date = innerDate[i];
                resume[i].Type = innerTypes[i];
                resume[i].Content = innerContent[i];
                resume[i].Ref = innerRef[i];
            }

            return cnt;
        }

        private async Task<Boolean> FarmRecord(string url, Product[] pro)
        {
            var doc = await Task.Factory.StartNew(() => client.Load(url));

            var companyShort = doc.GetElementbyId("ctl00_ContentPlaceHolder1_Producer").InnerText;
            var Farmer = doc.GetElementbyId("ctl00_ContentPlaceHolder1_FarmerName").InnerText;
            var productName = doc.GetElementbyId("ctl00_ContentPlaceHolder1_ProductName").InnerText;
            var origin = doc.GetElementbyId("ctl00_ContentPlaceHolder1_Place").InnerText;
            var packedDate = doc.GetElementbyId("ctl00_ContentPlaceHolder1_PackDate").InnerText;
            var varifiedCompany = doc.GetElementbyId("ctl00_ContentPlaceHolder1_ao_name").InnerText;

            if (companyShort == null) 
            {
                return false;
            }

            pro[0].CompanyShort = companyShort;
            pro[0].Farmer = Farmer;
            pro[0].ProductName = productName;
            pro[0].Origin = origin;
            pro[0].PackedDate = packedDate;
            pro[0].VarifiedCompany = varifiedCompany;

            return true;
        }

        private async Task<Boolean> getFurtherInfo(string url, Recipe[] re)
        {
            var doc = await Task.Factory.StartNew(() => client.Load(url));

            var nulltest = doc.DocumentNode.SelectSingleNode("//*[@id=\"ctl00_ContentPlaceHolder1_RecommandDIV\"]/div/ul/li[1]/p/a");

            if (nulltest != null)
            {
                re[0].dishName = doc.DocumentNode.SelectSingleNode("//*[@id=\"ctl00_ContentPlaceHolder1_RecommandDIV\"]/div/ul/li[1]/p/a").InnerText.ToString();
                re[0].dishPhoto = doc.DocumentNode.SelectSingleNode("//*[@id=\"ctl00_ContentPlaceHolder1_RecommandDIV\"]/div/ul/li[1]/div/a/img").Attributes["src"].Value;
                re[0].dishUrl = "https://taft.coa.gov.tw" + doc.DocumentNode.SelectSingleNode("//*[@id=\"ctl00_ContentPlaceHolder1_RecommandDIV\"]/div/ul/li[1]/p/a").Attributes["href"].Value;

                re[1].dishName = doc.DocumentNode.SelectSingleNode("//*[@id=\"ctl00_ContentPlaceHolder1_RecommandDIV\"]/div/ul/li[2]/p/a").InnerText.ToString();
                re[1].dishPhoto = doc.DocumentNode.SelectSingleNode("//*[@id=\"ctl00_ContentPlaceHolder1_RecommandDIV\"]/div/ul/li[2]/div/a/img").Attributes["src"].Value;
                re[1].dishUrl = "https://taft.coa.gov.tw" + doc.DocumentNode.SelectSingleNode("//*[@id=\"ctl00_ContentPlaceHolder1_RecommandDIV\"]/div/ul/li[2]/p/a").Attributes["href"].Value;

                re[2].dishName = doc.DocumentNode.SelectSingleNode("//*[@id=\"ctl00_ContentPlaceHolder1_RecommandDIV\"]/div/ul/li[3]/p/a").InnerText.ToString();
                re[2].dishPhoto = doc.DocumentNode.SelectSingleNode("//*[@id=\"ctl00_ContentPlaceHolder1_RecommandDIV\"]/div/ul/li[3]/div/a/img").Attributes["src"].Value;
                re[2].dishUrl = "https://taft.coa.gov.tw" + doc.DocumentNode.SelectSingleNode("//*[@id=\"ctl00_ContentPlaceHolder1_RecommandDIV\"]/div/ul/li[3]/p/a").Attributes["href"].Value;

                re[3].dishName = doc.DocumentNode.SelectSingleNode("//*[@id=\"ctl00_ContentPlaceHolder1_RecommandDIV\"]/div/ul/li[4]/p/a").InnerText.ToString();
                re[3].dishPhoto = doc.DocumentNode.SelectSingleNode("//*[@id=\"ctl00_ContentPlaceHolder1_RecommandDIV\"]/div/ul/li[4]/div/a/img").Attributes["src"].Value;
                re[3].dishUrl = "https://taft.coa.gov.tw" + doc.DocumentNode.SelectSingleNode("//*[@id=\"ctl00_ContentPlaceHolder1_RecommandDIV\"]/div/ul/li[4]/p/a").Attributes["href"].Value;

                return true;
            }
            else
            {
                return false;
            }
        }

        private async Task<string> getPediaUrl(string url)
        {
            var doc = await Task.Factory.StartNew(() => client.Load(url));

            var nodetest = doc.DocumentNode.SelectSingleNode("//*[@id=\"ctl00_ContentPlaceHolder1_ProductName\"]/a");

            if (nodetest != null)
            {
                string toReturn = doc.DocumentNode.SelectSingleNode("//*[@id=\"ctl00_ContentPlaceHolder1_ProductName\"]/a").Attributes["href"].Value;
                return toReturn;
            }
            else
            {
                return "NULL";
            }
        }
    }
    
}