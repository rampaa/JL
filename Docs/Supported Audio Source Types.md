### 1) Local files through `Local Path` type

Example:
`C:\Users\User\Desktop\jpod_files\{Reading} - {Term}.mp3`

- **Tip**: If you wish to use [local-audio-yomichan](https://github.com/yomidevs/local-audio-yomichan) without needing the [Local Audio Server for Yomichan](https://ankiweb.net/shared/info/1045800357) add-on or even Anki itself, check out the [Yomichan Audio Collection Renamer for JL](https://github.com/rampaa/YomichanAudioCollectionRenamerForJL).

---

### 2) URLs returning an audio directly through `URL` type

Example:
`http://assets.languagepod101.com/dictionary/japanese/audiomp3.php?kanji={Term}&kana={Reading}`

---

### 3) URLs returning a JSON response in Custom Audio List format through `URL (JSON)` type

Example:
`http://127.0.0.1:5050/?sources=jpod,jpod_alternate,nhk16,forvo&term={Term}&reading={Reading}`

---

### 4) Windows OneCore and SAPI-compatible voices via the `Text to Speech` type

All SAPI-compatible voices are supported, along with built-in Windows OneCore voices. Examples include:

- Built-in Windows SAPI voices (e.g., Microsoft Haruka Desktop)
- Built-in Windows OneCore voices (e.g., Microsoft Ayumi)
- [VOICEVOX](https://voicevox.hiroshiba.jp/) (requires a SAPI adapter such as [SAPIForVOICEVOX](https://github.com/shigobu/SAPIForVOICEVOX))
- Microsoft Natural Voices (requires a SAPI adapter such as [NaturalVoiceSAPIAdapter](https://github.com/gexgd0419/NaturalVoiceSAPIAdapter))
