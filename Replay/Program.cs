using System;
using System.Diagnostics;
using System.IO;
using TouhouTools;

namespace Replay
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                return;
            }

            var rpyPath = args[0];
            if (!File.Exists(rpyPath))
            {
                return;
            }

            var prefix = GetPrefix(rpyPath);
            var code = GetCode(prefix);
            var gameInfo = TouhouTools.Program.SearchGame(code);
            if (gameInfo == null)
            {
                return;
            }

            var replay = Path.Combine(gameInfo.SaveFolder, "replay");
            Directory.CreateDirectory(replay);

            var tempRpy = Path.Combine(replay, $"{prefix}_udTemp.rpy");
            File.Copy(rpyPath, tempRpy);

            var process = gameInfo switch
            {
                ShortcutGameInfo _ => Process.Start(new ProcessStartInfo(gameInfo.StartPath)
                {
                    UseShellExecute = true
                }),
                ExecutableGameInfo i => Process.Start(new ProcessStartInfo(i.StartPath)
                {
                    WorkingDirectory = i.WorkingDirectory
                }),
                _ => throw new NotImplementedException()
            };

            process.WaitForExit();
            File.Delete(tempRpy);
        }

        private static string GetPrefix(string rpyPath)
        {
            var fileName = Path.GetFileName(rpyPath);
            var underscoreIndex = fileName.IndexOf('_');
            return fileName.Substring(0, underscoreIndex);
        }

        private static string GetCode(string prefix)
        {
            if (prefix.Length == 3 || prefix == "th95")
            {
                return prefix.Insert(2, "0");
            }

            return prefix;
        }
    }
}
