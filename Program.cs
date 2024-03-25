using System;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;

using NihongoHelperBot.Classes;
using NihongoHelperBot.Services;

namespace NihongoHelperBot
{
    public static class Program
    {
        /// <summary>
        /// Важные константы
        /// </summary>     
        private static readonly string apiKey = "1142184201:AAHSqjmAO9x--qqsGmLGvKIEAsg6RKZXABg";
        private static readonly TelegramBotClient Bot = new TelegramBotClient(apiKey);     

        /// <summary>
        /// Инициализация бота
        /// </summary>
        public static void Main(string[] args)
        {
            /// Инициализация шедулера
            var sheduler = new Sheduler(Bot);
            
            Thread tr = new Thread(sheduler.Run);
            tr.Start();

            var me = Bot.GetMeAsync().Result;
            Console.Title = me.Username;

            Bot.OnMessage += BotOnMessageReceived;
            Bot.OnMessageEdited += BotOnMessageReceived;
            Bot.OnReceiveError += BotOnReceiveError;

            Bot.StartReceiving(Array.Empty<UpdateType>());
            Console.WriteLine($"Start listening for @{me.Username}");
            Console.ReadLine();
            Bot.StopReceiving();            
        }

        /// <summary>
        /// Обработка приходящих от пользователя комманд
        /// </summary>
        private static async void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs)
        {
            const string usage = @"
                        Фунционал:
/import     - Импорт выбранного Excel файла в систему
/setNotify  - Установить частоту уведомлений";

            var message = messageEventArgs.Message;

            if (message.Type == MessageType.Document)
            {
                FileService fileService = new FileService(Bot,apiKey);
                await fileService.GetFileFromUser(message.From, message.Document.FileId);
            }

            if (message == null || message.Type != MessageType.Text) return;

            switch (message.Text.Split(' ').First())
            {
                case "/import":
                    await Bot.SendTextMessageAsync(
                       message.Chat.Id,
                       "Файл для загрузки должен быть формата Excel (.xlsx). Данные должны быть расположены в формате 'вопрос' и 'ответ' в соседних столбцах");
                    await Bot.SendChatActionAsync(message.Chat.Id, ChatAction.Typing);
                    await Task.Delay(1500); 

                    await Bot.SendTextMessageAsync(
                        message.Chat.Id,
                        "Добавьте Excel файл для анализа. По окончанию загрузки файла появится сообщение об успешном/неуспешном развитии событий.");
                    break;

                case "/setNotify":
                    await Bot.SendChatActionAsync(message.Chat.Id, ChatAction.Typing);

                    await Task.Delay(500); // simulate longer running task

                    await Bot.SendTextMessageAsync(
                       message.Chat.Id,
                       "Щас создастся событие");

                    NotificationService service = new NotificationService(Bot);
                    service.SaveNotification(message.From.Id, 1);

                    break;

                case "/help":
                    await Bot.SendChatActionAsync(message.Chat.Id, ChatAction.Typing);

                    await Task.Delay(500); // simulate longer running task
                    ReplyKeyboardMarkup ReplyKeyboard1 = new[]
                   {
                        new[] { "1.1", "1.2" },
                        new[] { "2.1", "2.2" },
                    };

                    await Bot.SendTextMessageAsync(
                        message.Chat.Id,
                        usage,
                        replyMarkup: ReplyKeyboard1);

                    break;

                case "/notifyInfo":///InProgress
                    await Bot.SendChatActionAsync(message.Chat.Id, ChatAction.Typing);

                    await Task.Delay(500); // simulate longer running task

                    break;

                default:
                    await Bot.SendTextMessageAsync(
                        message.Chat.Id,
                        usage,
                        replyMarkup: new ReplyKeyboardRemove());
                    break;
            }
        }
                   
        private static void BotOnReceiveError(object sender, ReceiveErrorEventArgs receiveErrorEventArgs)
        {
            Console.WriteLine("Received error: {0} — {1}",
                receiveErrorEventArgs.ApiRequestException.ErrorCode,
                receiveErrorEventArgs.ApiRequestException.Message);
        }
    }
}
