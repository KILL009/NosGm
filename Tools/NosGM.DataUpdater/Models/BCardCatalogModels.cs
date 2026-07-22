/*
 * Derived from the design of noszanou/BCardGistUpdater at commit
 * 53153c990ae5b65a603d223eeda504df2a67d5fb.
 * Copyright (C) noszanou and BCardGistUpdater contributors.
 * Modifications Copyright (C) 2026 NosGM contributors.
 * SPDX-License-Identifier: GPL-3.0-only
 */

using System.Text.Json.Serialization;

namespace NosGM.DataUpdater.Models;

public sealed record BCardCatalogDocument(
    [property: JsonPropertyName("schemaVersion")] int SchemaVersion,
    [property: JsonPropertyName("language")] string Language,
    [property: JsonPropertyName("sourceSha256")] string SourceSha256,
    [property: JsonPropertyName("types")] IReadOnlyList<BCardTypeEntry> Types);

public sealed record BCardTypeEntry(
    [property: JsonPropertyName("type")] long Type,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("subtypes")] IReadOnlyList<BCardSubtypeEntry> Subtypes);

public sealed record BCardSubtypeEntry(
    [property: JsonPropertyName("subtype")] long Subtype,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string Description);

public sealed record GeneratedCatalogFile(
    string RepositoryPath,
    string Content,
    BCardCatalogDocument Catalog);

public sealed record CatalogGenerationResult(
    string SourceFile,
    string SourceSha256,
    IReadOnlyList<GeneratedCatalogFile> Files,
    IReadOnlyList<string> UnsupportedLanguages);
