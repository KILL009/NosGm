# NosGM localization

NosGM resolves dynamic server messages per account. The neutral World Server resource is English, and culture-specific satellite resources override the keys they translate. Missing entries safely fall back to English.

## Supported server cultures

| Language | Code | Accepted aliases |
| --- | --- | --- |
| English | `en` | `en-US`, `en-GB`, `english`, legacy `uk`/`gb` |
| Spanish | `es` | `es-ES`, `spanish`, `español`, `espanol` |
| German | `de` | `de-DE`, `german`, `deutsch` |
| French | `fr` | `fr-FR`, `french`, `français`, `francais` |
| Italian | `it` | `it-IT`, `italian`, `italiano` |
| Polish | `pl` | `pl-PL`, `polish`, `polski` |
| Czech | `cs` | `cs-CZ`, legacy `cz`, `czech`, `čeština` |
| Russian | `ru` | `ru-RU`, `russian`, `русский` |
| Japanese | `ja` | `ja-JP`, legacy `jp`, `japanese`, `日本語` |
| Chinese (Simplified) | `zh` | `zh-CN`, `zh-Hans`, legacy `cn`, `chinese`, `中文` |

## Supported client profiles

The Login listening port is the source of truth for the installed client language. The launcher must connect the client to the matching Login port.

| Protocol prefix | RegionType | Login port | NSlangData suffix | Server culture |
| --- | ---: | ---: | --- | --- |
| `EN` | `0` | `4000` | `UK` | `en` |
| `DE` | `1` | `4001` | `DE` | `de` |
| `FR` | `2` | `4002` | `FR` | `fr` |
| `IT` | `3` | `4003` | `IT` | `it` |
| `PL` | `4` | `4004` | `PL` | `pl` |
| `ES` | `5` | `4005` | `ES` | `es` |
| `CZ` | `6` | `4006` | `CZ` | `cs` |
| `RU` | `7` | `4007` | `RU` | `ru` |
| `JP` | `8` | `4008` | `JP` | `ja` |
| `CN` | `9` | `4009` | `CN` | `zh` |

`NosGm.Login` opens all ten regional ports by default. Passing `--port 4005`, for example, starts only the Spanish listener.

The accepted local port determines the trusted `RegionType` written into `NsTeST`. The `RegionType` byte inside `NoS0575` remains compatibility data and cannot override the listening port. Modern `NoS0576` and `NoS0577` packets must also report the country matching that trusted port. A successful login synchronizes `Account.Language`.

## NsTeST account identity

NosGM accepts an exact database account name or the matching regional alias, such as `ES_account`. Prefix stripping is allowed only when the prefix matches the trusted Login port. It never replaces password, ticket or Master session validation.

## Login ports and World channel ports are different

World endpoint ports are independent from Login language ports. A World endpoint using TCP port `4006` does not automatically mean Czech. Language comes from the trusted Login region; World channels keep their independently configured ports such as `1337`, `1338` and `1339`.

## Player selection

The selected culture is stored in `Account.Language` and loaded on every World connection. Players and administrators can use:

```text
$Language en
$Language es
$Language de
$Language fr
$Language it
$Language pl
$Language cs
$Language ru
$Language ja
$Language zh
```

## Server and client responsibilities

NosGM translates emulator-generated messages. Most static game content remains in the installed client files:

- `NSlangData_XX.NOS` contains item, NPC, monster, quest, skill, map and dialogue tables.
- `conststring_XX.dat` contains interface labels and other client constants.

To display the complete game in a language, use the authorized matching client data and regional Login port.

## Developer usage

For one player, prefer:

```csharp
Session.GetMessageFromKey("ITEM_ACQUIRED")
```

Use `Language.Instance.GetMessageFromKey(key)` for server logs or messages without a recipient. Resolve broadcasts separately for each receiving session.
