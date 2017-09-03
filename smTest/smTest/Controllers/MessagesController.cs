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
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Bson.Serialization.Attributes;

using Newtonsoft.Json.Linq;

using System.Net.Http.Headers;
using Autofac;
using MongoDB.Driver.Builders;
using System.Diagnostics;

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
                    /*var connectionString = "mongodb://msp12:msp2017@ds123084.mlab.com:23084/msp";
                    var client = new MongoClient(connectionString);
                    var db = client.GetDatabase("msp");
                    IMongoCollection<MongoDBCreate.ProductDB> collection = db.GetCollection<MongoDBCreate.ProductDB>("productResume");*/
                    //MongoDBCreate.ProductDB newItem = new MongoDBCreate.ProductDB { Uriname = decodeQRCode(reply, activity.Attachments.First().ContentUrl), op = 1 };


                    // uriName 是decode qr code 完後的網址
                    //var UriName = decodeQRCode(reply, activity.Attachments.First().ContentUrl);

                    //collection.InsertOne(newItem);
                    var filter = Builders<MongoDBCreate.ProductDB>.Filter.Eq("op", 1);
                    var update = Builders<MongoDBCreate.ProductDB>.Update
                        .Set("Uriname", decodeQRCode(reply, activity.Attachments.First().ContentUrl));
                    var result = await collection.UpdateOneAsync(filter, update);
                    var user = collection.Find(r => r.op == 1).Limit(1).ToList();

                    /*foreach (var tmp in user)
                    {
                        reply.Text = tmp.Uriname;
                        continue;
                    }*/
                    // receivedText 是爬蟲過後的訊息
                    //string receivedText = await PassScrapTextAsync(uriName);
                 
                    Trace.TraceInformation("DB.");
                    GenericTemplate(activity);
                    Trace.TraceInformation("GenericTemplate done");

                }

                else if (activity.ChannelId == "facebook")
                {

                    //讀fb data
                    var fbData = JsonConvert.DeserializeObject<FBChannelModel>(activity.ChannelData.ToString());
                    
                    
                    if (activity.Text == "try")
                    {
                        var user = collection.Find(r => r.op == 1).Limit(1).ToList();

                        foreach (var tmp in user)
                        {
                            reply.Text = tmp.Uriname;
                        }
                       // GenericTemplate(reply);
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
                        var user = collection.Find(r => r.op == 1).Limit(1).ToList();
                        string url="aaa";
                        foreach (var tmp in user)
                        {
                            url = tmp.Uriname;
                        }
                        var farmresults = await FarmRecord(url, ProductInfo);
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
                        reply.Text = $"your choice is {fbData.message.quick_reply.payload}";
                    }


                    else
                    { 
                        //用 luis 去偵測使用者的意思
                        await ProcessLUIS(activity, activity.Text);

                    }
                }


                else
                {
                    GenericTemplate(reply);
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


        private async Task<String> PassScrapTextAsync(string uriName)
        {
            Uri uriResult;
            bool result = Uri.TryCreate(uriName, UriKind.Absolute, out uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
            if (result == true)
            {
                int cnt = await ProductionRecord (uriName, ProductResume);
                string alltext = "作業日期\t\t作業種類\t\t作業內容\n\n============================\n\n";

                 if (cnt != -1)
                 {
                    for (int i = 0; i < cnt; ++i)
                    {
                        alltext += String.Join(", ", ProductResume[i].Date);
                        alltext += "\t\t";
                        alltext += String.Join(", ", ProductResume[i].Type);
                        alltext += "\t\t";
                        alltext += String.Join(", ", ProductResume[i].Content);
                        alltext += "\n\n";
                    }
                } else
                {
                    alltext = "ERROR in parsing resume\n";
                }

                var farmresults = await FarmRecord(uriName, ProductInfo);

                if (farmresults) {
                    alltext += "\n**************\n";

                    alltext += String.Join("\n", ProductInfo[0].CompanyShort);
                    alltext += String.Join("\n", ProductInfo[0].Farmer);
                    alltext += String.Join("\n", ProductInfo[0].Origin);
                    alltext += String.Join("\n", ProductInfo[0].PackedDate);
                    alltext += String.Join("\n", ProductInfo[0].ProductName);
                    alltext += String.Join("\n", ProductInfo[0].VarifiedCompany);
                }
                else {
                    alltext += String.Join("\n", "Errors in parsing farmresults\n");
                }

                bool hasRecipe = await getFurtherInfo(uriName, topRecipe);

                if (hasRecipe) {
                    alltext += "\n**************\n";

                    for (int i = 0; i < 4; ++i) {
                        alltext += String.Join("\n", topRecipe[i].dishName);
                        alltext += String.Join("\n", topRecipe[i].dishPhoto);
                        alltext += String.Join("\n", topRecipe[i].dishUrl);
                        alltext += String.Join("\n", "------------------\n");
                    }
                }
                else {
                    alltext += String.Join("\n", "No recipe found\n");
                }

                var pediaUrl = await getPediaUrl(uriName);

                if (pediaUrl != "NULL") {
                    alltext += String.Join("\n", pediaUrl);
                }
                else {
                    alltext += String.Join("\n", "No URL found\n");
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