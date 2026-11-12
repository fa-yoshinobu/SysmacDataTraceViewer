"""Fail the maintenance check when the bundled .NET runtime is outdated."""
import argparse
import json
from pathlib import Path
import re
import urllib.request
import xml.etree.ElementTree as ET


def runtime_version(project):
    value = ET.parse(project).findtext(".//RuntimeFrameworkVersion", "")
    if not re.fullmatch(r"\d+\.\d+\.\d+", value):
        raise ValueError("RuntimeFrameworkVersion must pin a stable patch version")
    return value


def check_version(current, metadata):
    latest = metadata["latest-runtime"]
    if not re.fullmatch(r"\d+\.\d+\.\d+", latest) or latest.split(".")[:2] != current.split(".")[:2]:
        raise ValueError("Release metadata does not describe the pinned runtime channel")
    outdated = tuple(map(int, current.split("."))) < tuple(map(int, latest.split(".")))
    if outdated:
        print(f"::error title=Bundled .NET runtime needs updating::Pinned {current}; latest {latest}. Update RuntimeFrameworkVersion, validate, and publish a new application release.")
    else:
        print(f"Bundled runtime {current}; latest stable runtime {latest}.")
    return not outdated


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project", type=Path, default=Path(__file__).resolve().parents[2] / "src/SysmacDataTraceViewer/SysmacDataTraceViewer.csproj")
    args = parser.parse_args()
    current = runtime_version(args.project)
    channel = ".".join(current.split(".")[:2])
    url = f"https://builds.dotnet.microsoft.com/dotnet/release-metadata/{channel}/releases.json"
    with urllib.request.urlopen(url, timeout=30) as response:
        metadata = json.load(response)
    return 0 if check_version(current, metadata) else 1


if __name__ == "__main__":
    raise SystemExit(main())
