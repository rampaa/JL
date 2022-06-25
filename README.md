# JL
JL is a program for looking up Japanese words and expressions. Inspired by [Nazeka](https://github.com/wareya/nazeka) and [Chiitrans Lite](https://github.com/alexbft/chiitrans).

Download from the [releases page](https://github.com/rampaa/JL/releases). Prefer the x86 version for 50-80% less memory usage. Prefer the x64 version if you intend to use *lots of* dictionaries together ~~because x86 programs cannot use more than 2 GB of RAM~~. The x86 version of JL can now use more than 2 GB of RAM (up to 4 GB) because it's marked as [LARGEADDRESSAWARE](https://docs.microsoft.com/en-us/cpp/build/reference/largeaddressaware-handle-large-addresses).

IMPORTANT: If you are using Windows 7 and you intend to use EPWING dictionaries, you MUST use the x86 version because of a .NET bug. See the [link](https://github.com/dotnet/runtime/issues/66272) for more details.


## Screenshots
<img src="https://user-images.githubusercontent.com/25622653/169386347-c6c4d4ff-e071-4e5f-a08a-8bf480b22219.png">

<p float="left">
  <img src="https://user-images.githubusercontent.com/25622653/169386915-33e8441f-3c99-4479-afb0-a5d7f575a9bf.png" width="40%" height="40%" />
  <img src="https://user-images.githubusercontent.com/25622653/169388031-d430e9d7-155b-4a55-8159-3b928a16c231.png" width="20%" height="20%" /> 
  <img src="https://user-images.githubusercontent.com/25622653/169387074-674749ac-8908-4eed-a334-18ed5548d0d3.png" width="20%" height="20%" />
</p>

## System requirements
* Windows 7 or later
* .NET Desktop Runtime 6.0 or later

## Features
* Highly customizable
* Custom word and name dictionaries
* Anki mining
* Pass-through mode
* Invisible mode (see https://github.com/rampaa/JL/pull/7#issuecomment-1069236589)
* Allows different opacity level on un/hover
* Pitch accent (needs a pitch accent dictionary such as [Kanjium](https://foosoft.net/projects/yomichan/#dictionaries))
* Remembers last window position
* Recursive lookups
* Halfwidth -> Fullwidth conversions (and vice-versa)
* Hiragana -> Katakana conversions (and vice-versa)
* Chouonpu conversions
* Can keep itself as the topmost window (i.e. JL can be used with programs like [Magpie](https://github.com/Blinue/Magpie))

## Supported dictionaries

### EDICT

* JMdict
* JMnedict
* KANJIDIC (w/ composition data)

### EPWING

#### [Yomichan Import](https://github.com/FooSoft/yomichan-import/) format

* Daijirin
* Daijisen
* Gakken
* Gakken Yojijukugo
* Iwanami
* Jitsuyou
* Kanjigen
* Kenkyuusha
* Kirei Cake
* Kotowaza
* Koujien
* Meikyou
* Nikkoku
* Oubunsha (without images)
* Shinjirin
* Shinmeikai
* Shinmeikai Yojijukugo
* Weblio Kogo
* Zokugo
* Pitch accent dictionaries (e.g. [Kanjium](https://foosoft.net/projects/yomichan/#dictionaries)) through "Pitch Accent (Yomichan)" type
* Other dictionaries (such as grammar dictionaries) through "Nonspecific (Yomichan)" type


#### [Nazeka EPWING Converter](https://github.com/wareya/nazeka_epwing_converter) format
* Daijirin
* Kenkyuusha
* Shinmeikai


### Frequency Dictionaries

#### Yomichan Import format
* Anime & J-drama
* BCCWJ
* BCCWJ-LUW
* BCCWJ-SUW
* Innocent Ranked
* JPDB
* Kokugojiten
* Monodicts 206k
* Narou
* Netflix
* Nier
* Novels
* VN
* Wikipedia
* Youtube
* And probably all other frequency dictionaries that Yomichan currently supports

#### Nazeka format
* Narou
* Novels
* VN


## Credits
* [Nazeka](https://github.com/wareya/nazeka): Deconjugation rules, deconjugator, frequency lists
* [JMdict](https://www.edrdg.org/wiki/index.php/JMdict-EDICT_Dictionary_Project): JMdict_e.gz
* [JMnedict](https://www.edrdg.org/enamdict/enamdict_doc.html): JMnedict.xml.gz
* [KANJIDIC](https://www.edrdg.org/wiki/index.php/KANJIDIC_Project): kanjidic2.xml.gz
* [cjkvi-ids](https://github.com/cjkvi/cjkvi-ids): ids.txt

## FAQ
### Why can't I look anything up?
Make sure you're not in pass-through mode, kanji mode, have lookup-on-select-only enabled, or have disabled lookups.
### Why can't I scroll down the results list?
You need to be in mining mode in order to interact with the popup window.
### How do I disable pass-through mode?
Press the opacity slider button located top-left of the main window.
### How can I use JL with Magpie?
Enable the "Preferences>General>Always on top" option. This option will make sure that JL is the topmost window on every clipboard change.

Disable the "Preferences>Popup>Focus on lookup" option. When this option is disabled, JL can work without stealing the focus away from other windows.

### How do I add EPWING dictionaries?

#### [Yomichan Import]
Select the folder containing the **unzipped** (so you should have a folder of files named like term_bank_1.json, term_bank_2.json...)  contents of a dictionary converted with [Yomichan Import](https://github.com/FooSoft/yomichan-import/), on the Manage Dictionaries window.
#### [Nazeka EPWING Converter]
Select the file you got from [Nazeka EPWING Converter](https://github.com/wareya/nazeka_epwing_converter), on the Manage Dictionaries window.

### Where are my settings stored?
* Anki settings: Config/AnkiConfig.json
* Dictionary settings: Config/dicts.json
* Frequency settings: Config/freqs.json
* Stats: Config/Stats.json
* Everything else: JL.dll.config
### Will you add machine translation capabilities?
No.
## License
Licensed under the [Apache License, Version 2.0](https://www.apache.org/licenses/LICENSE-2.0)
