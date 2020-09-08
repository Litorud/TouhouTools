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
                var dialog = new CommonOpenFileDialog("リプレイファイルを選択してください。")
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
                var gameName = string.Equals(code, "th06", StringComparison.OrdinalIgnoreCase) ? "東方紅魔郷"
                    : string.Equals(code, "th07", StringComparison.OrdinalIgnoreCase) ? "東方妖々夢"
                    : string.Equals(code, "th08", StringComparison.OrdinalIgnoreCase) ? "東方永夜抄"
                    : string.Equals(code, "th09", StringComparison.OrdinalIgnoreCase) ? "東方花映塚"
                    : string.Equals(code, "th095", StringComparison.OrdinalIgnoreCase) ? "東方文花帖"
                    : string.Equals(code, "th10", StringComparison.OrdinalIgnoreCase) ? "東方風神録"
                    : string.Equals(code, "th11", StringComparison.OrdinalIgnoreCase) ? "東方地霊殿"
                    : string.Equals(code, "th12", StringComparison.OrdinalIgnoreCase) ? "東方星蓮船"
                    : string.Equals(code, "th125", StringComparison.OrdinalIgnoreCase) ? "ダブルスポイラー"
                    : string.Equals(code, "th128", StringComparison.OrdinalIgnoreCase) ? "妖精大戦争"
                    : string.Equals(code, "th13", StringComparison.OrdinalIgnoreCase) ? "東方神霊廟"
                    : string.Equals(code, "th14", StringComparison.OrdinalIgnoreCase) ? "東方輝針城"
                    : string.Equals(code, "th143", StringComparison.OrdinalIgnoreCase) ? "弾幕アマノジャク"
                    : string.Equals(code, "th15", StringComparison.OrdinalIgnoreCase) ? "東方紺珠伝"
                    : string.Equals(code, "th16", StringComparison.OrdinalIgnoreCase) ? "東方天空璋"
                    : string.Equals(code, "th165", StringComparison.OrdinalIgnoreCase) ? "秘封ナイトメアダイアリー"
                    : string.Equals(code, "th17", StringComparison.OrdinalIgnoreCase) ? "東方鬼形獣"
                    : $"{code.ToLower()}.exe";

                MessageBox.Show(
                    $@"“{fileName}” に対応するゲームを見つけられませんでした。
次に表示するダイアログで、「{gameName}」の場所を選択してください。",
                    "Replay",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                var message = $"「{gameName}」の場所を選択してください。";
                var dialog = new CommonOpenFileDialog(message)
                {
                    IsFolderPicker = true,
                    EnsureFileExists = true
                };
                dialog.Controls.Add(new CommonFileDialogLabel(message));

                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    MessageBox.Show(dialog.FileName);
                }
                else
                {
                    return;
                }

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
