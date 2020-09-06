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

            var fileName = Path.GetFileName(rpyPath);
            var underscoreIndex = fileName.IndexOf('_');
            var code = fileName.Substring(0, underscoreIndex);
            var gameInfo = TouhouTools.Program.SearchGame(code);
            if (gameInfo == null)
            {
                return;
            }

            var replay = Path.Combine(gameInfo.SaveFolder, "replay");
            Directory.CreateDirectory(gameInfo.SaveFolder);

            var tempRpy = Path.Combine(replay, $"{code}_udTemp.rpy");
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
    }
}
