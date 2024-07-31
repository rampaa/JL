A file named `Regex_Replacements.txt` (which will work for all profiles) or `{ProfileName}_Regex_Replacements.txt` (e.g., `Default_Regex_Replacements.txt` will only work for the profile named `Default`) should be created in the `..\JL\Profiles` folder before JL is started.

The file format is similar to `SavedRegexReplacements.txt` used by the `Regex Replacer` extension of [Textractor](https://github.com/Artikash/Textractor). The only notable difference is that only `i`, `m`, `n`, `s`, and `x` modifiers are recognized by JL. For more information, see: https://learn.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-options.

All occurrences of a RegEx pattern are replaced by the RegEx replacement functionality; thus, the `g` modifier is not used. Replacements are applied in the order in which they appear in the aforementioned file.

Example RegEx replacement pattern: `|REGEX|^.*?"(.*?)".*?|BECOMES|$1|MODIFIER|mx|END|`
