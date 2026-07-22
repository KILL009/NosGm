# NosGM.ClientEnhancements attribution notice

`NosGM.ClientEnhancements` is an optional client-side research and compatibility component. It is not part of the NosGM login, master or world-server runtime.

## Upstream source

- Repository: `ImNotAVirus/NostaleWidget`
- Reviewed commit: `fc1b6dda5d797efc24a053180d30702f8dad162a`
- Copyright: `Copyright (c) 2022 ApourtArtt`
- License: MIT

The reviewed repository also references `DitzProject/ClientModdingAPI` and `ApourtArtt/DelphiClassInfo`. Exact immutable revisions for those earlier references were not recorded in the reviewed snapshot, so NosGM preserves the names without inventing commit identifiers.

## NosGM adaptation

NosGM contributors created a conservative foundation that:

- builds only as x86 on Windows;
- does not include an injector or automatic loader;
- performs no work from `DllMain` beyond disabling thread notifications;
- requires an explicit initialization call;
- verifies the host file version, architecture and SHA-256 before activation;
- provides a separate client probe that creates an exact local profile;
- rejects blank, malformed or unknown profile values;
- includes a wildcard pattern parser with unit tests;
- includes memory-range and executable-page validation;
- includes reversible patch transactions that verify original bytes before writing;
- restores applied patch bytes during transaction destruction;
- includes no packet send, packet receive, automation, movement, combat or farming function;
- contains no client executable, proprietary capture, memory dump or game asset;
- keeps Discord, cooldown, FPS, resolution and minimized-rendering features disabled until an exact client profile and regression evidence are available.

## License

This component is distributed under the MIT License preserved in `LICENSE`. Copyright in upstream work remains with ApourtArtt and other respective contributors. Copyright in later NosGM modifications remains with their respective authors.
