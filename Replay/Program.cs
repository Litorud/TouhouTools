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
                Console.WriteLine("リプレイファイルが指定されていません。");
                return;
            }

            var rpy = args[0];
            if (!File.Exists(rpy))
            {
                Console.WriteLine("指定したファイルは存在しません。");
                return;
            }

            var prefix = GetPrefix(rpy);
            var code = GetCode(prefix);
            var gameInfo = TouhouTools.Program.SearchGame(code);
            if (gameInfo == null)
            {
                Console.WriteLine("指定したファイルに対応するゲームを、このコンピューターから見つけることができませんでした。");
                return;
            }

            var replay = Path.Combine(gameInfo.SaveFolder, "replay");
            Directory.CreateDirectory(replay);

            string tempRpy;
            try
            {
                tempRpy = GetTempRpyPath(replay, prefix);
            }
            catch (Exception)
            {
                Console.WriteLine("同名のファイルが存在するため、コピーできません。");
                return;
            }
            File.Copy(rpy, tempRpy);

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
            if (prefix.Length == 3 || prefix.Equals("th95", StringComparison.OrdinalIgnoreCase))
            {
                return prefix.Insert(2, "0");
            }

            return prefix;
        }

        private static string GetTempRpyPath(string replay, string prefix)
        {
            if (prefix.Equals("th10", StringComparison.OrdinalIgnoreCase))
            {
                for (var i = 25; i >= 1; i--)
                {
                    var path = Path.Combine(replay, $"{prefix}_{i:00}.rpy"); // TH10 の可能性があるので、th10 決め打ちにしない。
                    if (!File.Exists(path))
                    {
                        return path;
                    }
                }
            }
            else
            {
                var path = Path.Combine(replay, $"{prefix}_udTemp.rpy");
                if (!File.Exists(path))
                {
                    return path;
                }

                var temp = "Temp".AsSpan();
                for (var i = 1; i <= 9999; i++)
                {
                    var str = i.ToString();
                    var part = temp[..^str.Length].ToString(); // ReadOnlySpan<char>.ToString() は文字列のコピーを作らない。
                    var name = $"{prefix}_ud{part}{str}.rpy";

                    path = Path.Combine(replay, name);
                    if (!File.Exists(path))
                    {
                        return path;
                    }
                }
            }

            throw new Exception();
        }
    }
}
