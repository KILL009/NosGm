# NosGM localization

NosGM resolves dynamic server messages per account. The neutral World Server resource is English, and `LocalizedResources.fr.resx` overrides the translated French keys. Missing French entries safely fall back to English.

## Player selection

The selected culture is stored in `Account.Language` and is loaded with the account on every World Server connection.

Players can change it in game:

- `$Language fr` — French
- `$Language en` — English

Registration panels should save `fr` or `en` in `Account.Language`. The current login/world protocol does not carry a trustworthy client-language field, so `RegionType` must not be treated as a locale.

## Developer usage

For a message sent to one player, use the session-aware resolver:

```csharp
Session.GetMessageFromKey("ITEM_ACQUIRED")
```

Use `Language.Instance.GetMessageFromKey(key)` only for server logs or messages that do not have a recipient.

Packet handlers automatically establish the current session culture, so legacy calls made directly inside a handler are also resolved for that player. New code should still prefer `Session.GetMessageFromKey` because it remains explicit and works in callbacks.

For broadcasts, resolve the message separately for each receiving session. A single pre-rendered broadcast cannot display different languages to different players.

## Adding a language

1. Add the culture code to `Language.SupportedCultures`.
2. Add `Resource/LocalizedResources.<culture>.resx` to the World Server project.
3. Preserve every format token such as `{0}`, `{1}` and protocol separator.
4. Leave untranslated keys out of the satellite resource so the neutral English resource is used.
5. Replace hard-coded player-facing strings with resource keys and the session-aware resolver.

The initial French catalog was adapted from the archived OpenNos French resources. Only keys that still exist in NosGM and have compatible format tokens are included.
