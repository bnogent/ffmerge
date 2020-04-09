using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ffmerge
{
    public class Program
    {
        public static string ComputeHashMD5(string filename)
        {            
            using (var tool = SHA1.Create())
            {
                //using (var stream = File.OpenRead(filename))
                using (var stream = new BufferedStream(File.OpenRead(filename), 1200000))
                {
                    var hash = tool.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        public static long newFilesCount = 0;
        public static long newFilesSize = 0;

        public static long existingFilesCount = 0;
        public static long existingFilesSize = 0;

        public static int errors = 0;


        public static void LogErrorMessage(string err)
        {
            string[] enum2= new string[] {err };
            File.AppendAllLines(logerr, enum2);
        }


        private static string[] enum1 = new string[1];
        public static void ParseDirAnCopy(DirectoryInfo dir, string dirToCopy)
        {                    
            try
            {
                foreach (var f in dir.GetFiles())
                {         
                    try
                    {
                        //if (f.FullName.EndsWith(".svn-base"))
                        //    continue;


                        var h = ComputeHashMD5(f.FullName);
                        if (!db.Contains(h))
                        {

                            if (!simulate)
                            {
                                Directory.CreateDirectory(dirToCopy);
                                string filename = Path.Combine(dirToCopy, f.Name);
                                // create a copy of a file if it already exists and different in the same directory
                                while (File.Exists(filename))
                                    filename += "%";
                                File.Copy(f.FullName, filename, true);
                            }

                            newFilesCount++;
                            newFilesSize += f.Length;

                            enum1[0] = h;                                                        
                            File.AppendAllLines(dbFile, enum1);
                            db.Add(h);

                            if ((newFilesCount + existingFilesCount)%100==0)
                                System.Console.WriteLine($"count={newFilesCount+existingFilesCount}, size={newFilesSize+existingFilesSize} => hash={h}, name={f.FullName}");

                        } else
                        {
                            existingFilesCount++;
                            existingFilesSize += f.Length;
                        }                                               
                    }
                    catch 
                    {                        
                        OnError($"Can't read file :{f.FullName}");
                    }
                }
            }
            catch 
            {
                OnError($"Can't list files of :{dir}");                
            }

            try
            {
                foreach (var d in dir.GetDirectories())                
                    ParseDirAnCopy(d, Path.Combine(dirToCopy, d.Name));                
            }
            catch
            {
                OnError($"Can't list directories of :{dir}");                
            }
        }

        private static void OnError(string err)
        {
            LogErrorMessage(err);
            Console.WriteLine(err);
            errors++;
        }

        public static HashSet<string> db = new HashSet<string>();
        public static string dbFile;
        public static string consolidatedDir;
        public static string dirToSave;
        public static string logerr;


        public static bool simulate = true;

        public static void Main(string[] args)
        {

            Stopwatch watch = Stopwatch.StartNew();

            dbFile = args[0];
            consolidatedDir = args[1];
            logerr = args[2];
            dirToSave = args[3];

            if (File.Exists(dbFile))
            {
                Console.Write("Reading db...");
                string line;
                using (StreamReader file = new StreamReader(dbFile))
                    while ((line = file.ReadLine()) != null)
                        db.Add(line);
                Console.WriteLine();
            }
            Console.WriteLine("Let's go");
            ParseDirAnCopy(new DirectoryInfo(dirToSave), consolidatedDir);
            Console.WriteLine("The end.");


            Console.WriteLine();
            Console.WriteLine($"New files: count={newFilesCount}, size={newFilesSize}");
            Console.WriteLine($"Existing files: count={existingFilesCount}, size={existingFilesSize}");
            Console.WriteLine($"Total: count={newFilesCount+ existingFilesCount}, size={newFilesSize+ existingFilesSize}");
            Console.WriteLine($"Errors : {errors} - see '{logerr}'");

            watch.Stop();
            Console.WriteLine($"Elapsed hours={watch.Elapsed.TotalHours}");
        }
    }
}