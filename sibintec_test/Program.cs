using System;
using System.Linq;
using Oracle.DataAccess.Client;
using System.Data;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Configuration;

namespace sibintec_test
{
    class Program
    {
        public static MyDeque<FileObject> array_1 = new MyDeque<FileObject>();
        public static MyDeque<FileObject> array_2 = new MyDeque<FileObject>();
        static object locker = new object();
        static string defaultFolder;
        public static int filesCount = 0;
        public static int queueProgress = 0;
        public static int recordsSaved;
        public static int errorRaised;
        public static bool isFilesCounted = false;
        public static DateTime timer;

        static void Main(string[] args)
        {
            Console.WriteLine("START");
            timer = DateTime.Now;
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            defaultFolder = config.AppSettings.Settings["folderForWork"].Value;

            MyDeque<string> foldersArray = new MyDeque<string>();
            foldersArray.EnqueueLast(defaultFolder);
            foreach(string folderPath in args)
                foldersArray.EnqueueLast(folderPath);

            Thread firstThread = new Thread(new ParameterizedThreadStart(GetFiles));
            firstThread.Name = "Поток 1 для сбора файлов";
            firstThread.Start(foldersArray);

            Thread secondThread = new Thread(HashCalculate);
            secondThread.Name = "Поток 2 для подсчёта хэша";
            secondThread.Start();

            Thread thirdThread = new Thread(RecordsSave);
            thirdThread.Name = "Поток 3 для сохранения результата в БД";
            thirdThread.Start();

            Thread fourthThread = new Thread(ShowProgress);
            fourthThread.Name = "Поток 4 для вывода общего прогресса работы";
            fourthThread.Start();

            Console.ReadKey();
        }



        public static void GetFiles(object foldersForOperate)
        {
            Console.WriteLine("Запущен {0}", Thread.CurrentThread.Name);
            MyDeque<string> files = new MyDeque<string>();
            string[] filesInFolder;
            string localFileName;

            foreach (string folderPath in (MyDeque<string>)foldersForOperate)
            {
                filesInFolder = new DirectoryInfo(folderPath).GetFiles("*", SearchOption.AllDirectories).Select(f => f.FullName).ToArray();
                foreach (string fullPath in filesInFolder)
                {
                    files.EnqueueLast(fullPath);
                }
            }

            filesCount = files.Count();
            isFilesCounted = true;

            foreach (string fullPath in files)
            {
                lock(locker)
                {
                    localFileName = Path.GetFileName(fullPath);
                    array_1.EnqueueLast(new FileObject() { filePath = fullPath, fileName = localFileName });
                }
            }

            lock (locker)
            {
                array_1.EnqueueFirst(new FileObject() { stopThread = true });
            }
            
            Console.WriteLine("{0} завершает работу", Thread.CurrentThread.Name);
            Thread.CurrentThread.Abort();
        }

        static public void HashCalculate()
        {
            Console.WriteLine("Запущен {0}", Thread.CurrentThread.Name);
            FileObject fileObject;
            bool stopThreadSignal = false;
            MyDeque<FileObject> localStorage = new MyDeque<FileObject>();

            do
            {
                if (array_1.IsEmpty)
                {
                    //Console.WriteLine("Поток 2 ожидает появления файлов в очереди на обработку");
                    Thread.Sleep(200);
                }
                else
                {
                    lock (locker)
                    {
                        fileObject = array_1.DequeueLast();
                    }
                    stopThreadSignal = fileObject.stopThread;

                    if (stopThreadSignal == false)
                    {
                        try
                        {
                            string md5 = ComputeMD5Checksum(fileObject.filePath);
                            fileObject.fileHash = md5;
                            //Console.WriteLine(md5);
                        }
                        catch (Exception e)
                        {
                            fileObject.fileError = e.Message;
                            //Console.WriteLine(e.Message);
                            errorRaised++;
                        }
                        finally
                        {
                            lock (locker)
                            {
                                array_2.EnqueueLast(fileObject);
                            }
                        }
                    }
                }
                     
            }
            while (stopThreadSignal == false);

            lock (locker)
            {
                array_2.EnqueueFirst(new FileObject() { stopThread = true });
            }

            Console.WriteLine("{0} завершает работу", Thread.CurrentThread.Name);
            Thread.CurrentThread.Abort();

        }
            

        static public void RecordsSave()
        {
            Console.WriteLine("Запущен {0}", Thread.CurrentThread.Name);
            string connString = "Data Source=ORCL;User Id=sibintech_normal;Password=sibintech_pass;Enlist=false;";
            FileObject objectToSave;
            string[] stringParams = new string[2];
            bool stopThreadSignal = false;

            stringParams[0] = connString;

            do
            {
                if (array_2.IsEmpty)
                {
                    //Console.WriteLine("Поток 3 ожидает появления объектов в очереди на обработку");
                    Thread.Sleep(200);
                }
                else
                {
                    lock (locker)
                    {
                        objectToSave = array_2.DequeueLast();
                    }
                    stopThreadSignal = objectToSave.stopThread;

                    if (stopThreadSignal == false)
                    {
                        try
                        {
                            string strSQL = String.Format("INSERT INTO SIBINTECH_NORMAL.FILES_CONTENT (FILE_NAME, FILE_PATH, FILE_HASH, FILE_ERROR) VALUES ('{0}', '{1}', '{2}', '{3}')", objectToSave.fileName, objectToSave.filePath, objectToSave.fileHash, objectToSave.fileError);
                            //Console.WriteLine(strSQL);
                            stringParams[1] = strSQL;
                            bool saveResult = ExecuteSqlQuery(stringParams);
                            recordsSaved++;
                            if (!saveResult)
                                errorRaised++;
                        }
                        catch (Exception e)
                        {
                            //Console.WriteLine(e.Message);
                            errorRaised++;
                        }
                        finally
                        {
                            queueProgress++;
                        }
                    }
                }

            }
            while (stopThreadSignal == false);

            Console.WriteLine("{0} завершает работу", Thread.CurrentThread.Name);
            Thread.CurrentThread.Abort();
        }

        static public string ComputeMD5Checksum(string path)
        {
            using (FileStream fs = System.IO.File.OpenRead(path))
            using (MD5 md5 = new MD5CryptoServiceProvider())
            {
                byte[] checkSum = md5.ComputeHash(fs);
                string result = BitConverter.ToString(checkSum).Replace("-", String.Empty);
                return result;
            }
        }

        static public bool ExecuteSqlQuery(string[] stringParams)
        {
            string connString = stringParams[0];
            string sqlQuery = stringParams[1];

            using (OracleConnection conn = new OracleConnection(connString))
            {
                conn.Open();
                using (OracleCommand cmd = new OracleCommand(sqlQuery, conn))
                using (OracleTransaction tx = conn.BeginTransaction(IsolationLevel.ReadCommitted))
                {
                    try
                    {
                        if (cmd.ExecuteNonQuery() < 0)
                        {
                            //throw new Exception("Unable to set id_package");
                            return false;
                        }
                        else
                        {
                            tx.Commit();
                            return true;
                        }
                    }
                    catch (Exception e)
                    {
                        tx.Rollback();
                        return false;
                    }
                }

            }

        }

        static public void ShowProgress()
        {
            TimeSpan timeElapsed;
            do
            {
                Thread.Sleep(200);
            }
            while (isFilesCounted == false);

            using (var progress = new ProgressBar())
            {
                do
                {
                    progress.Report((double)queueProgress / filesCount);
                }
                while (queueProgress < filesCount);
            }
            timeElapsed = DateTime.Now - timer;
            Console.WriteLine("Работа программы завершена");
            Console.WriteLine("Файлов найдено: {0}", filesCount);
            Console.WriteLine("Файлов обработано: {0}", queueProgress);
            Console.WriteLine("Записей сохранено в БД: {0}", recordsSaved);
            Console.WriteLine("Ошибок произошло: {0}", errorRaised);
            Console.WriteLine("Время, затраченное на работу: {0}", timeElapsed);

            Thread.CurrentThread.Abort();
        }

    }
    public class FileObject
    {
        public string fileName;
        public string filePath;
        public string fileHash;
        public string fileError;
        public bool stopThread = false;
    }





}
