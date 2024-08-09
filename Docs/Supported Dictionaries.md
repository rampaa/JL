# Supported Dictionaries

* JMdict (displayed info can be customized through Manage Dictionaries->JMdict->Edit->Options)
* JMnedict
* KANJIDIC2 (with de/composition data provided by [ids.txt](https://github.com/cjkvi/cjkvi-ids/blob/master/ids.txt))

* [Yomichan Import](https://github.com/FooSoft/yomichan-import/) format
  * Word dictionaries through `Word Dictionary` type
  * Kanji dictionaries through `Kanji Dictionary` type (should include a file like `kanji_bank_1.json`)
  * Kanji dictionaries with word schema through `Kanji Dictionary with Word Schema` type
  * Name dictionaries through `Name Dictionary` type
  * Pitch accent dictionaries through `Pitch Accent Dictionary` type
  * Other dictionaries (such as grammar dictionaries) through `Nonspecific Dictionary` type
  * Note: JL *can* import Yomichan dictionaries with structured content but it will strip the non-content parts (e.g. HTML tags) so whether the displayed result will be satisfactory depends on the dictionary. It's recommended to avoid using dictionaries like `JMdict for Yomichan` and `Jitendex` that essentially have the same data as JMdict. Since JL already supports the JMdict itself, using these dictionaries will only result in an inferior user experience on JL.

* [Nazeka EPWING Converter](https://github.com/wareya/nazeka_epwing_converter) format
  * Word dictionaries (Daijirin, Shinmeikai, Kenkyuusha etc.) through `Word Dictionary` type
  * Name dictionaries through `Name Dictionary` type
  * Kanji dictionaries through `Kanji Dictionary` type
  * Other dictionaries through `Nonspecific Dictionary` type


# Supported Frequency Dictionaries
* Yomichan Import format
  * Word frequency dictionaries through `Yomichan` type
  * Kanji frequency dictionaries through `Yomichan (Kanji)` type

* Nazeka format
  * Word frequency dictionaries through `Nazeka` type
