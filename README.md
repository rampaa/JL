# JL
JL is a program for looking up Japanese words and expressions. Inspired by [Nazeka](https://github.com/wareya/nazeka) and [Chiitrans Lite](https://github.com/alexbft/chiitrans).

Download from the [releases page](https://github.com/rampaa/JL/releases). Prefer the x86 version for 50-80% less memory usage.

IMPORTANT: If you are using Windows 7 and you intend to use EPWING dictionaries, you MUST use the x86 version because of a .NET bug. See the [link](https://github.com/dotnet/runtime/issues/66272) for more details.


## Screenshots

<img src="https://user-images.githubusercontent.com/25622653/155894148-31bef5b8-d290-4e00-b12a-3294c2a708b8.png" width=75% height=75%>
<img src="https://user-images.githubusercontent.com/25622653/155894231-a14b9893-2a01-4c9a-9ff4-b45d993617c4.png" width=29% height=29%>

## System requirements
* Windows 7 or later
* .NET Desktop Runtime 6.0 or later

## Features
* Highly customizable
* Custom word and name dictionaries
* Anki mining
* Pass-through mode 
* Remembers last window position
* Recursive lookups
* Halfwidth -> Fullwidth conversions (and vice-versa)
* Hiragana -> Katakana conversions (and vice-versa)
* Chouonpu conversions

## Supported dictionaries

### EDICT

* JMdict
* JMnedict
* KANJIDIC (w/ composition data)

### EPWING

#### [Yomichan Import](https://github.com/FooSoft/yomichan-import/) format

* Daijirin
* Kenkyuusha
* Daijisen
* Gakken
* Kotowaza
* Koujien
* Meikyou

#### [Nazeka EPWING Converter](https://github.com/wareya/nazeka_epwing_converter) format
* Daijirin 
* Kenkyuusha
* Shinmeikai

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
### How do I add EPWING dictionaries?
Select the folder containing the **unzipped** (so you should have a folder of files named like term_bank_1.json, term_bank_2.json...)  contents of a dictionary converted with [yomichan-import](https://github.com/FooSoft/yomichan-import/), on the Manage Dictionaries window.
### Where are my settings stored?
* Anki settings: Config/AnkiConfig.json
* Dictionary settings: Config/dicts.json
* Everything else: JL.dll.config
### Will you add machine translation capabilities?
No.
## License
Licensed under the [Apache License, Version 2.0](https://www.apache.org/licenses/LICENSE-2.0)
