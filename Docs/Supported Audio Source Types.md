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

### 4) Windows Text to Speech

Example:  
`Microsoft Haruka`
