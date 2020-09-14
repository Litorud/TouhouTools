using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.WindowsAPICodePack.Dialogs.Controls;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using TouhouTools;

namespace Replay
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            string rpy;
            if (args.Length == 0)
            {
                var dialog = new CommonOpenFileDialog("リプレイファイルを選択してください")
                {
                    //Multiselect = true,
                    EnsureFileExists = true
                };

                var rpyFilter = new CommonFileDialogFilter()
                {
                    DisplayName = "リプレイファイル"
                };
                rpyFilter.Extensions.Add("rpy");
                dialog.Filters.Add(rpyFilter);

                var allFilter = new CommonFileDialogFilter()
                {
                    DisplayName = "すべてのファイル"
                };
                allFilter.Extensions.Add("*");
                dialog.Filters.Add(allFilter);

                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    rpy = dialog.FileName;
                }
                else
                {
                    return;
                }
            }
            else
            {
                rpy = args[0];

                if (!File.Exists(rpy))
                {
                    MessageBox.Show("指定したファイルは存在しません。", "Replay", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            var fileName = Path.GetFileName(rpy);
            var prefix = GetPrefix(fileName);
            var code = GetCode(prefix);
            var gameInfo = TouhouTools.Program.SearchGame(code);
            if (gameInfo == null)
            {
                var exeName = string.Equals(code, "th06", StringComparison.OrdinalIgnoreCase) ? "東方紅魔郷.exe" : $"{code.ToLower()}.exe";

                MessageBox.Show(
                    $@"“{fileName}” に対応するゲームを見つけられませんでした。
次に表示するダイアログで、“{exeName}” の場所を選択してください。",
                    "Replay",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                var dialog = new CommonOpenFileDialog($"“{exeName}” の場所を選択してください")
                {
                    IsFolderPicker = true,
                    EnsureFileExists = true
                };
                dialog.Controls.Add(new CommonFileDialogLabel($"“{exeName}” の場所"));

                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    var gameDirectory = dialog.FileName;
                    // 実行ファイルのパスを取得する。
                    // 指定されたディレクトリから th???.exe を探せば、リプレイファイル名が形式通りでなくてもコピーおよび起動ができると思ったが、
                    // 東方紅魔郷.exe への特殊対応が必要になるし、EXE 名からリプレイファイル接頭辞を引く処理も必要になるため、現時点では対応しない。
                    var startPath = Path.Combine(gameDirectory, exeName);
                    if (!File.Exists(startPath))
                    {
                        MessageBox.Show("すみません。このゲームには対応していません。", "Replay", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var saveFolder = string.Compare(code, "th125", true) < 0
                        ? TouhouTools.Program.VirtualStoreHolder.GetSaveFolder(exeName, gameDirectory)
                        : TouhouTools.Program.ShanghaiAliceHolder.GetSaveFolder(exeName);

                    gameInfo = new ExecutableGameInfo(
                        code,
                        Path.GetFileName(gameDirectory),
                        startPath,
                        saveFolder,
                        gameDirectory);
                }
                else
                {
                    return;
                }
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
                MessageBox.Show("同名のファイルが存在するため、コピーできません。", "Replay", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private static string GetPrefix(string fileName)
        {
            var underscoreIndex = fileName.IndexOf('_');
            return fileName.AsSpan()[..underscoreIndex].ToString();
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
