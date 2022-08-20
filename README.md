# 補足 / Supplement

## 日本語版のダウンロードはこちらです: [Windows]()

## Japanese:

このプログラムは[元のレポジトリ](https://git.cartooncraft.fr/shARPII/script-chan/)の内容を少し変更を加えたバージョンになります。最新のバージョンではないため新しいバージョンが欲しい場合は[元のレポジトリ](https://git.cartooncraft.fr/shARPII/script-chan/)を参照してください。

### 変更内容

* 日本語化

`Resources.ja-JP(.Designer.cs|.resx)`の作成と`script-chan2\GUI\Settings\SettingsViewModel.cs:56`に`ja-JP`の項目を追加

* ゲームスタートの時間を変更

`script-chan2\GUI\Match\MatchViewModel.cs:1169`の時間を5秒から10秒に変更

## English:

This program is a slightly modified version of the [original repository](https://git.cartooncraft.fr/shARPII/script-chan/). If you want a newer version, please refer to the [original repository](https://git.cartooncraft.fr/shARPII/script-chan/).

### Code Changes

* Add Japanese localization

Create `Resources.ja-JP(.Designer.cs|.resx)` and Added `ja-JP` at `script-chan2\GUI\Settings\SettingsViewModel.cs:56`

* Change start game time

Change duration from 5 seconds to 10 seconds at `script-chan2\GUI\Match\MatchViewModel.cs:1169`

---

# Introduction

Script chan is the tool used by world cup organizers since 2015.
You can use it to manage and create your osu! multiplayer rooms.


# Useful Links

User documentation: https://git.cartooncraft.fr/shARPII/script-chan/wikis/About-us/Home

Forum link: https://osu.ppy.sh/community/forums/topics/730734

Discord link: http://discord.gg/QGv44Sc

# Disclaimer

The osu! staff tolerates running bots on your own osu! account, however you are not allowed to create a dedicated osu! account to your bot without acknowledging the staff. Any attempt to do so will be considered multi-accounting, which is against the osu! Terms of Use.
Normal accounts have undisclosed rate limits. Script-chan, by default, uses limits that are somewhat tested on user accounts but there's no guarantee provided. Using a smaller duration is at your own risk.


# Contributing

If you are willing to contribute to the project, you can join our discord server and discuss with us!
We already have some long term ideas but we will gladly appreciate new ideas to improve Script chan.

# License

It is licensed as GPL 3.0. The entire license text is available in the [LICENSE](https://git.cartooncraft.fr/shARPII/script-chan/blob/master/LICENSE) file, however I recommend you to take a look at this [short summary](https://choosealicense.com/licenses/gpl-3.0/) to get a better idea!
