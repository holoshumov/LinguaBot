using System;
using System.IO;
using System.Linq;
using System.Speech.AudioFormat;
using System.Speech.Synthesis;
using System.Threading;
using System.Threading.Tasks;

using Telegram.Bot;

using NihongoHelperBot.DBContext;


namespace NihongoHelperBot.Services
{
    class NotificationService
    {
        private static readonly string file = @"Files\\test.wav";
        private static readonly string botVoice = "Microsoft Haruka Desktop";

        private TelegramBotClient bot { get; set; }

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="botValue">Объект Tg bot-а для рассылки сообщений</param>
        public NotificationService(TelegramBotClient botValue)
        {
            bot = botValue;
        }

        /// <summary>
        /// Сохранение в БД оповещения с интервалом от текущего момента
        /// </summary>
        /// <param name="userId">Id пользователя </param>
        /// <param name="timeout">Минутный интервал следующего оповещения</param>
        public async void SaveNotification(int userId, int timeout)
        {
            using (DatabaseContext db = new DatabaseContext())
            {
                var userDB = db.Users.SingleOrDefault(u => u.UserIdInt == userId);
                if (userDB == null)
                {
                    await bot.SendTextMessageAsync(
                        userId,
                        "У вас нету данных для рассылки! Импортируйте вопросы по команде /import");
                }
                else
                {
                    var time = DateTime.Now.AddMinutes(timeout);
                    var createdNotify = new Notification
                    {
                        Id_User = userId,
                        Timeout = timeout.ToString(),
                        Is_Notificated = false,
                        NextNotify = time.ToString()
                    };
                    var notification = db.Notifications.SingleOrDefault(n => n.Id_User == userId);

                    if (notification == null)
                        db.Notifications.Add(createdNotify);
                    else
                    {
                        notification.NextNotify = createdNotify.NextNotify;
                        notification.Timeout = createdNotify.Timeout;
                    }

                    db.SaveChanges();
                }
            }
        }

        /// <summary>
        /// Отправка оповещения с рандомным словом и ответом пользователю
        /// </summary>
        /// <param name="userId">Id пользователя</param>
        public async void SendNotificationToUser(int userId)
        {
            try
            {
                using (DatabaseContext db = new DatabaseContext())
                {
                    var userDB = db.Users.SingleOrDefault(u => u.UserIdInt == userId);

                    if (userDB == null)
                        await bot.SendTextMessageAsync(
                            userId,
                            "Сначала импортируйте вопросы, прежде чем запускать отправку");
                    else
                    {
                        var questions = db.Questions.Where(q => q.Id_User == userDB.Id).ToList();
                        var question = questions[new Random().Next(0, questions.Count - 1)];
                        await GetAudioByText(question.QuestionText);

                        var fileName = file.Split(Path.DirectorySeparatorChar).Last();

                        using (var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            await bot.SendAudioAsync(
                                userId,
                                fileStream,
                                question.QuestionText);
                        }

                        await bot.SendTextMessageAsync(
                            userId,
                            "Через 10 секунд прийдет ответ");

                        Thread.Sleep(10 * 1000);

                        await bot.SendTextMessageAsync(
                           userId,
                           @"Ответ: " + question.Answer);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        /// <summary>
        /// Метод для использование TTS
        /// </summary>
        /// <param name="text">Текст для TTS</param>
        /// <returns>(неявно) Аудиофайл </returns>
        private async Task GetAudioByText(string text)
        {
            using (SpeechSynthesizer speechSynth = new SpeechSynthesizer())
            { // создаём объект
                speechSynth.Volume = 100; // устанавливаем уровень звука

                var allVoices = speechSynth.GetInstalledVoices();
                var selectedVoice = allVoices.FirstOrDefault(z => z.VoiceInfo.Name == botVoice);

                speechSynth.SelectVoice(selectedVoice.VoiceInfo.Name); // устанавливаем данную озвучку
                speechSynth.SetOutputToWaveFile(file, new SpeechAudioFormatInfo(32000, AudioBitsPerSample.Sixteen, AudioChannel.Mono));
                speechSynth.Speak(text);
            }
        }
    }
}
