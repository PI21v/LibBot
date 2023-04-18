using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Requests;
using System.Runtime.CompilerServices;
using Telegram.Bot.Framework.Abstractions;
using MailKit;
using Org.BouncyCastle.Crypto.Tls;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bots.Types;
using Polly;
using Org.BouncyCastle.Asn1.X509.Qualified;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Types.InputFiles;
using Telegraph;
using Telegraph.Net.Models;
using Telegraph.Net;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bots.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Telegram.Bots;
using Newtonsoft.Json;
using System.Xml.Linq;
using MimeKit.Cryptography;
using System.Net.Http.Headers;

namespace TelegramLibBot
{
    public class LibraryBot
    {
        enum States
        {
            WaitTitle = 1,
            TitleRequest = 2,
            WaitGenre = 3,
            GenreRequest =4,
            WaitDescription = 5,
            DescriptionRequest = 6,
            WaitAuthor = 7,
            AuthorRequest = 8,
            WaitImage = 9,
            ImageRequest = 10,
            IsWantToSendImage = 12,
            Auto = 11
        }


        private static string token { get; set; } = "6058875061:AAGE5X5gw2DSOJE2ru58NFtZXXJyDNB9Im4";
        private static long chatId { get; set; } = -1001898169442;
        private static ITelegramBotClient client;

        #region данные_на_ввод
        public static string title = "";
        public static string genre = "";
        public static string description = "";
        public static string author = "";

        public static string fileID;
        public static string fileURL;
        public static Telegram.Bot.Types.File file;
        public static Telegram.Bot.Types.Message temp;
        #endregion

        public ITelegramBotClient Client { get => client; }

        public LibraryBot()
        {
            client = new TelegramBotClient(token);            
            
            Console.WriteLine($"[{DateTime.Now}] Телеграмм бот запущен.");
            client.StartReceiving(Update, Error);           
            Thread.Sleep(int.MaxValue);
            
        }


        private async static Task Error(ITelegramBotClient arg1, Exception arg2, CancellationToken arg3)
        {
            throw new NotImplementedException();
        }



        private async static Task Update(ITelegramBotClient client, Telegram.Bot.Types.Update update, CancellationToken token)
        {
            var message = update.Message;
            var updates = client.GetUpdatesAsync();
            if (message?.Text != null && message.Chat.Id != chatId)
            {
                await Console.Out.WriteLineAsync($"[{DateTime.Now}]({message.From.Id})" +
            $"{message.From.Username}:{message.Text}");

                switch (message.Text.ToLower())
                {
                    case "/start":
                        await client.SendTextMessageAsync(message.Chat.Id, "Приветствую!Чем могу помочь?\nДля дополнительной информации " +
                            "введите /commandslist", replyMarkup: GetButtons());
                        break;
                    case "/createpost":
                    case "создать пост":
                        await client.SendTextMessageAsync(message.Chat.Id, "Выберите тип поста: ", replyMarkup: GetCreatePostButtons());
                        States states = States.TitleRequest;
                        long id = message.Chat.Id;
                        string? previous = update?.Message?.Text;
                        await CreatePost(states,client,update,token,id,previous);
                        await client.SendTextMessageAsync(message.Chat.Id, "Отправляю пост на модерацию");
                        Article article = new Article(title, genre, author, description);


                        
                        //var pagePath = await CreateTelegraphPost(article);                       
                        //await client.SendTextMessageAsync(chatId, pagePath);

                        if (article.Author.Length + article.Genre.Length + article.Description.Length + article.Author.Length > 1000)
                        {

                            var pagePath = await CreateTelegraphPost(article);
                            await client.SendTextMessageAsync(chatId, pagePath);
                        }
                        else
                        {
                            var photo = new InputOnlineFile(fileID);
                            await client.SendPhotoAsync(chatId, photo, caption: article.ToString());

                            await client.SendTextMessageAsync(chatId, article.ToString());

                            Telegram.Bot.Types.Message pollMessage = await client.SendPollAsync(
                                chatId: chatId,
                                question: $"Запрос на публикацию поста от ({message.From.Id}){message.From.Username}",
                                options: new[]
                                {
                                "Публиковать",
                                "Не публиковать"
                                }, cancellationToken: token);
                        }
                        break;
                    case "/commandslist":
                    case "список команд":
                        await client.SendTextMessageAsync(message.Chat.Id, "Доступный список комманд:");
                        using (var sr = new StreamReader("C:\\Users\\fikra\\source\\repos\\TelegramLibBot\\TelegramLibBot\\commandslist.txt"))
                        {
                            while (!sr.EndOfStream)
                            {
                                await client.SendTextMessageAsync(message.Chat.Id, sr.ReadLine().ToString());
                            }
                        }
                        break;
                    case "/template":
                        await client.SendTextMessageAsync(message.Chat.Id, "Создайте пост по шаблону, чтобы я мог его спарсить:<Название поста>^" +
                            "<#Жанр поста>(может быть несколько через запятую)^*Описание поста*(основная часть)^ <Имя автора>");
                        break;
                }
            }
        }

        public async static Task<string> UploadImageToTelegraph()
        {
            var imageUrl = $"https://api.telegram.org/file/bot{token}/{file.FilePath}";
            using (var httpClient = new HttpClient())
            {
                using (var content = new MultipartFormDataContent())
                {
                    using (var imageStream = new FileStream(imageUrl, FileMode.OpenOrCreate))
                    {
                        var imageContent = new StreamContent(imageStream);
                        imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpg");
                        content.Add(imageContent, "file", Path.GetFileName(imageUrl));
                        using (var response = await httpClient.PostAsync("https://telegra.ph/upload", content))
                        {
                            var responseJson = await response.Content.ReadAsStringAsync();
                            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<TelegraphUploadResult>(responseJson);
                            if (result != null && result.error == 0 && result.result.Count > 0)
                            {
                                return result.result[0].src;
                            }
                        }
                    }
                }
            }
            return null;
        }

        private async static Task<string> CreateTelegraphPost(Article article)
        {
            var client = new TelegraphClient();
            Account account = await client.CreateAccountAsync(
                "Minoddein"     
                     
            );
            //var imageUrl = await UploadImageToTelegraph();

            var imageUrl = $"https://api.telegram.org/file/bot{token}/{file.FilePath}";

            var nodeElementImage =  new NodeElement 
            { 
                Tag = "img",
                Attributes = new Dictionary<string, string>
                {
                    { "src", imageUrl },                    
                }
            };
            
            NodeElement nodeElementGenres = new NodeElement
            {               
                Tag = "_text",
                Attributes = new Dictionary<string, string> { { "value", "Жанры:"+article.Genre+"\n\n" } }              
            };

            NodeElement nodeElementDescription = new NodeElement
            {
                Tag = "_text",                      
                Attributes = new Dictionary<string, string> { { "value", article.Description } }                
            };           
            var nodes = new List<NodeElement>();
            nodes.Add(nodeElementImage);
            nodes.Add(nodeElementGenres);
            nodes.Add(nodeElementDescription);
           
            ITokenClient tokenClient = client.GetTokenClient(account.AccessToken);
            
            Telegraph.Net.Models.Page page = await tokenClient.CreatePageAsync(article.Title, nodes.ToArray(),article.Author);

            
            return page.Url;
        }

        private async static Task CreatePost(States states, ITelegramBotClient client, Telegram.Bot.Types.Update update, CancellationToken token,long id,string previous)
        {
            switch (states)
            {
                case States.TitleRequest:
                    await client.SendTextMessageAsync(id, "Введите название поста: ");
                    states = States.WaitTitle;
                    await CreatePost(states, client, update, token, id, previous);
                    await Console.Out.WriteLineAsync($"[{DateTime.Now}] Состояние запроса.");
                    break;
                case States.WaitTitle:
                    
                    var updates = client.GetUpdatesAsync(offset: 0, limit: 100, timeout: 0).Result;
                    
                    var message = updates.LastOrDefault(u => u.Message != null && u.Message.Chat.Id == id)?.Message?.Text;

                    if (message != null && message != "" && message != previous)
                    {
                         await client.SendTextMessageAsync(id, "Вы сказали: " + message);
                         title = message;
                         states = States.GenreRequest;
                         previous = message;
                    }
                    await Console.Out.WriteLineAsync($"[{DateTime.Now}] Состояние Ожидания.");
                    await Task.Delay(1000);
                    await CreatePost(states, client, update, token, id, previous);
                    
                    break;
                case States.GenreRequest:
                    await client.SendTextMessageAsync(id, "Введите жанры поста: ");
                    states = States.WaitGenre;
                    await CreatePost(states, client, update, token, id, previous);
                    await Console.Out.WriteLineAsync($"[{DateTime.Now}] Состояние запроса.");
                    break;
                case States.WaitGenre:
                    updates = client.GetUpdatesAsync(offset: 0, limit: 100, timeout: 0).Result;

                    message = updates.LastOrDefault(u => u.Message != null && u.Message.Chat.Id == id)?.Message?.Text;

                    if (message != null && message != "" && message != previous)
                    {
                        await client.SendTextMessageAsync(id, "Вы сказали: " + message);
                        genre = message;
                        states = States.DescriptionRequest;
                        previous = message;
                    }
                    await Console.Out.WriteLineAsync($"[{DateTime.Now}] Состояние Ожидания.");
                    await Task.Delay(1000);
                    await CreatePost(states, client, update, token, id, previous);
                    break;
                case States.DescriptionRequest:
                    await client.SendTextMessageAsync(id, "Введите описание поста: ");
                    states = States.WaitDescription;
                    await CreatePost(states, client, update, token, id, previous);
                    await Console.Out.WriteLineAsync($"[{DateTime.Now}] Состояние запроса.");
                    break;
                case States.WaitDescription:
                    updates = client.GetUpdatesAsync(offset: 0, limit: 100, timeout: 0).Result;

                    message = updates.LastOrDefault(u => u.Message != null && u.Message.Chat.Id == id)?.Message?.Text;

                    if (message != null && message != "" && message != previous)
                    {
                        await client.SendTextMessageAsync(id, "Вы сказали: " + message);
                        description = message;
                        states = States.AuthorRequest;
                        previous = message;
                    }
                    await Console.Out.WriteLineAsync($"[{DateTime.Now}] Состояние Ожидания.");
                    await Task.Delay(1000);
                    await CreatePost(states, client, update, token, id, previous);
                    break;
                case States.AuthorRequest:
                    await client.SendTextMessageAsync(id, "Укажите автора поста: ");
                    states = States.WaitAuthor;
                    await CreatePost(states, client, update, token, id, previous);
                    await Console.Out.WriteLineAsync($"[{DateTime.Now}] Состояние запроса.");
                    break;
                case States.WaitAuthor:
                    updates = client.GetUpdatesAsync(offset: 0, limit: 100, timeout: 0).Result;

                    message = updates.LastOrDefault(u => u.Message != null && u.Message.Chat.Id == id)?.Message?.Text;

                    if (message != null && message != "" && message != previous)
                    {
                        await client.SendTextMessageAsync(id, "Вы сказали: " + message);
                        author = message;                       
                        states = States.IsWantToSendImage;
                        previous = message;
                    }
                    await Console.Out.WriteLineAsync($"[{DateTime.Now}] Состояние Ожидания.");
                    await Task.Delay(1000);
                    await CreatePost(states, client, update, token, id, previous);
                    break;
                case States.IsWantToSendImage:
                    await client.SendTextMessageAsync(id, "Хотите добавить картинку?(После ответа 'Да', загрузите изображение)", replyMarkup: GetAddImageToPostButton());
                    states = States.ImageRequest;
                    await CreatePost(states, client, update, token, id, previous);
                    await Console.Out.WriteLineAsync($"[{DateTime.Now}] Состояние запроса.");
                    break;
                case States.ImageRequest:
                    updates = client.GetUpdatesAsync(offset: 0, limit: 100, timeout: 0).Result;

                    message = updates.LastOrDefault(u => u.Message != null && u.Message.Chat.Id == id)?.Message?.Text;

                    if (message != null && (message == "Да" || message == "Нет"))
                    {
                        if(message == "Да")
                        {
                            states = States.WaitImage;
                            previous = message;
                        }
                        else
                        {
                            states = States.Auto;
                        }
                    }
                    await Console.Out.WriteLineAsync($"[{DateTime.Now}] Состояние Ожидания.");
                    await Task.Delay(1000);
                    await CreatePost(states, client, update, token, id, previous);
                    break;
                case States.WaitImage:
                    updates = client.GetUpdatesAsync(offset: 0, limit: 100, timeout: 0).Result;

                    temp = updates.LastOrDefault(u => u.Message != null && u.Message.Chat.Id == id)?.Message;

                    if (temp?.Photo != null)
                    {
                        fileID = temp.Photo[0].FileId;
                        fileURL = temp.Photo[0].FileUniqueId;
                        file = await client.GetFileAsync(fileID);
                        states = States.Auto;
                    }
                    await Console.Out.WriteLineAsync($"[{DateTime.Now}] Состояние Ожидания.");
                    await Task.Delay(1000);
                    await CreatePost(states, client, update, token, id, previous);
                    break;
                case States.Auto:
                    return;
                
                    
            }

        }

     
        

        #region Вариант_с_Неработающим_поочерёдным_вводом
        //private async static Task<Task> CreatePost(ITelegramBotClient client, Telegram.Bot.Types.Update update, CancellationToken token)
        //{
        //    var message = update.Message;

        //    if ((message.Text == "Создать пост" || message.Text == "/createpost") && message != null && message.Text != "")
        //    {
        //        var tcs = new TaskCompletionSource<bool>();

        //        //await client.SendTextMessageAsync(message.Chat.Id, "Создайте пост(шаблон создания можно узнать по команде /template): ");
        //        await client.SendTextMessageAsync(message.Chat.Id, "Введите название: ");
        //        var article = await WaitForMessageAsync(client, message.Chat.Id, token);
        //        string article1 = article;

        //        await client.SendTextMessageAsync(message.Chat.Id, "Введите название 2: ");
        //        var article2 = await WaitForMessageAsync(client, message.Chat.Id, token);
        //        string article3 = article;

        //        #region test
        //        //while (message.Text == "")
        //        //{
        //        //    var updates = client.GetUpdatesAsync();
        //        //    if (updates.Result.Length > 0)
        //        //        message.Text = updates.Result.Last().Message.Text;
        //        //}
        //        //string temp = message.Text;
        //        #endregion
        //        await client.SendTextMessageAsync(message.Chat.Id, "Вы ввели название:\n " + article1.ToString() + " " + article3.ToString());
        //        tcs.SetResult(true);

        //        return tcs.Task;
        //    }
        //    return Task.CompletedTask;
        //}

        //private static async Task<string> WaitForMessageAsync(ITelegramBotClient client, long chatId, CancellationToken token)
        //{

        //    while (!token.IsCancellationRequested)
        //    {

        //        var updates = client.GetUpdatesAsync(offset: 0, limit: 100, timeout: 0).Result;
        //        var message = updates.LastOrDefault(u => u.Message != null && u.Message.Chat.Id == chatId)?.Message.Text;

        //        if (message != null && message != "" && message != "Создать пост" && message != "/createpost")
        //        {
        //            return message;
        //        }
        //        await Task.Delay(1000);
        //    }

        //    throw new OperationCanceledException();
        //}
        #endregion

        #region Вариант с парсингом
        //private async static Task<Task> CreatePost(ITelegramBotClient client, Telegram.Bot.Types.Update update, CancellationToken token)
        //{
        //    var message = update.Message;

        //    if ((message.Text == "Создать пост" || message.Text == "/createpost") && message != null && message.Text != "")
        //    {
        //        var tcs = new TaskCompletionSource<bool>();

        //        await client.SendTextMessageAsync(message.Chat.Id, "Создайте пост(шаблон создания можно узнать по команде /template): ");
        //        var article = await WaitForMessageAsync(client, message.Chat.Id, token);

        //        Article article1 = article;
        //        #region test
        //        //while (message.Text == "")
        //        //{
        //        //    var updates = client.GetUpdatesAsync();
        //        //    if (updates.Result.Length > 0)
        //        //        message.Text = updates.Result.Last().Message.Text;
        //        //}
        //        //string temp = message.Text;
        //        #endregion
        //        await client.SendTextMessageAsync(message.Chat.Id, "Вы ввели название:\n " + article1.ToString() + " ");
        //        tcs.SetResult(true);

        //        return tcs.Task;
        //    }
        //    return Task.CompletedTask;
        //}

        //private static async Task<Article> WaitForMessageAsync(ITelegramBotClient client, long chatId, CancellationToken token)
        //{

        //    while (!token.IsCancellationRequested)
        //    {

        //        var updates = client.GetUpdatesAsync(offset: 0, limit: 100, timeout: 0).Result;
        //        var message = updates.LastOrDefault(u => u.Message != null && u.Message.Chat.Id == chatId)?.Message.Text;

        //        if (message != null && message != "" && message != "Создать пост" && message != "/createpost")
        //        {
        //            return Parsing(message);
        //        }
        //        await Task.Delay(1000);
        //    }

        //    throw new OperationCanceledException();
        //}

        //private static Article Parsing(string text)
        //{
        //    List<string> words = text.Split(new char[] { '^' }).ToList();
        //    List<string> genres = words.Where(x => x.StartsWith('#')).ToList();
        //    string genre = "";
        //    foreach(var w in genres)
        //    {
        //        genre += w;
        //    }
        //    StringBuilder description = new StringBuilder(words.FirstOrDefault(x => x.StartsWith("*") && x.EndsWith("*")));
        //    description.Replace('*', ' ');

        //    return new Article(words[0], genre, words.ToList().Last(), description);

        //}
        #endregion

        private static IReplyMarkup? GetCreatePostButtons()
        {
            return new ReplyKeyboardMarkup(new List<List<KeyboardButton>>
            {
                new List<KeyboardButton>{new KeyboardButton("Книга"), new KeyboardButton("Сериал"), new KeyboardButton("Фильм")},
                new List<KeyboardButton>{new KeyboardButton("Манга"), new KeyboardButton("Комикс"), new KeyboardButton("Статья")}
            });
        }

        private static IReplyMarkup? GetAddImageToPostButton()
        {
            return new ReplyKeyboardMarkup(new List<List<KeyboardButton>>
            {
                new List<KeyboardButton>{new KeyboardButton("Да"), new KeyboardButton("Нет")}
            });
        }

        private static IReplyMarkup? GetButtons()
        {
            return new ReplyKeyboardMarkup(new List<List<KeyboardButton>>
                {
                    new List<KeyboardButton> { new KeyboardButton("Создать пост"), new KeyboardButton("Список команд") }
                });
            
        }


    }

    public class TelegraphUploadResult
    {
        public int error { get; set; }
        public List<TelegraphUploadResultItem> result { get; set; }
    }

    public class TelegraphUploadResultItem
    {
        public string src { get; set; }
        public int w { get; set; }
        public int h { get; set; }
    }

}
