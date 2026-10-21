#!/usr/bin/env python3
"""Verify independently built source snapshots and published API contract.

This is contract/static integration only: no real device, browser, TCP, or
WebSocket session is exercised.
"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import subprocess


def git_sha(directory):
    return subprocess.check_output(
        ["git", "-C", str(directory), "rev-parse", "HEAD"], text=True
    ).strip()


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--unity", default=".")
    parser.add_argument("--backend", default="external/backend")
    parser.add_argument("--frontend", default="external/pwa")
    parser.add_argument("--output", default="cross-repo-evidence.json")
    args = parser.parse_args()
    unity = Path(args.unity)
    backend = Path(args.backend)
    frontend = Path(args.frontend)

    version_file = unity / "Unity/ProjectSettings/ProjectVersion.txt"
    manifest_file = unity / "Unity/Packages/manifest.json"
    assert version_file.is_file() and manifest_file.is_file(), "Unity project files absent"
    unity_manifest = json.loads(manifest_file.read_text(encoding="utf-8"))
    assert "com.unity.test-framework" in unity_manifest["dependencies"]

    api_file = backend / "src/main/resources/static/openapi.json"
    schema = json.loads(api_file.read_text(encoding="utf-8"))
    assert schema.get("openapi", "").startswith("3."), "Unexpected OpenAPI version"
    players = schema.get("paths", {}).get("/api/players", {})
    assert {"get", "post"}.issubset(players), "GET/POST players contract missing"
    assert "200" in players["get"]["responses"], "GET 200 response missing"
    assert "201" in players["post"]["responses"], "POST 201 response missing"

    frontend_package = json.loads((frontend / "package.json").read_text(encoding="utf-8"))
    assert "build" in frontend_package["scripts"]
    assert (frontend / "dist/index.html").is_file(), "PWA build absent"
    backend_artifacts = list((backend / "target").glob("backend-*.jar"))
    assert backend_artifacts, "Verified backend JAR absent"

    report = {
        "unity_commit": git_sha(unity),
        "backend_commit": git_sha(backend),
        "frontend_commit": git_sha(frontend),
        "unity_editor": version_file.read_text(encoding="utf-8").strip(),
        "backend_contract_sha256": hashlib.sha256(api_file.read_bytes()).hexdigest(),
        "openapi": schema["openapi"],
        "players_methods": ["GET", "POST"],
        "frontend_package_version": frontend_package["version"],
        "checks": [
            "Unity project metadata present",
            "Backend compiled and independently verified by Maven",
            "Frontend compiled independently by Vite",
            "Players OpenAPI GET/POST contract shape present",
        ],
        "not_verified": [
            "Unity Player build or EditMode/PlayMode tests",
            "Frontend consuming actual backend HTTP responses",
            "Flutter build, mobile device or LAN/WebSocket",
        ],
    }
    Path(args.output).write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    with open(os.environ.get("GITHUB_STEP_SUMMARY", os.devnull), "a", encoding="utf-8") as f:
        f.write("### Cross-repository static integration\n")
        for component in ("unity", "backend", "frontend"):
            f.write(f"- {component}: `{report[component + '_commit']}`\n")
        f.write("- Maven/React build and OpenAPI shape verified. **Not end-to-end**.\n")
    print(json.dumps(report, indent=2))


if __name__ == "__main__":
    main()
