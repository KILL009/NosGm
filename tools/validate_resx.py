#!/usr/bin/env python3
"""Validate NosGM RESX catalogs and optionally repair their required structure."""

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

REQUIRED_RESHEADERS = (
    ("resmimetype", "text/microsoft-resx"),
    ("version", "2.0"),
    (
        "reader",
        "System.Resources.ResXResourceReader, System.Windows.Forms, "
        "Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
    ),
    (
        "writer",
        "System.Resources.ResXResourceWriter, System.Windows.Forms, "
        "Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
    ),
)


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

    return text[:first_data] + "-->\n  " + text[first_data:], True


def parse_xml_text(text: str, path: Path) -> ET.Element:
    try:
        root = ET.fromstring(text)
    except ET.ParseError as exc:
        raise ValueError(f"{path}: invalid XML: {exc}") from exc

    if root.tag != "root":
        raise ValueError(f"{path}: expected <root>, found <{root.tag}>")
    return root


def repair_missing_resheaders(text: str, path: Path) -> tuple[str, bool]:
    """Insert the four ResX headers required by ResGen and Visual Studio."""
    root = parse_xml_text(text, path)
    present = {node.get("name") for node in root.findall("resheader")}
    missing = [(name, value) for name, value in REQUIRED_RESHEADERS if name not in present]
    if not missing:
        return text, False

    first_data = text.find("<data ")
    if first_data >= 0:
        line_start = text.rfind("\n", 0, first_data) + 1
        indent = text[line_start:first_data] or "  "
    else:
        closing_root = text.rfind("</root>")
        if closing_root < 0:
            raise ValueError(f"{path}: missing </root>")
        line_start = text.rfind("\n", 0, closing_root) + 1
        indent = "  "

    block_parts: list[str] = []
    for name, value in missing:
        block_parts.extend(
            (
                f'{indent}<resheader name="{name}">\n',
                f"{indent}  <value>{value}</value>\n",
                f"{indent}</resheader>\n",
            )
        )

    return text[:line_start] + "".join(block_parts) + text[line_start:], True


def validate_resheaders(root: ET.Element, path: Path) -> None:
    headers: dict[str, str] = {}
    duplicates: list[str] = []

    for node in root.findall("resheader"):
        name = node.get("name")
        if not name:
            raise ValueError(f"{path}: <resheader> element without a name attribute")
        if name in headers:
            duplicates.append(name)
        value_node = node.find("value")
        headers[name] = "" if value_node is None or value_node.text is None else value_node.text.strip()

    if duplicates:
        raise ValueError(
            f"{path}: duplicate resheaders: {', '.join(sorted(set(duplicates)))}"
        )

    for name, expected in REQUIRED_RESHEADERS:
        actual = headers.get(name)
        if actual is None:
            raise ValueError(f"{path}: missing required resheader '{name}'")
        if actual != expected:
            raise ValueError(
                f"{path}: invalid resheader '{name}': expected '{expected}', found '{actual}'"
            )


def parse_catalog(path: Path) -> dict[str, str]:
    text = read_text(path)
    root = parse_xml_text(text, path)
    validate_resheaders(root, path)

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
    try:
        catalog = parse_catalog(path)
    except ValueError as exc:
        return [str(exc)]

    errors: list[str] = []
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


def repair_catalog(path: Path) -> bool:
    original = read_text(path)
    repaired, comment_changed = repair_unclosed_schema_comment(original)
    repaired, headers_changed = repair_missing_resheaders(repaired, path)
    changed = comment_changed or headers_changed

    if changed:
        path.write_text(repaired, encoding="utf-8", newline="\n")
        repairs: list[str] = []
        if comment_changed:
            repairs.append("comment")
        if headers_changed:
            repairs.append("required resheaders")
        print(f"Repaired {', '.join(repairs)}: {path}")
    return changed


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--repair",
        action="store_true",
        help="repair malformed comments and missing required ResX headers",
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
        try:
            for path in satellite_files:
                repair_catalog(path)
        except ValueError as exc:
            print(exc, file=sys.stderr)
            return 1

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
        f"{len(neutral)} neutral keys, required headers verified."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
