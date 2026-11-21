# Emberwake licensing and attribution

## Code contributions

[Commit `11a59be0c9`](https://github.com/BluCottonCandy/Emberwake/commit/11a59be0c932596486cdd9b9f18f562b52cc7b2d), which merged the structure fire fix, is the licensing cutoff. Code already present at that commit keeps the licenses granted for it. Historical releases retain their applicable terms.

By submitting original code to Emberwake after that cutoff, you license your contribution under the [GNU Affero General Public License, version 3 only](LICENSE-AGPLv3.txt) (`AGPL-3.0-only`). You retain your copyright. This default covers both new files and original changes to existing files.

When editing an inherited file, preserve the copyright and license notices for the existing code. When bringing in third-party code, identify its source and license, retain its notices, and check that its terms permit use in the combined project.

## Inherited code, including `_CP14` and `CP14`

In its [MIT licensing commit](https://github.com/crystallpunk-14/crystall-punk-14/commit/95b4b05574b32d887188b801be5d3f9aedee3c83), CrystallPunk released its content code under [MIT](LICENSE.TXT), removed `LICENSE_CE.TXT`, and removed the workflow that requested the CrystallEdge contributor agreement. This MIT release includes upstream-authored code in the `_CP14` and `CP14` directories. Specific third-party licenses and copyright notices still apply to the material they cover. Dependencies such as RobustToolbox carry their own licenses.

The [CrystallEdge contributor agreement](CLA.md) is retained as a historical upstream document. The contribution terms above apply to new Emberwake code.

## Running a modified server

Section 13 of the [AGPLv3](LICENSE-AGPLv3.txt) requires operators of a modified, AGPL-covered server to prominently offer players its corresponding source at no charge. A visible link to the source and build scripts for the exact deployed version can provide that access.

## Sprites, music, maps, and other assets

Asset licenses are independent of the code cutoff. Use the license and credits recorded with each asset (`meta.json`, `attributions.yml`, or a specific notice). The archived README describes most assets as CC BY-SA 3.0. Its [asset notice for `_CP14` and `CP14`](https://github.com/crystallpunk-14/crystall-punk-14/blob/5b6108377e40235c768be3ac6ffadb37a085f441/Legal.md#licensingattribution) specifies CC BY-SA 4.0 unless otherwise stated. Check the original source when an asset's terms are unclear.

## Third-party material

The following licenses are accepted for material sourced from other authors.

- The following Creative Commons 4.0 (or later) licenses: [CC-BY 4.0](https://creativecommons.org/licenses/by/4.0/legalcode.en), [CC-BY-SA 4.0](https://creativecommons.org/licenses/by-sa/4.0/legalcode.en), [CC-BY-ND](https://creativecommons.org/licenses/by-nd/4.0/legalcode.en), (ND licenses are discouraged, but accepted) or an earlier version with *permission from the author*.
- [The MIT License](https://opensource.org/license/MIT)
- Public domain content under [CC0](https://creativecommons.org/public-domain/cc0/), etc. NOTE: The absence of a copyright mark means **all rights reserved** *not* public domain, public domain must be explicitly marked/stated. Attribution is still required for public domain works.

### Asset attribution and sourcing

- All assets *must* be properly licensed, attributed, and include the original title of the work. This also requires a link to the original author, and license and if needed provides a way to contact the author.
- For audio files especially: **you must find the actual author not just a YouTube channel**. <u>If you cannot find the author/original license you cannot use the asset.</u> Failure to be able to provide sourcing information will result in your PR being closed and potentially barring from future contributions depending on the severity of the situation.
- License versioning is important, make sure to include the correct version of the license in your attribution. If a version is not specified by the author with a versioned license, that is not a valid license and cannot be used.
- Most Assets have their license and the copyright in the metadata file. [Example](https://github.com/space-wizards/space-station-14/blob/master/Resources/Textures/Objects/Tools/crowbar.rsi/meta.json).
- "No Derivatives" licenses should be avoided if possible (but are permitted) as they can prevent fixing bugs or making for accessibility reasons (Color blindness or epilepsy fixes, etc.).
