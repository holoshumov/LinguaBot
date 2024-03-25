using System;
using System.Linq;
using System.Net;
using System.IO;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Diagnostics;

using Telegram.Bot;
using Microsoft.Office.Interop.Excel;

using NihongoHelperBot.DBContext;

namespace NihongoHelperBot.Services
{
    class FileService
    {
        private TelegramBotClient bot { get; set; }
        private string apiKey { get; set; }

        /// <summary>
        /// Решение бага с Excel
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="botValue">Объект Tg bot-а для рассылки сообщений</param>
        /// <param name="apiKeyValue">Ключ апи бота для скачивания файлов</param>
        public FileService(TelegramBotClient botValue, string apiKeyValue)
        {
            bot = botValue;
            apiKey = apiKeyValue;
        }

        /// <summary>
        /// Получение файлов от пользователя, скачивая их с Telegram
        /// </summary>
        /// <param name="user">Объект пользователя Телеграмма</param>
        /// <param name="fileId">Id файла</param>
        public async Task GetFileFromUser(Telegram.Bot.Types.User user, string fileId)
        {
            var uploadFile = await bot.GetFileAsync(fileId);

            string link = @"https://api.telegram.org/file/bot" + apiKey + "/" + uploadFile.FilePath; //ссылка
            WebClient webClient = new WebClient();
            string path = uploadFile.FileUniqueId + ".xlsx";
            webClient.DownloadFileAsync(new Uri(link), @"Files\\" + path);

            ParsingExcelToDatabase(path, user);
        }

        /// <summary>
        /// Импорт из Excel-файла в БД слов с переводом
        /// </summary>
        /// <param name="pathToFile">Путь к файлу</param>
        /// <param name="user">Объект пользователя Телеграмма</param>
        public async void ParsingExcelToDatabase(string pathToFile, Telegram.Bot.Types.User user)
        {
            try
            {
                string path = @"I:\My Projects\NihongoHelperBot\bin\Debug\Files\" + pathToFile;
                object m = Type.Missing;

                uint processId = 0;

                Application ObjExcel = new Application();

                GetWindowThreadProcessId(new IntPtr(ObjExcel.Hwnd), out processId);

                Workbook ObjWorkBook = ObjExcel.Workbooks.Open(path, 0, false, 5, "", "", false, XlPlatform.xlWindows, "", true, false, 0, true, false, false);
                Worksheet ObjWorkSheet = (Worksheet)ObjWorkBook.Sheets[1];

                Range range = ObjWorkSheet.UsedRange;
                int rowCount = range.Rows.Count;
                int colCount = range.Columns.Count;

                using (DatabaseContext db = new DatabaseContext())
                {
                    var userDB = db.Users.SingleOrDefault(u => u.UserIdInt == user.Id);

                    if (userDB == null)
                    {
                        User newUser = new User
                        {
                            UserName = user.FirstName + " " + user.LastName,
                            UserIdInt = user.Id
                        };
                        db.Users.Add(newUser);
                        db.SaveChanges();
                    }

                    var userDBId = db.Users.SingleOrDefault(u => u.UserIdInt == user.Id).Id;
                    
                    for (int i = 1; i <= rowCount; i++)
                    {
                        if (range.Cells[i, 1].Value2.ToString() != null && range.Cells[i, 2].Value2.ToString() != null)
                        {
                            Question question = new Question
                            {
                                Id_User = db.Users.SingleOrDefault(u => u.UserIdInt == user.Id).Id,
                                QuestionText = range.Cells[i, 1].Value2.ToString(),
                                Answer = range.Cells[i, 2].Value2.ToString()
                            };

                            var updatedQuestion = db.Questions.SingleOrDefault(u => (u.Id_User == userDBId && u.QuestionText == question.QuestionText));

                            if (updatedQuestion == null)
                            {
                                db.Questions.Add(question);
                            }
                            else
                            {
                                updatedQuestion.Answer = question.Answer;
                            }
                        }
                    }
                    db.SaveChanges();
                }

                if (ObjExcel != null)
                {
                    if (ObjExcel.Workbooks != null)
                    {
                        if (ObjExcel.Workbooks.Count < 0)
                        {
                            ObjWorkBook.Close(false, path, null);
                            ObjExcel.Workbooks.Close();
                            ObjExcel.Quit();
                            rowCount = -1;
                            colCount = -1;
                            range = null;
                            ObjWorkSheet = null;
                            ObjWorkBook = null;
                            ObjExcel = null;
                        }
                    }
                }

                try
                {
                    if (processId != 0)
                    {
                        Process excelProcess = Process.GetProcessById((int)processId);
                        excelProcess.CloseMainWindow();
                        excelProcess.Refresh();
                        excelProcess.Kill();
                    }
                }
                catch
                {
                    // Process was already killed
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                GC.WaitForPendingFinalizers();

                File.Delete(path);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                await bot.SendTextMessageAsync(
                    user.Id,
                    "Ошибка!");
            }
            finally
            {
                await bot.SendTextMessageAsync(
                    user.Id,
                    "Файл успешно загружен");
            }
        }

    }
}
