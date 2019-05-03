using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MobileAnalysis
{
    class Program
    {
        public static int File_Count = 0;
        public static string Source_Dir = "";

        public static void DeleteTempJar()
        {
            if (File.Exists(@".\temp\MANIFEST.MF"))
            {
                File.Delete(@".\temp\MANIFEST.MF");
            }
        }

        public static void GetManifestFromZip(string file)
        {
            using (ZipArchive archive = ZipFile.OpenRead(file))
            {
                var manifest = archive.Entries.First(m => m.FullName == "META-INF/MANIFEST.MF");
                manifest.ExtractToFile(Path.Combine(@".\temp\", manifest.Name));
            }
        }

        public static void ProcessFiles()
        {
            var games = new List<GameInfo>();
            var files = Directory.GetFiles(Source_Dir);

            var games_done = new List<GameInfo>();

            if (File.Exists("games-batch.json"))
            {
                games_done = Newtonsoft.Json.JsonConvert.DeserializeObject<List<GameInfo>>(File.ReadAllText("games-batch.json"));
            }

            var current_count = 0;

            foreach (var file in files)
            {
                Console.Write("\r{0}/{1} - {2}", current_count, File_Count, Path.GetFileName(file));
                current_count++;

                var found = false;

                foreach (var done in games_done)
                {
                    if (done.Filename == Path.GetFileName(file))
                    {
                        games.Add(games_done[current_count - 1]);
                        found = true;
                        break;
                    }
                }

                if (found)
                {
                    continue;
                }

                try
                {
                    GetManifestFromZip(file);

                    if (File.Exists(@".\temp\MANIFEST.MF"))
                    {
                        var strs = File.ReadAllLines(@".\temp\MANIFEST.MF");

                        var game = new GameInfo();

                        foreach (var str in strs)
                        {
                            if (str.Contains("MIDlet-Name"))
                            {
                                var name = str.Split(':');
                                game.Name = name[1].ToString().Trim(); ;
                            }

                            if (str.Contains("MIDlet-Vendor"))
                            {
                                var name = str.Split(':');
                                game.Vendor = name[1].ToString().Trim();
                            }

                            if (str.Contains("MIDlet-Version"))
                            {
                                var name = str.Split(':');
                                game.Version = name[1].ToString().Trim();
                            }
                        }

                        game.Filename = Path.GetFileName(file);

                        using (var md5 = MD5.Create())
                        {
                            using (var stream = File.OpenRead(file))
                            {
                                var hash = md5.ComputeHash(stream);
                                game.MD5 = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                            }
                        }

                        games.Add(game);

                        DeleteTempJar();
                    }
                    else
                    {
                        File.Move(file, Source_Dir + @"\_Broken\" + Path.GetFileName(file));
                        continue;
                    }

                }
                catch (Exception ex)
                {
                    File.Move(file, Source_Dir + @"\_Broken\" + Path.GetFileName(file));
                    DeleteTempJar();

                    continue;
                }

                var json = Newtonsoft.Json.JsonConvert.SerializeObject(games, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText("games-batch-new.json", json);
            }
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Enter Source Path:");
            Source_Dir = Console.ReadLine();

            Console.WriteLine(String.Format("Source Path: {0}", Source_Dir));

            var valid_path = false;

            try
            {
                Path.GetFullPath(Source_Dir);
                valid_path = true;
            }
            catch(Exception)
            {
                Console.WriteLine("Error Invalid Path");
                
            }

            if (valid_path)
            {
                if (!Directory.Exists(Source_Dir))
                {
                    Directory.CreateDirectory(Source_Dir);
                }

                if (!Directory.Exists(Source_Dir + @"\_Broken\"))
                {
                    Directory.CreateDirectory(Source_Dir + @"\_Broken\");
                }

                if (!Directory.Exists(@".\temp\"))
                {
                    Directory.CreateDirectory(@".\temp\");
                }

                var games = new List<GameInfo>();
                var files = Directory.GetFiles(Source_Dir);

                File_Count = files.Count();
                ProcessFiles();
            }
        }
    }
}
