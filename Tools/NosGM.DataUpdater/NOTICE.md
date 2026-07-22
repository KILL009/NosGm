# NosGM.DataUpdater attribution notice

`NosGM.DataUpdater` is an external maintenance tool for the NosGM project. It is not part of the NosGM server runtime.

## Upstream project

This tool is derived from and substantially inspired by:

- Project: `noszanou/BCardGistUpdater`
- Upstream commit: `53153c990ae5b65a603d223eeda504df2a67d5fb`
- Original author and maintainer: `noszanou`
- Upstream contributors: contributors recorded in the Git history of `noszanou/BCardGistUpdater`
- Upstream license: GNU General Public License version 3

The BCard parsing flow, NosTale resource-fetching setup and GitHub update concept were adapted from that project.

## NosGM modifications

NosGM contributors added or changed:

- configurable repository, branch, paths and language selection;
- ten-language generation targets;
- dry-run and local-resource modes;
- source hashing and stable manifests;
- catalog comparison and Markdown change reports;
- no-change detection before branch creation;
- one GitHub write per changed file;
- pull requests against the configured NosGM repository;
- separation from the NosGM server Core;
- documentation, workflow safety and credential handling.

Copyright in the original work remains with `noszanou` and the original BCardGistUpdater contributors. Copyright in later modifications remains with the respective NosGM contributors.

## License

This tool and its adapted files are distributed under `GPL-3.0-only`. The repository copy of the GNU GPL version 3 text is available under `LICENSES/GPL-3.0-only/`.

The generated factual catalog data may also be affected by rights in the source game data. This tool does not grant permission to redistribute proprietary NosTale client files or assets.
