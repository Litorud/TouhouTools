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
                .Concat(SearchRegistry(Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"))
                .Concat(SearchRegistry(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"))
                .Concat(SearchRegistry(Registry.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"));
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
                        var shortcut = Shortcut.ReadFromFile(shortcutPath);
                        var exePath = shortcut.LinkTargetIDList.Path;
                        var exeName = Path.GetFileNameWithoutExtension(exePath);
                        var code = exeName == "東方紅魔郷" ? "th06" : exeName;

                        string saveFolder;
                        if (code.CompareTo("th125") < 0)
                        {
                            saveFolder = GetSaveFolder(exeName, shortcut.StringData.WorkingDir);
                        }
                        else
                        {
                            // このメソッドは繰り返し呼び出されるので、
                            // Path.Combine(applicationData, "ShanghaiAlice") をキャッシュする余地がある。
                            // ただし、必要無いのに初期化されないようにしなくてはならない。
                            var applicationData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                            saveFolder = Path.Combine(applicationData, "ShanghaiAlice", exeName);
                        }

                        yield return new ShortcutGameInfo(code, gameName, shortcutPath, saveFolder);
                    }
                }
            }
        }

        private static IEnumerable<GameInfo> SearchRegistry(RegistryKey rootKey, string uninstallKey)
        {
            using (var key = rootKey.OpenSubKey(uninstallKey))
            {
                if (key == null)
                {
                    yield break;
                }

                foreach (var name in key.GetSubKeyNames())
                {
                    using (var subKey = key.OpenSubKey(name))
                    {
                        // インストール時にインストール先を変えた場合でも、Inno Setup: Icon Group の値は “上海アリス幻樂団\東方○○○” のようになり、
                        // 必ず “上海アリス幻樂団” が含まれる。
                        var iconGroup = subKey.GetValue("Inno Setup: Icon Group")?.ToString() ?? string.Empty;
                        if (!iconGroup.Contains("上海アリス幻樂団"))
                        {
                            continue;
                        }

                        var workingDirectory = subKey.GetValue("InstallLocation")?.ToString() ?? string.Empty;
                        var exeCandidates = Directory.EnumerateFiles(workingDirectory, "th???.exe");
                        if (!exeCandidates.Any())
                        {
                            continue;
                        }

                        var startPath = exeCandidates.First();
                        var exeName = Path.GetFileNameWithoutExtension(startPath);
                        var code = exeName == "東方紅魔郷" ? "th06" : exeName;

                        string saveFolder;
                        if (code.CompareTo("th125") < 0)
                        {
                            saveFolder = GetSaveFolder(exeName, workingDirectory);
                        }
                        else
                        {
                            // このメソッドは繰り返し呼び出されるので、
                            // Path.Combine(applicationData, "ShanghaiAlice") をキャッシュする余地がある。
                            // ただし、必要無いのに初期化されないようにしなくてはならない。
                            var applicationData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                            saveFolder = Path.Combine(applicationData, "ShanghaiAlice", exeName);
                        }

                        yield return new ExecutableGameInfo(
                            code,
                            subKey.GetValue("DisplayName")?.ToString() ?? string.Empty,
                            startPath,
                            saveFolder,
                            workingDirectory);
                    }
                }
            }
        }

        private static string GetSaveFolder(string exeName, string workingDirectory)
        {
            var localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var virtualStore = new DirectoryInfo(Path.Combine(localApplicationData, "VirtualStore"));
            if (virtualStore.Exists)
            {
                var parentName = Path.GetFileName(workingDirectory);
                var parents = virtualStore.EnumerateDirectories(parentName, SearchOption.AllDirectories);

                // 本当にゲームのバーチャルストアかどうか、コンフィグファイルの有無で確かめる。
                // コンフィグファイルは一度でもゲームを起動すると作成される。
                // スコアファイルは、プレイしないと作成されない。
                var scoreFile = $"{exeName}.cfg";
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
