using Microsoft.Win32;
using ShellLink;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TouhouTools
{
    public class Program
    {
        static void Main()
        {
            Console.WriteLine("スタートメニューからゲームの情報を取得します。");

            var games = SearchGames();

            foreach (var info in games)
            {
                Console.WriteLine($@"++++++++++++++++++++++++++++++++++++++++
{info.Name}
----------------------------------------
起動パス　　　　: {info.StartPath}
セーブフォルダー: {info.SaveFolder}
");
            }
        }

        public static IEnumerable<GameInfo> SearchGames()
        {
            return SearchPrograms(Environment.SpecialFolder.CommonPrograms)
                .Concat(SearchPrograms(Environment.SpecialFolder.Programs))
                .Concat(SearchRegistry(@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"))
                .Concat(SearchRegistry(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"));
        }

        private static IEnumerable<GameInfo> SearchPrograms(Environment.SpecialFolder programsFolder)
        {
            var programs = Environment.GetFolderPath(programsFolder);

            // スタートメニューの中から、“上海アリス幻樂団”というディレクトリを再帰的に探す。
            var directories = Directory.EnumerateDirectories(programs, "上海アリス幻樂団", SearchOption.AllDirectories);

            foreach (var directory in directories)
            {
                var gameDirectories = Directory.EnumerateDirectories(directory);

                foreach (var gameDirectory in gameDirectories)
                {
                    var gameName = Path.GetFileName(gameDirectory);
                    var shortcutPath = Path.Combine(gameDirectory, $"{gameName}.lnk");

                    if (File.Exists(shortcutPath))
                    {
                        var info = new ShortcutGameInfo
                        {
                            Name = gameName,
                            StartPath = shortcutPath
                        };

                        var shortcut = Shortcut.ReadFromFile(shortcutPath);
                        var exePath = shortcut.LinkTargetIDList.Path;
                        var exeName = Path.GetFileNameWithoutExtension(exePath);
                        info.Code = exeName == "東方紅魔郷" ? "th06" : exeName;

                        if (info.Code.CompareTo("th125") < 0)
                        {
                            info.SaveFolder = GetSaveFolder(info.Code, shortcut.StringData.WorkingDir);
                        }
                        else
                        {
                            // このメソッドは繰り返し呼び出されるので、
                            // Path.Combine(applicationData, "ShanghaiAlice") をキャッシュする余地がある。
                            // ただし、必要無いのに初期化されないようにしなくてはならない。
                            var applicationData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                            info.SaveFolder = Path.Combine(applicationData, "ShanghaiAlice", exeName);
                        }

                        yield return info;
                    }
                }
            }
        }

        private static IEnumerable<GameInfo> SearchRegistry(string uninstallKey)
        {
            using (var key = Registry.LocalMachine.OpenSubKey(uninstallKey))
            {
                foreach (var name in key.GetSubKeyNames())
                {
                    var subKey = key.OpenSubKey(name);

                    // インストール時にインストール先を変えた場合でも、Inno Setup: Icon Group の値は “上海アリス幻樂団\東方○○○” のようになり、
                    // 必ず “上海アリス幻樂団” が含まれる。
                    var iconGroup = Convert.ToString(subKey.GetValue("Inno Setup: Icon Group"));
                    if (!iconGroup.Contains("上海アリス幻樂団"))
                    {
                        continue;
                    }

                    var workingDirectory = Convert.ToString(subKey.GetValue("InstallLocation"));
                    var exeCandidates = Directory.EnumerateFiles(workingDirectory, "th???.exe");
                    if (!exeCandidates.Any())
                    {
                        continue;
                    }

                    var info = new ExecutableGameInfo
                    {
                        Name = Convert.ToString(subKey.GetValue("Inno Setup: Icon Group")),
                        StartPath = exeCandidates.First(),
                        WorkingDirectory = workingDirectory
                    };

                    var exeName = Path.GetFileNameWithoutExtension(info.StartPath);
                    info.Code = exeName == "東方紅魔郷" ? "th06" : exeName;

                    if (info.Code.CompareTo("th125") < 0)
                    {
                        info.SaveFolder = GetSaveFolder(info.Code, workingDirectory);
                    }
                    else
                    {
                        // このメソッドは繰り返し呼び出されるので、
                        // Path.Combine(applicationData, "ShanghaiAlice") をキャッシュする余地がある。
                        // ただし、必要無いのに初期化されないようにしなくてはならない。
                        var applicationData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                        info.SaveFolder = Path.Combine(applicationData, "ShanghaiAlice", exeName);
                    }

                    yield return info;
                }
            }
        }

        private static string GetSaveFolder(string code, string workingDirectory)
        {
            var localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var virtualStore = new DirectoryInfo(Path.Combine(localApplicationData, "VirtualStore"));
            if (virtualStore.Exists)
            {
                var parentName = Path.GetFileName(workingDirectory);
                var parents = virtualStore.EnumerateDirectories(parentName, SearchOption.AllDirectories);

                // 本当にゲームのバーチャルストアかどうか、スコアファイルの有無で確かめる。
                var scoreFile = code.CompareTo("th095") < 0 ? "score.dat" : $"score{code}.dat";
                var parent = parents.FirstOrDefault(p => File.Exists(Path.Combine(p.FullName, scoreFile)));
                if (parent != null)
                {
                    return parent.FullName;
                }
            }

            return workingDirectory;
        }

        public static GameInfo SearchGame(string code)
        {
            return SearchGames()
                .Where(g => g.Code.Equals(code, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();
        }
    }
}
