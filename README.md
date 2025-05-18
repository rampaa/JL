# JL
JL is a program for looking up Japanese words and expressions. Inspired by [Nazeka](https://github.com/wareya/nazeka) and [Chiitrans Lite](https://github.com/alexbft/chiitrans).

Download from the [releases page](https://github.com/rampaa/JL/releases).

## Screenshots
<img src="https://github.com/user-attachments/assets/8787b37f-1b9f-4f79-ba41-ea03dd1160cf">

<p float="left">
  <img src="https://github.com/user-attachments/assets/d731bf68-084a-46de-886e-8c0a217b3e7f" width="42%" height="42%" />
  <img src="https://github.com/user-attachments/assets/9a7c90d8-68ee-4b98-bc66-8e4b5c5581ad" width="29%" height="29%" /> 
  <img src="https://github.com/user-attachments/assets/72abcc15-0187-4e27-a592-aa84b2249c44" width="19%" height="19%" />
</p>

<p float="left">
  <img src="https://github.com/user-attachments/assets/ee201845-ce83-4183-96cb-ed715ea78061" width="30%" height="30%" />
  <img src="https://github.com/user-attachments/assets/cf37d4a2-d617-4274-b71a-1461f029306d" width="29%" height="29%" /> 
  <img src="https://github.com/user-attachments/assets/7f70e11a-ebd9-46c4-a953-f041af2cde7c" width="34%" height="34%" />
</p>

## System requirements
* .NET Desktop Runtime 9.0 or later

## Features
* Highly customizable (i.e. You can change the size/color/opacity/hotkey of pretty much everything)
* Custom word and name dictionaries
* Supports lots of dictionaries (see the [Supported Dictionaries](https://github.com/rampaa/JL/blob/master/Docs/Supported%20Dictionaries.md) page for details)
* Pitch accent (needs a pitch accent dictionary such as [Kanjium](https://foosoft.net/projects/yomichan/#dictionaries))
* Allows customizing the displayed info per dictionary (not showing PoS info for JMdict, choosing whether to put newlines between definitions etc. through "Manage Dictionaries"->"Edit" button of the dictionary->"Options" section)
* Can deconjugate verbs (see the [Deconjugation Support](https://github.com/rampaa/JL/blob/master/Docs/Deconjugation%20Support.md) page for the list of supported adjective and verb types)
* Anki mining (allows different configurations for word/kanji/name mining)
* Support for multiple audio sources (see the [Supported Audio Source Types](https://github.com/rampaa/JL/blob/master/Docs/Supported%20Audio%20Source%20Types.md) page for more details)
* Allows different opacity level on un/hover
* Pass-through mode (i.e. mouse clicks will pass through JL)
* Recursive lookups (i.e. popup within popup)
* Remembers last window position
* Halfwidth -> Fullwidth conversions (and vice-versa)
* Hiragana -> Katakana conversions (and vice-versa)
* Chouonpu conversions (e.g. can find 清掃 from セーソー)
* Text normalization (e.g. can find 株式会社 when ㍿ is looked up)
* Has a backlog
* Touch screen support (i.e. Left Click/Touch lookup mode)
* Can work without stealing the focus away from other windows
* Can keep itself as the topmost window (i.e. JL can be used with programs like [Magpie](https://github.com/Blinue/Magpie))
* Stats (read character/line count, read characters per minute, number of times a specific term was looked up etc.)

## FAQ
### How does it work?
JL grabs the text from the clipboard by default. It can also be configured to capture it from a WebSocket.

You need another program to copy the text to the clipboard and/or to a WebSocket (e.g. [Textractor](https://github.com/Artikash/Textractor) for visual novels). 
### Why can't I look anything up?
Make sure you didn't disable lookups with the "Toggle lookup" hotkey. Also make sure that you have active dictionaries under Manage Dictionaries window.
### Why can't I scroll down the results list?
You need to be in mining mode in order to interact with the popup window. "Left click/Touch" and "Text select" lookup modes will automatically activate the mining mode. If you are using the "Mouse move" lookup mode, you can activate the mining mode with a middle mouse click or with the mining mode hotkey (by default it's "Alt+M").
### How do I disable pass-through mode?
Press the opacity slider button located top-left of the main window. You can also disable it with the same hotkey you've enabled it if you have the global hotkeys enabled and if its hotkey is a valid global hotkey. (Only hotkeys with modifiers (e.g. Alt+T), function keys (except for F12) and numpad keys can be used as global hotkeys.)
### How can I use JL with Magpie?
Enable the "Preferences>Main Window>Always on top" option. This option will make sure that JL is the topmost window on every clipboard change.

Optional: Grant UIAccess privilege to JL. This allows JL to be on top of Magpie even before any clipboard change occurs. See [Granting UIAccess privilege to JL](https://github.com/rampaa/JL/blob/master/Docs/Granting%20UIAccess%20Privilege%20to%20JL.md) page for more details.

If you use JL v2.0.0+ and Magpie v0.11.0+ then you don't necessarily need to change the following settings.

Disable the "Preferences>Main Window>Focus on hover" option.

Disable the "Preferences>Popup>Focus on lookup" option.

Optional if you use the "Mouse move" look up mode, required otherwise: Disable the "Preferences>General>Focusable" option. When this option is disabled, JL won't steal the focus away from other windows, even in case of a mouse click. This allows you to open popups within popups without Magpie exiting the fullscreen mode. Note that if this option is disabled, you won't be able to use hotkeys unless you enable the global hotkeys option and assign valid global hotkeys, because JL won't have the keyboard focus. You can also use the middle mouse button to activate the mining mode.

### How do I add new dictionaries?

#### [Yomichan Import]
Select the folder containing the **unzipped** (so you should have a folder of files named like term_bank_1.json, term_bank_2.json...)  contents of a dictionary that was converted with [Yomichan Import](https://github.com/FooSoft/yomichan-import/), on the Manage Dictionaries window.
#### [Nazeka EPWING Converter]
Select the file you got from [Nazeka EPWING Converter](https://github.com/wareya/nazeka_epwing_converter), on the Manage Dictionaries window.

### Where are my settings stored?
* Anki settings: Config/AnkiConfig.json
* Audio source settings: Config/AudioSourceConfig.json
* Dictionary settings: Config/dicts.json
* Frequency settings: Config/freqs.json
* Everything else: Config/Configs.sqlite
### Will you add machine translation capabilities?
No.

## Credits
* [Nazeka](https://github.com/wareya/nazeka): Deconjugation rules, deconjugator, frequency lists
* [JMdict](https://www.edrdg.org/wiki/index.php/JMdict-EDICT_Dictionary_Project): JMdict_e.gz
* [JMnedict](https://www.edrdg.org/enamdict/enamdict_doc.html): JMnedict.xml.gz
* [KANJIDIC](https://www.edrdg.org/wiki/index.php/KANJIDIC_Project): kanjidic2.xml.gz
* [cjkvi-ids](https://github.com/cjkvi/cjkvi-ids): ids.txt

## License
Licensed under the [Apache License, Version 2.0](https://www.apache.org/licenses/LICENSE-2.0)
