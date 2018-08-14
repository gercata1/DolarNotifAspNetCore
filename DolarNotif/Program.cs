using DolarNotif.Model;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using MimeKit;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;


namespace DolarNotif
{
    class Program
    {
        public static IConfigurationRoot Configuration { get; set; }
        static async Task MainAsync(string[] args)
        {
            #region SettingUpConfig

            var builder = new ConfigurationBuilder()
                 .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

            Configuration = builder.Build();


            #endregion

            var currentValues = new BrouModel();
            BrouModel oldValues = null;

            if (File.Exists("output.json"))
            {
                File.Copy("output.json", "old.output.json", true);

                var values = File.ReadAllText("output.json");

                oldValues = JsonConvert.DeserializeObject<BrouModel>(values);

            }

            var url = Configuration["url"];

            using (var client = new HttpClient())
            {
                var response = await client.GetStringAsync(url);

                HtmlDocument document = new HtmlDocument();
                document.LoadHtml(response);

                var trs = document.QuerySelectorAll(".portlet-boundary_cotizacionfull_WAR_broutmfportlet_ table tbody tr");
                foreach (var tr in trs)
                {
                    var tds = tr.QuerySelectorAll("td");
                    if (tds.Count > 0)
                    {
                        var currencyText = tds[0].InnerText.Trim();
                        var compra = tds[2].InnerText.Trim().Replace(",", ".");
                        var venta = tds[4].InnerText.Trim().Replace(",", ".");

                        if (currencyText == "Dólar")
                        {
                            currentValues.Dolar.Compra = double.Parse(compra);
                            currentValues.Dolar.Venta = double.Parse(venta);
                        }
                        else if (currencyText == "Dólar eBROU")
                        {
                            currentValues.DolarEBrou.Compra = double.Parse(compra);
                            currentValues.DolarEBrou.Venta = double.Parse(venta);
                        }
                    }
                }


                var valuesString = JsonConvert.SerializeObject(currentValues);
                File.WriteAllText("output.json", valuesString);

            }

            if (oldValues != null && (oldValues.Dolar.Compra != currentValues.Dolar.Compra || oldValues.DolarEBrou.Compra != currentValues.DolarEBrou.Compra))
            {
                var cambio = "Bajo";
                if (currentValues.Dolar.Compra > oldValues.Dolar.Compra || currentValues.DolarEBrou.Compra > oldValues.DolarEBrou.Compra)
                    cambio = "Subio";

                try
                {
                    //From Address 
                    string FromAddress = Configuration["EmailFrom"];
                    string FromAdressTitle = "Dolar Notif";
                    //To Address 
                    string ToAddress = Configuration["EmailTo"];
                    string Subject = cambio + " - Cotizacion Dolar";
                    string BodyContent = "<h2> Cambio de cotizacion del dolar </h2> <br><br><table><tr><th></th><th>Viejo valor</th><th>Nuevo valor</th></tr><tr><td>Dolar</td><td>" + oldValues.Dolar.Compra.ToString("C") + "</td><td>" + currentValues.Dolar.Compra.ToString("C") + "</td></tr><tr><td>Ebrou</td><td>" + oldValues.DolarEBrou.Compra.ToString("C") + "</td><td>" + currentValues.DolarEBrou.Compra.ToString("C") + "</td></tr></table>";

                    //Smtp Server 
                    string SmtpServer = "smtp.gmail.com";
                    //Smtp Port Number 
                    int SmtpPortNumber = 587;


                    var mimeMessage = new MimeMessage();
                    mimeMessage.From.Add(new MailboxAddress(FromAdressTitle, FromAddress));
                    mimeMessage.To.Add(new MailboxAddress(ToAddress, ToAddress));
                    mimeMessage.Subject = Subject;
                    mimeMessage.Body = new TextPart("html")
                    {
                        Text = BodyContent

                    };

                    using (var client = new SmtpClient())
                    {

                        client.Connect(SmtpServer, SmtpPortNumber, false);
                        // Note: only needed if the SMTP server requires authentication 
                        // Error 5.5.1 Authentication  
                        client.Authenticate(FromAddress, Configuration["EmailPass"]);
                        client.Send(mimeMessage);
                        Console.WriteLine("The mail has been sent successfully !!");
                        client.Disconnect(true);

                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }


            Console.WriteLine("Finish.");
        }



        public static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
        }
    }

}