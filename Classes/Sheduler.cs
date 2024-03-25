using System;
using System.Linq;
using System.Threading;

using Telegram.Bot;

using NihongoHelperBot.Services;

namespace NihongoHelperBot.Classes
{
    /// <summary>
    /// Шедулер для рассылки оповещений
    /// </summary>
    class Sheduler
    {
        private static readonly int timeoutSheduler = 1;
        private TelegramBotClient bot { get; set; }                

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="botValue">Объект Tg bot-а для рассылки сообщений</param>
        public Sheduler (TelegramBotClient botValue)
        {
            bot = botValue;
        }

        public async void Run()
        {
            /// Костыль для самописного шедулера
            while (true)
            {
                using (DatabaseContext db = new DatabaseContext())
                {
                    var notifications = db.Notifications.ToList();

                    if (notifications.Count == 0)
                    {
                        Console.WriteLine("Нечего расслылать");
                    }
                    else
                    {
                        foreach (var notify in notifications)
                        {
                            var notifyDateTime = Convert.ToDateTime(notify.NextNotify);
                            if (notifyDateTime.CompareTo(DateTime.Now) <= 0)
                            {
                                var notificationService = new NotificationService(bot);
                                notificationService.SendNotificationToUser(notify.Id_User);
                                notificationService.SaveNotification(notify.Id_User, Convert.ToInt32(notify.Timeout));
                            }
                        }
                    }
                }
                Thread.Sleep(timeoutSheduler * 60 * 1000);
            }
        }
    }
}
