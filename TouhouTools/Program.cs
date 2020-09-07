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
        static readonly string shanghaiAlice = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ShanghaiAlice");

        static void Main()
        {
            var games = SearchGames();

            foreach (var info in games)
            {
                Console.WriteLine($@"----------------------------------------
{info.Name}
----------------------------------------
識別コード　　　: {info.Code}
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
                .Concat(SearchRegistry(Registry.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"))
                .Concat(SearchProgramFiles(Environment.SpecialFolder.ProgramFilesX86))
                .Concat(SearchProgramFiles(Environment.SpecialFolder.ProgramFiles));
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

                        var saveFolder = code.CompareTo("th125") < 0
                            ? VirtualStoreHolder.GetSaveFolder(exeName, shortcut.StringData.WorkingDir)
                            : Path.Combine(shanghaiAlice, exeName);

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

                        var saveFolder = code.CompareTo("th125") < 0
                            ? VirtualStoreHolder.GetSaveFolder(exeName, workingDirectory)
                            : Path.Combine(shanghaiAlice, exeName);

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

        // Program Files を調べる。
        // 紅魔郷～文花帖は Uninstall キー配下にキーが作られないが、既定で Program Files 配下にインストールされるので、
        // Program Files 配下を調べることで、新たにゲームを見つけられる可能性がある。
        private static IEnumerable<GameInfo> SearchProgramFiles(Environment.SpecialFolder programFilesFolder)
        {
            var programFiles = Environment.GetFolderPath(programFilesFolder);

            // 東方紅魔郷は、既定のインストール先が上海アリス幻樂団配下ではなく、 Program Files 直下になっている。
            const string name = "東方紅魔郷";
            var th06ExePath = Path.Combine(programFiles, name, $"{name}.exe");
            if (File.Exists(th06ExePath))
            {
                var workingDirectory = Path.GetDirectoryName(th06ExePath)!;
                yield return new ExecutableGameInfo(
                    "th06",
                    name,
                    th06ExePath,
                    VirtualStoreHolder.GetSaveFolder(name, workingDirectory),
                    workingDirectory);
            }

            // Program Files の中から、“上海アリス幻樂団”というディレクトリを再帰的に探す。
            var directories = Directory.EnumerateDirectories(programFiles, "上海アリス幻樂団", SearchOption.AllDirectories);

            // SearchOption.AllDirectories の結果を列挙すると、UnauthorizedAccessException が発生することがある。
            // 参考: https://docs.microsoft.com/ja-jp/dotnet/standard/io/how-to-enumerate-directories-and-files
            // 例外を無視するために、foreach ではなく直接 Enumerator を扱う。
            var enumerator = directories.GetEnumerator();
            do
            {
                try
                {
                    if (!enumerator.MoveNext())
                    {
                        break;
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    continue;
                }

                var gameDirectories = Directory.EnumerateDirectories(enumerator.Current);

                foreach (var gameDirectory in gameDirectories)
                {
                    var exeCandidates = Directory.EnumerateFiles(gameDirectory, "th???.exe");
                    if (!exeCandidates.Any())
                    {
                        continue;
                    }

                    var startPath = exeCandidates.First();
                    var exeName = Path.GetFileNameWithoutExtension(startPath);
                    var code = exeName == "東方紅魔郷" ? "th06" : exeName;

                    var saveFolder = code.CompareTo("th125") < 0
                        ? VirtualStoreHolder.GetSaveFolder(exeName, gameDirectory)
                        : Path.Combine(shanghaiAlice, exeName);

                    yield return new ExecutableGameInfo(
                        code,
                        Path.GetFileName(gameDirectory),
                        startPath,
                        saveFolder,
                        gameDirectory);
                }
            } while (true);
        }

        public static GameInfo SearchGame(string code)
        {
            return SearchGames()
                .Where(g => g.Code.Equals(code, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();
        }

        // ダブルスポイラー以降は VirtualStore を調べる必要がないため、VirtualStore を取得する処理は無意味になる。
        // そこで、このクラスに隔離することで、参照されたときだけ処理を行うようにする。
        // このパターンは、shanghaiAlice フィールドに対しても適用する価値があるものの、
        // shanghaiAlice のほうは参照する確率がおそらく高いので、このような複雑なことはせず、直接フィールドに保持している。
        static class VirtualStoreHolder
        {
            static DirectoryInfo virtualStore;

            public static readonly Func<string, string, string> GetSaveFolder;

            static VirtualStoreHolder()
            {
                var localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                virtualStore = new DirectoryInfo(Path.Combine(localApplicationData, "VirtualStore"));

                if (virtualStore.Exists)
                {
                    GetSaveFolder = GetSaveFolderFromVirtualStore;
                }
                else
                {
                    GetSaveFolder = GetSaveFolderFromWorkingDirectory;
                }
            }

            static string GetSaveFolderFromVirtualStore(string exeName, string workingDirectory)
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

                return workingDirectory;
            }

            static string GetSaveFolderFromWorkingDirectory(string exeName, string workingDirectory) => workingDirectory;
        }
    }
}
