using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Oracle.DataAccess.Client;
using System.Data;
using System.IO;
using System.Security.Cryptography;
using System.Collections;
using System.Threading;


namespace sibintec_test
{
    class Program
    {
        public static MyDeque<FileObject> array_1 = new MyDeque<FileObject>();
        public static MyDeque<FileObject> array_2 = new MyDeque<FileObject>();
        static object locker = new object();
        static string defaultFolder = @"D:\\My documentos\";
        public static int filesCount = 0;
        public static int queueProgress = 0;
        public static int recordsSaved;
        public static int errorRaised;
        public static bool isFilesCounted = false;
        public static DateTime timer;

        static void Main(string[] args)
        {
            timer = DateTime.Now;

            MyDeque<string> foldersArray = new MyDeque<string>();
            foldersArray.EnqueueLast(defaultFolder);
            foreach(string folderPath in args)
                foldersArray.EnqueueLast(folderPath);
            foldersArray.EnqueueLast(@"D:\\VIDEO\");

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


            Console.Read();


            //Console.ReadKey();
            /*
                var connString = "Data Source=ORCL;User Id=oracle_user;Password=oracle_pass;Enlist=false;DBA Privilege=SYSDBA";
                var strSQL = "UPDATE SYS.FILES_CONTENT SET FILES_CONTENT_HASH = 'sfhbsdfb' WHERE FILES_CONTENT_ID = 1;"; //and reason='П01'
                using (OracleConnection conn = new OracleConnection(connString))
                {
                    conn.Open();
                    using (OracleCommand cmd = new OracleCommand(strSQL, conn))
                    using (OracleTransaction tx = conn.BeginTransaction(IsolationLevel.ReadCommitted))
                    {
                        try
                        {
                            if (cmd.ExecuteNonQuery() < 0)
                            {
                                throw new Exception("Unable to set id_package");
                            }
                            else
                            {
                                tx.Commit();
                            }
                        }
                        catch (Exception e)
                        {
                            tx.Rollback();
                        }
                    }

                    var key = Console.ReadKey();

                }
                */
        }



        public static void GetFiles(object foldersForOperate)
        {
            //Console.WriteLine("Запущен {0}", Thread.CurrentThread.Name);
            MyDeque<string> files = new MyDeque<string>();
            string[] filesInFolder;
            string localFileName;

            foreach (string folderPath in (MyDeque<string>)foldersForOperate)
            {
                filesInFolder = new DirectoryInfo(folderPath).GetFiles("*.*", SearchOption.AllDirectories).Select(f => f.FullName).ToArray();
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
            
            //Console.WriteLine("{0} завершает работу", Thread.CurrentThread.Name);
            Thread.CurrentThread.Abort();
        }

        static public void HashCalculate()
        {
            //Console.WriteLine("Запущен {0}", Thread.CurrentThread.Name);
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

            //Console.WriteLine("{0} завершает работу. Успешно обработано {1} файлов, ошибок возникло {2}", Thread.CurrentThread.Name, handledFiles, raisedErrors);
            Thread.CurrentThread.Abort();

        }
            

        static public void RecordsSave()
        {
            FileObject objectToSave;
            bool stopThreadSignal = false;

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
                            recordsSaved++;
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

            //Console.WriteLine("{0} завершает работу. Успешно создано {1} записей в БД", Thread.CurrentThread.Name, savedRecords);
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

            //public static int filesCount = 1;
            //public static int queueProgress = 0;
            //public static int recordsSaved;
            //public static int errorRaised;
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
