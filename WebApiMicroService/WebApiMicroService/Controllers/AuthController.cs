using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;
using System.Text.Json;
using System.Text;
using WebApiMicroService.Models;
using System.Drawing;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using HtmlAgilityPack;
using System.Xml.Linq;
using System.Web;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace WebApiMicroService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ILogger<AuthController> _logger;
        string url = "https://eos2.vstu.ru/";

        public AuthController(ILogger<AuthController> logger)
        {
            _logger = logger;   
        }

        [HttpPost("auth")]
        public async Task<IActionResult> AddPerson([FromBody]Auth auth)
        {
            AuthEos authorization = new AuthEos { UserName = auth.username, Password = auth.password};
            var cookieContainer = new CookieContainer();
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = false,
                CookieContainer = cookieContainer
            };
            using (HttpClient client = new HttpClient(handler))
            {
                HttpResponseMessage response = await client.GetAsync(url+"login/index.php");
                if (response.IsSuccessStatusCode)
                {
                    IEnumerable<string> cookies = response.Headers.GetValues("Set-Cookie");

                    // Теперь у вас есть коллекция куков, которые вы можете использовать в последующих запросах
                    foreach (var cookie1 in cookies)
                    {
                        cookieContainer.SetCookies(new Uri("https://eos2.vstu.ru"), cookie1.Split(';')[0]);
                        Console.WriteLine($"Cookie: {cookie1}");
                        // Здесь вы можете обработать или сохранить куки по своему усмотрению
                    }

                    var page = await client.GetStringAsync(url+$"login/index.php");                    
                    HtmlDocument tokenHtml = new HtmlDocument();
                    tokenHtml.LoadHtml(page);
                    var token = tokenHtml.DocumentNode.SelectSingleNode("//input[@name='logintoken']").GetAttributeValue("value", "");

                    /// Auth loginData = new Auth
                    //{
                    //anchor = "",
                    // logintoken = sss,
                    //  username = "19106266",
                    //   password = "St089618%"
                    //};

                    // Установите заголовки запроса.
                    AddHeaders(client);

                    var formData = new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("anchor", $"\"\""),
                        new KeyValuePair<string, string>("logintoken", $"{token}"),
                        new KeyValuePair<string, string>("username", $"{auth.username}"),
                        new KeyValuePair<string, string>("password", $"{auth.password}"),
                        // Другие параметры, если они есть
                    };

                    var content = new FormUrlEncodedContent(formData);

                    HttpResponseMessage postResponse = await client.PostAsync(url+"login/index.php", content);

                    // Обработайте ответ от сервера.
                    if (postResponse.StatusCode == HttpStatusCode.SeeOther)
                    {

                        HttpResponseMessage reconnectResponse = await client.GetAsync(postResponse.Headers.Location);
                        string id = postResponse.Headers.Location.ToString().Split('=')[1];
                        if (reconnectResponse.StatusCode == HttpStatusCode.SeeOther)
                        {
                            HttpResponseMessage rereconnectResponse = await client.GetAsync(reconnectResponse.Headers.Location);
                            if (rereconnectResponse.StatusCode == HttpStatusCode.OK)
                            {
                                var mainPage = await client.GetStringAsync(url+$"user/profile.php?id={id}");
                                HtmlDocument htmlDocument = new HtmlDocument();
                                htmlDocument.LoadHtml(mainPage);
                                // Поиск всех ссылок в HTML
                                var sectionNode = htmlDocument.DocumentNode.SelectSingleNode("//section[@id='region-main']");

                                if (sectionNode != null)
                                {
                                    // Находим все элементы <div class="card-body"> внутри секции
                                    var cardNodes = sectionNode.SelectNodes(".//div[@class='card-body']");
                                    if (cardNodes != null)
                                    {
                                        var _li = cardNodes[2].SelectSingleNode(".//li[@class='contentnode']");
                                        var dd = _li.SelectSingleNode(".//dd");
                                        var ddLinks = dd.SelectNodes(".//a");

                                        var l = ddLinks[ddLinks.Count - 1].GetAttributeValue("href", "").Trim();
                                        HttpResponseMessage fullInformation = await client.GetAsync(l.Replace("amp;", ""));
                                        if (fullInformation.IsSuccessStatusCode)
                                        {
                                            htmlDocument = new HtmlDocument();
                                            htmlDocument.LoadHtml(await fullInformation.Content.ReadAsStringAsync());
                                            var FullNameNode = htmlDocument.DocumentNode.SelectSingleNode("//div[@class='page-header-headings']");
                                            var FullName = FullNameNode.SelectSingleNode("//h2").InnerText.Split(' ');

                                            Person person = new Person()
                                            {
                                                FirstName = FullName[0],
                                                LastName = FullName[1],
                                                MiddleName = FullName[2],
                                            };
                                            Console.WriteLine($"FIO: {string.Concat(FullName)}");
                                            sectionNode = htmlDocument.DocumentNode.SelectSingleNode("//section[@id='region-main']");

                                            if (sectionNode != null)
                                            {
                                                cardNodes = sectionNode.SelectNodes(".//div[@class='card-body']");
                                                if (cardNodes != null)
                                                {
                                                    foreach (var cardNode in cardNodes)
                                                    {
                                                        Card card = new Card();
                                                        // Извлекаем название карточки
                                                        var cardTitle = cardNode.SelectSingleNode(".//h3");

                                                        if (cardTitle != null)
                                                        {
                                                            var li = cardNode.SelectNodes(".//li[@class='contentnode']");
                                                            var li2 = cardNode.SelectNodes(".//li");

                                                            card.Title = cardTitle.InnerText;
                                                            Console.WriteLine($"Название карточки: {cardTitle.InnerText}");

                                                            if (li != null)
                                                            {
                                                                foreach (var liNode in li)
                                                                {
                                                                    Point point = new Point();
                                                                    var Name = liNode.SelectSingleNode(".//dt");
                                                                    var LinkBody = liNode.SelectSingleNode(".//dd");
                                                                    point.Title = Name.InnerText;

                                                                    Console.WriteLine($"ПодИмя: {Name.InnerText}");
                                                                    var links = LinkBody.SelectNodes(".//a");
                                                                    if (links != null)
                                                                    {
                                                                        foreach (var link in links)
                                                                        {
                                                                            string linkText = link.InnerText.Trim();
                                                                            string linkHref = link.GetAttributeValue("href", "").Trim();
                                                                            linkText = WebUtility.HtmlDecode(linkText);
                                                                            point.Add(linkText);
                                                                            Console.WriteLine($"Текст: {linkText}");
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        point.Add(LinkBody.InnerText);
                                                                        Console.WriteLine($"Текст: {LinkBody.InnerText}");
                                                                    }
                                                                    card.Add(point);
                                                                }

                                                            }
                                                            else if (li2 != null)
                                                            {
                                                                foreach (var liNode in li2)
                                                                {
                                                                    Point point = new Point();
                                                                    point.Title = "";
                                                                    var links = liNode.SelectSingleNode(".//a");
                                                                    string linkText = links.InnerText.Trim();
                                                                    string linkHref = links.GetAttributeValue("href", "").Trim();
                                                                    linkText = WebUtility.HtmlDecode(linkText);
                                                                    point.Add(linkText);
                                                                    Console.WriteLine($"Текст: {linkText}");
                                                                    card.Add(point);
                                                                }
                                                            }
                                                        }
                                                        person.AddCard(card);
                                                        // Ищем и выводим все ссылки внутри карточки
                                                    }
                                                }
                                            }
                                            using (var dbContext = new AppDbContext())
                                            {
                                                var users = dbContext.Users.ToList();
                                                var log = GetHash(auth.username);
                                                var pass = GetHash(auth.password);
                                                var user = users.FirstOrDefault(x=> x.login == log && x.password == pass);
                                                if (user == null)
                                                {
                                                    dbContext.Add(new User()
                                                    {
                                                        login = log,
                                                        password = pass,
                                                        firstname = person.FirstName,
                                                        lastname = person.LastName,
                                                        middlename = person.MiddleName,
                                                        email = person.listCard[0].listPoint[0].listText[0],
                                                        hide_contacts = true  
                                                    });
                                                    await dbContext.SaveChangesAsync();
                                                }
                                                var academics = dbContext.AcademicSubjects.ToList();
                                                var listAcademic = person.listCard[2].listPoint[0].listText;
                                                for (int i = 0; i < listAcademic.Count; i++)
                                                {
                                                    if (academics.FirstOrDefault(x => x.name == listAcademic[i]) == null)
                                                    {
                                                        dbContext.Add(new AcademicSubject()
                                                        {
                                                            name = listAcademic[i]
                                                        });
                                                    }
                                                }
                                                await dbContext.SaveChangesAsync();
                                            }
                                            return Ok();
                                        }
                                    }
                                    else
                                    {                                        
                                        Console.WriteLine("Карточки не найдены в секции.");
                                    }
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("Gg");
                        }
                        // Сервер вернул статус 303 See Other, что означает успешное перенаправление.
                        // Вы можете выполнить дополнительные действия, если это необходимо.

                    }
                    else if (postResponse.IsSuccessStatusCode)
                    {
                        // Обработайте успешный ответ (статус 200 OK).
                        // Вы можете проверить содержимое, если это необходимо.
                        Console.WriteLine("Успешный ответ (статус 200 OK).");
                    }
                    else
                    {
                        // Обработайте другие статусы, если это необходимо.
                        Console.WriteLine($"Статус: {postResponse.StatusCode}");
                    }
                }
            }
            return BadRequest();                
        }
        public static void AddHeaders(HttpClient client)
        {
            client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
            client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
            client.DefaultRequestHeaders.Add("Accept-Language", "ru,en;q=0.9");
            client.DefaultRequestHeaders.Add("Cache-Control", "max-age=0");
            client.DefaultRequestHeaders.Add("Connection", "keep-alive");

            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/116.0.5845.2272 YaBrowser/23.9.0.2272 Yowser/2.5 Safari/537.36");
            client.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "document");
            client.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "navigate");
            client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-origin");
            client.DefaultRequestHeaders.Add("Sec-Fetch-User", "?1");
            client.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
            // Создайте данные формы для отправки.
            client.DefaultRequestHeaders.Add("Host", "eos2.vstu.ru");
            client.DefaultRequestHeaders.Add("Origin", "https://eos2.vstu.ru");
            client.DefaultRequestHeaders.Add("Referer", "https://eos2.vstu.ru/login/index.php");
            client.DefaultRequestHeaders.Add("Sec-Ch-Ua", "\"Chromium\";v=\"116\", \"Not)A;Brand\";v=\"24\", \"YaBrowser\";v=\"23\"");
            client.DefaultRequestHeaders.Add("Sec-Ch-Ua-Mobile", "?0");
            client.DefaultRequestHeaders.Add("Sec-Ch-Ua-Platform", "\"Windows\"");            
        }
        private string GetHash(string input)
        {
            var md5 = MD5.Create();
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(hash);
        }
    }
    public class Auth
    {
        public string username { get; set; }
        public string password { get; set; }
    }

    class Person
    {
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public List<Card> listCard { get; set; }
        public Person()
        {
            listCard = new List<Card>();
        }
        public void AddCard(Card card)
        {
            listCard.Add(card);
        }
    }

    class Card
    {
        public string Title { get; set; }
        public List<Point> listPoint { get; }
        public Card()
        {
            listPoint = new List<Point>();
        }

        public void Add(Point point)
        {
            listPoint.Add(point);
        }
    }

    class Point
    {
        public string Title { get; set; }
        public List<string> listText { get; set; }

        public Point()
        {
            listText = new List<string>();
        }
        public void Add(string text)
        {
            listText.Add(text);
        }
    }    
}