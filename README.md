# TouhouTools/Replay

TouhouTools/Replay は、東方Project のリプレイを、少し簡単に開始するためのプログラムです。
TouhouTools/Replay を使うと、リプレイファイル（拡張子が `.rpy` のファイル）を、自分で `replay` フォルダーへコピーしなくても、自動的にコピーし、さらにゲームを起動することができます。

# ダウンロード

[Replay.zip (Ver. 0.1)](https://github.com/Litorud/TouhouTools/releases/download/v0.1/Replay.zip)

# 使い方

## コマンドラインで実行

`Replay.exe` の引数に、リプレイファイルを指定してください。
```
> .\Replay.exe th17_01.rpy
```
これでゲームが起動します。

引数を指定しなかった場合は、ファイル選択ダイアログが開きます。
このダイアログでリプレイファイルを選択してください。

## エクスプローラーで実行

`Replay.exe` に、リプレイファイルをドラッグ&ドロップしてください。
これでゲームが起動します。

リプレイファイルをドラッグ&ドロップせず、直接 `Replay.exe` を起動した場合は、ファイル選択ダイアログが開きます。
このダイアログでリプレイファイルを選択してください。

## 実行がブロックされる

現時点では、実行すると、Windows Defender SmartScreen にブロックされます。
これは Windows の機能で、Windows が信用していないプログラムを自動的にブロックしています。
実行するには、「詳細」をクリックしてから「実行」をクリックします。

## リプレイの開始

起動したゲームのリプレイ一覧に “Temp” という名前のリプレイがあるはずです。
これを選べば、目的のリプレイを開始できます。

### 同名のファイルが存在する場合

リプレイファイルは、起動したゲームの `replay` フォルダーに、`th??_udTemp.rpy` という名前でコピーします。
もし、同名のファイルがすでに `replay` フォルダーに存在していたら、“th??_udTem**1**.rpy” という名前でコピーします。
それも存在していたら、“thxx_udTem**2**.rpy”, “thxx_udTem**3**.rpy”, ……のように、コピーできるファイル名を探します。

### 東方風神録の場合

東方風神録は、`th??_udTemp.rpy` という名前のリプレイファイルを読み込まないので、代わりに “th10_**25**.rpy” という名前でコピーします。
同名のファイルが存在する場合は、“th10_**24**.rpy”, “th10_**23**.rpy”, ……のように、コピーできるファイル名を探します。

### 【注意】

- `th??_udTemp.rpy`（東方風神録の場合は `th10_25.rpy`）は、ゲームを起動している間だけの一時的なファイルです。ゲームを終了したとき、この一時的なファイルは消滅します。必ず、元のファイルを残しておいてください。

## アンインストール

`Replay.exe` を含むフォルダーを削除してください。
もし、ファイルを `Replay.exe` に関連付けした場合は、手動で解除してください。現在の Windows は、関連付けの解除には若干複雑な手順を踏む必要があります。
