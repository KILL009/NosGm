#!/usr/bin/env python3
"""Validate NosGM RESX catalogs and optionally repair malformed schema comments."""

from __future__ import annotations

import argparse
import re
import sys
import xml.etree.ElementTree as ET
from collections import Counter
from pathlib import Path

RESOURCE_DIR = Path("Data/Frostvein.Program/Frostvein.World/Resource")
NEUTRAL_FILE = RESOURCE_DIR / "LocalizedResources.resx"
SATELLITE_PATTERN = "LocalizedResources.*.resx"
PLACEHOLDER_RE = re.compile(r"\{\d+(?:[^{}]*)\}")


def read_text(path: Path) -> str:
    return path.read_text(encoding="utf-8-sig")


def repair_unclosed_schema_comment(text: str) -> tuple[str, bool]:
    """Close the introductory RESX comment before the first real data element."""
    comment_start = text.find("<!--")
    first_data = text.find("<data ")
    if comment_start < 0 or first_data < 0:
        return text, False

    comment_end = text.find("-->", comment_start + 4)
    if comment_end >= 0 and comment_end < first_data:
        return text, False

    # first_data points at '<'; its leading indentation remains in the prefix.
    return text[:first_data] + "-->\n  " + text[first_data:], True


def parse_catalog(path: Path) -> dict[str, str]:
    try:
        root = ET.parse(path).getroot()
    except ET.ParseError as exc:
        raise ValueError(f"{path}: invalid XML: {exc}") from exc

    if root.tag != "root":
        raise ValueError(f"{path}: expected <root>, found <{root.tag}>")

    catalog: dict[str, str] = {}
    duplicates: list[str] = []
    for node in root.findall("data"):
        name = node.get("name")
        if not name:
            raise ValueError(f"{path}: <data> element without a name attribute")
        if name in catalog:
            duplicates.append(name)
        value_node = node.find("value")
        catalog[name] = "" if value_node is None or value_node.text is None else value_node.text

    if duplicates:
        raise ValueError(f"{path}: duplicate keys: {', '.join(sorted(set(duplicates)))}")
    return catalog


def validate_catalog(path: Path, neutral: dict[str, str]) -> list[str]:
    errors: list[str] = []
    try:
        catalog = parse_catalog(path)
    except ValueError as exc:
        return [str(exc)]

    unknown = sorted(set(catalog) - set(neutral))
    if unknown:
        errors.append(f"{path}: unknown keys: {', '.join(unknown)}")

    for key, value in catalog.items():
        if key not in neutral:
            continue
        expected = Counter(PLACEHOLDER_RE.findall(neutral[key]))
        actual = Counter(PLACEHOLDER_RE.findall(value))
        if expected != actual:
            errors.append(
                f"{path}: placeholder mismatch for {key}: "
                f"expected {dict(expected)}, found {dict(actual)}"
            )
    return errors


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--repair",
        action="store_true",
        help="close malformed introductory comments before validating",
    )
    args = parser.parse_args()

    if not NEUTRAL_FILE.exists():
        print(f"Missing neutral catalog: {NEUTRAL_FILE}", file=sys.stderr)
        return 2

    satellite_files = sorted(
        path for path in RESOURCE_DIR.glob(SATELLITE_PATTERN) if path != NEUTRAL_FILE
    )
    if not satellite_files:
        print("No satellite RESX catalogs found.", file=sys.stderr)
        return 2

    if args.repair:
        for path in satellite_files:
            original = read_text(path)
            repaired, changed = repair_unclosed_schema_comment(original)
            if changed:
                path.write_text(repaired, encoding="utf-8", newline="\n")
                print(f"Repaired introductory comment: {path}")

    try:
        neutral = parse_catalog(NEUTRAL_FILE)
    except ValueError as exc:
        print(exc, file=sys.stderr)
        return 1

    errors: list[str] = []
    for path in satellite_files:
        errors.extend(validate_catalog(path, neutral))

    if errors:
        print("RESX validation failed:", file=sys.stderr)
        for error in errors:
            print(f"  - {error}", file=sys.stderr)
        return 1

    print(
        f"RESX validation passed: {len(satellite_files)} satellite catalogs, "
        f"{len(neutral)} neutral keys."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
