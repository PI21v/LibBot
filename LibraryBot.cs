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
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Routing;
using Telegram.Bots.Types.Passport;
using Kvyk.Telegraph.Models;
using Newtonsoft.Json.Linq;
using System.Reflection.Metadata.Ecma335;

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
            Auto = 11,
        }

        private static string token { get; set; } = "6058875061:AAGE5X5gw2DSOJE2ru58NFtZXXJyDNB9Im4";
        private static long chatId { get; set; } = -1001898169442;//Id чата-предложки
        private static long chatIdMain { get; set; } = -1001705833894;//Id чата-предложки
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
            if (message?.Text != null && message.Chat.Id != chatId && message.Chat.Id != chatIdMain)
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
                        //Чащин
                        fileID = null;
                        fileURL = null;
                        file = null;
                        temp = null;
                        States states = States.PostTypeRequest;//Установка начального состояния ожидания данных(состояние "Запрос типа поста")
                        //
                        long id = message.Chat.Id;
                        string? previous = update?.Message?.Text;
                        await CreatePost(states, client, update, token, id, previous);
                        if (exitFlag)
                        {
                            exitFlag = false;
                            goto case "/start";
                        }
                        await client.SendTextMessageAsync(message.Chat.Id, "Отправляю пост на модерацию");
                        Article article = new Article(title, genre, author, description,postType,tegs);

                        var pagePath = await CreateTelegraphPost(article);
                        await client.SendTextMessageAsync(chatId, pagePath);

                        //Якубенко
                        if (article.Author.Length + article.Genre.Length + article.Description.Length + article.Author.Length > 1000 && file != null)//Если суммарная длина поста превышает 1000 символов, то пост отправляется в виде статьи Telegraph
                        {

                            //var pagePath = await CreateTelegraphPost(article);
                            await client.SendTextMessageAsync(chatIdMain, pagePath);
                        }//
                        else
                        {
                            var photo = new InputOnlineFile(fileID);
                            await client.SendPhotoAsync(chatId, photo, caption: article.ToString());
                            await client.SendPhotoAsync(chatIdMain, photo, caption: article.ToString());

                            Telegram.Bot.Types.Message pollMessage = await client.SendPollAsync(
                                chatId: chatId,
                                question: $"Запрос на публикацию поста от ({message.From.Id}){message.From.Username}",
                                options: new[]
                                {
                                "Публиковать",
                                "Не публиковать"
                                }, cancellationToken: token);//Отправляем сообщение с голосованием                            
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
                }
            }           
        }

        /// <summary>
        /// Метод для загрузки изображения на сервер Telegraph.ph с помощью HTTP-запроса
        /// </summary>
        /// <returns></returns>
        public async static Task<string> UploadImageToTelegraph()
        {
            var imageUrl = $"https://api.telegram.org/file/bot{token}/{file.FilePath}";//Сохраняем Url ссылку изображения            
            using (var httpClient = new HttpClient())//Создаём объект класса для отправки HTTP-запросов
            {
                var imageBytes = await httpClient.GetByteArrayAsync(imageUrl);
                using (var content = new MultipartFormDataContent())//Создаём объект класса для содержания файла изображения
                {
                    using (var imageStream = new MemoryStream(imageBytes))//Создаём объект для чтения бинарных данных изображения
                    {

                        var imageContent = new StreamContent(imageStream);
                        imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpg");
                        content.Add(imageContent, "file", Path.GetFileName(imageUrl));
                        using (var response = await httpClient.PostAsync("https://telegra.ph/upload", content))//Отправляем запрос с изображением на URL-адрес с данными из MultipartFormDataContent
                        {
                            var responseJson = await response.Content.ReadAsStringAsync();//Получаем ответ в формате JSON
                            await Console.Out.WriteLineAsync($"[{DateTime.Now}]{responseJson}");
                            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<TelegraphUploadResult[]>(responseJson);//Выполняем десериализацию и сохраняем в объекте класса TelegraphUploadResult в поле result 
                            if (result != null)
                            {
                                return result[0].src;
                            }
                            else
                            {
                                await Console.Out.WriteLineAsync($"[{DateTime.Now}]Ошибка десериализации изображения с сервера");
                            }
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Создание поста в формате Telegraph статьи
        /// </summary>
        /// <param name="article"></param>
        /// <returns></returns>
        private async static Task<string> CreateTelegraphPost(Article article)
        {
            try
            {
                var client = new TelegraphClient();
                Telegraph.Net.Models.Account account = await client.CreateAccountAsync(
                    "Minoddein"

                );//Регистрация аккаунта Telegraph
                
                var imageUrl = await UploadImageToTelegraph();
                //var imageUrl = $"https://api.telegram.org/file/bot{token}/{file.FilePath}";
                var nodeElementImage = new NodeElement //Создаём элемент статьи с тегом "img" и атрибутом для хранения изображения 
                {
                    Tag = "img",
                    Attributes = new Dictionary<string, string>
                {
                    { "src", imageUrl },                   
                }
                };
               
                NodeElement nodeElementGenres = new NodeElement//Создаём элемент статьи с тегом "_text" и атрибутом для хранения жанров статьи
                {
                    Tag = "_text",
                    Attributes = new Dictionary<string, string> { { "value", "Жанры:" + article.Genre + "\n\n" } }
                };

                NodeElement nodeElementDescription = new NodeElement//Создаём элемент статьи с тегом "_text" и атрибутом для хранения основной информации статьи
                {
                    Tag = "_text",
                    Attributes = new Dictionary<string, string> { { "value", article.Description } }
                };
                var nodes = new List<NodeElement>();
                nodes.Add(nodeElementImage);
                nodes.Add(nodeElementGenres);
                nodes.Add(nodeElementDescription);

                ITokenClient tokenClient = client.GetTokenClient(account.AccessToken);//Получаем токен-доступа для авторизации и работы с ресурсами сервера

                Telegraph.Net.Models.Page page = await tokenClient.CreatePageAsync(article.Title, nodes.ToArray(), article.Author);//Создаём страницу(статья Telegraph)
                return page.Url;//Возвращаем ссылку на статью
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return null;
        }

        /// <summary>
        /// Создаём пост на основе введённых пользователем данных
        /// </summary>
        /// <param name="states"></param>
        /// <param name="client"></param>
        /// <param name="update"></param>
        /// <param name="token"></param>
        /// <param name="id"></param>
        /// <param name="previous"></param>
        /// <returns></returns>
        private async static Task CreatePost(States states, ITelegramBotClient client, Telegram.Bot.Types.Update update, CancellationToken token,long id,string previous)
        {
            switch (states)
            {
                
                case States.WaitPostType:
                    var updates = client.GetUpdatesAsync(offset: 0, limit: 100, timeout: 0).Result;

                    var message = updates.LastOrDefault(u => u.Message != null && u.Message.Chat.Id == id)?.Message?.Text;
                    if (message == "/exit")
                    {
                        exitFlag = true;
                        await client.SendTextMessageAsync(id, "Выход из процесса создания...", replyMarkup: GetCreatePostButtons());
                        return;
                    }
                    if (message != null && message != "" && message != previous)
                    {
                        await client.SendTextMessageAsync(id, "Вы сказали: " + message);
                        
                        await Console.Out.WriteLineAsync($"[{DateTime.Now}]({update.Id})" +
            $":{message}");
                        postType = message;
                        states = States.TitleRequest;
                        previous = message;
                        //await client.EditMessageReplyMarkupAsync(id, idMessage, replyMarkup: null);
                    }                  
                    await Console.Out.WriteLineAsync($"[{DateTime.Now}] Состояние Ожидания.");
                    await Task.Delay(1000);
                    await CreatePost(states, client, update, token, id, previous);
                    break;
                
                case States.TitleRequest:
                    await client.SendTextMessageAsync(id, "Введите название поста: ", replyMarkup: GetExitButton());
                    states = States.WaitTitle;
                    await CreatePost(states, client, update, token, id, previous);
                    await Console.Out.WriteLineAsync($"[{DateTime.Now}] Состояние запроса.");
                    break;
                case States.WaitTitle:
                    
                    updates = client.GetUpdatesAsync(offset: 0, limit: 100, timeout: 0).Result;
                    
                    message = updates.LastOrDefault(u => u.Message != null && u.Message.Chat.Id == id)?.Message?.Text;
                    if (message == "/exit")
                    {
                        exitFlag = true;
                        await client.SendTextMessageAsync(id, "Выход из процесса создания...", replyMarkup: GetCreatePostButtons());
                        return;
                    }
                    if (message != null && message != "" && message != previous)
                    {
                        await client.SendTextMessageAsync(id, "Вы сказали: " + message);
                        await Console.Out.WriteLineAsync($"[{DateTime.Now}]({update.Id})" +
            $":{message}");
                        title = message;
                         states = States.GenreRequest;
                         previous = message;
                    }
                    
                    await Console.Out.WriteLineAsync($"[{DateTime.Now}] Состояние Ожидания.");
                    await Task.Delay(1000);
                    await CreatePost(states, client, update, token, id, previous);
                    
                    break;
                case States.WaitGenre:
                    updates = client.GetUpdatesAsync(offset: 0, limit: 100, timeout: 0).Result;

                    message = updates.LastOrDefault(u => u.Message != null && u.Message.Chat.Id == id)?.Message?.Text;
                    if (message == "/exit")
                    {
                        exitFlag = true;
                        await client.SendTextMessageAsync(id, "Выход из процесса создания...", replyMarkup: GetCreatePostButtons());
                        return;
                    }
                    if (message != null && message != "" && message != previous)
                    {
                        await client.SendTextMessageAsync(id, "Вы сказали: " + message);
                        await Console.Out.WriteLineAsync($"[{DateTime.Now}]({update.Id})" +
            $":{message}");
                        genre = message;
                        states = States.TegsRequest;
                        previous = message;
                    }
                    
                    await Console.Out.WriteLineAsync($"[{DateTime.Now}] Состояние Ожидания.");
                    await Task.Delay(1000);
                    await CreatePost(states, client, update, token, id, previous);
                    break;
                case States.TegsRequest:
                    Message = await client.SendTextMessageAsync(id, "Введите теги поста: ", replyMarkup: GetExitButton());
                    idMessage = Message.MessageId;
                    states = States.WaitTegs;
                    await CreatePost(states, client, update, token, id, previous);
                    await Console.Out.WriteLineAsync($"[{DateTime.Now}] Состояние запроса.");
                    break;
                case States.WaitTegs:
                    updates = client.GetUpdatesAsync(offset: 0, limit: 100, timeout: 0).Result;

                    message = updates.LastOrDefault(u => u.Message != null && u.Message.Chat.Id == id)?.Message?.Text;
                    if (message == "/exit")
                    {
                        exitFlag = true;
                        await client.SendTextMessageAsync(id, "Выход из процесса создания...", replyMarkup: GetCreatePostButtons());
                        return;
                    }
                    if (message != null && message != "" && message != previous)
                    {
                        await client.SendTextMessageAsync(id, "Вы сказали: " + message);
                        await Console.Out.WriteLineAsync($"[{DateTime.Now}]({update.Id})" +
            $":{message}");
                        tegs = message;
                        states = States.DescriptionRequest;
                        previous = message;
                    }
                    await Console.Out.WriteLineAsync($"[{DateTime.Now}] Состояние Ожидания.");
                    await Task.Delay(1000);
                    await CreatePost(states, client, update, token, id, previous);
                    break;               
                case States.DescriptionRequest:
                    await client.SendTextMessageAsync(id, "Введите описание поста: ", replyMarkup: GetExitButton());
                    states = States.WaitDescription;
                    await CreatePost(states, client, update, token, id, previous);
                    await Console.Out.WriteLineAsync($"[{DateTime.Now}] Состояние запроса.");
                    break;
                case States.WaitDescription:
                    updates = client.GetUpdatesAsync(offset: 0, limit: 100, timeout: 0).Result;

                    message = updates.LastOrDefault(u => u.Message != null && u.Message.Chat.Id == id)?.Message?.Text;
                    if (message == "/exit")
                    {
                        exitFlag = true;
                        await client.SendTextMessageAsync(id, "Выход из процесса создания...", replyMarkup: GetExitButton());
                        return;
                    }
                    if (message != null && message != "" && message != previous)
                    {
                        await client.SendTextMessageAsync(id, "Вы сказали: " + message);
                        await Console.Out.WriteLineAsync($"[{DateTime.Now}]({update.Id})" +
            $":{message}");
                        description = message;
                        states = States.AuthorRequest;
                        previous = message;
                    }
                    
                    await Console.Out.WriteLineAsync($"[{DateTime.Now}] Состояние Ожидания.");
                    await Task.Delay(1000);
                    await CreatePost(states, client, update, token, id, previous);
                    break;
                //
                
                case States.AuthorRequest:
                    await client.SendTextMessageAsync(id, "Укажите автора поста: ", replyMarkup: GetAuthorButtons());
                    states = States.WaitAuthor;
                    await CreatePost(states, client, update, token, id, previous);
                    await Console.Out.WriteLineAsync($"[{DateTime.Now}] Состояние запроса.");
                    break;
                case States.WaitAuthor:
                    updates = client.GetUpdatesAsync(offset: 0, limit: 100, timeout: 0).Result;

                    message = updates.LastOrDefault(u => u.Message != null && u.Message.Chat.Id == id)?.Message?.Text;
                    if (message == "/exit")
                    {
                        exitFlag = true;
                        await client.SendTextMessageAsync(id, "Выход из процесса создания...", replyMarkup: GetCreatePostButtons());
                        return;
                    }                   
                    if (message != null && message != "" && message != previous)
                    {
                        await client.SendTextMessageAsync(id, "Вы сказали: " + message);
                        await Console.Out.WriteLineAsync($"[{DateTime.Now}]({update.Id})" +
            $":{message}");
                        author = message;                       
                        states = States.IsWantToSendImage;
                        previous = message;
                    }
                    
                    await Console.Out.WriteLineAsync($"[{DateTime.Now}] Состояние Ожидания.");
                    await Task.Delay(1000);
                    await CreatePost(states, client, update, token, id, previous);
                    break;
                //
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
                case States.Auto:
                    return;
                
                    
            }

        }


        

        
        private static IReplyMarkup? GetAuthorButtons()
        {
            return new ReplyKeyboardMarkup(new List<List<KeyboardButton>>
            {
                new List<KeyboardButton>{new KeyboardButton("Аноним"), new KeyboardButton("/exit")}
            });
        }
        
        private static IReplyMarkup? GetExitButton()
        {
            return new ReplyKeyboardMarkup(new List<KeyboardButton>
            {               
                new KeyboardButton("/exit")
            });
        }
        

        private static IReplyMarkup? GetAddImageToPostButton()
        {
            return new ReplyKeyboardMarkup(new List<List<KeyboardButton>>
            {
                new List<KeyboardButton>{new KeyboardButton("Да"), new KeyboardButton("Нет")}
            });

        }

    }

    
    public class TelegraphUploadResult
    {
        public int error { get; set; }       
        public string src { get; set; }
        public int w { get; set; }
        public int h { get; set; }
        public List<TelegraphUploadResultItem> result { get; set; }
    }

    public class TelegraphUploadResultItem
    {
        public string src { get; set; }
        public int w { get; set; }
        public int h { get; set; }
    }

}
