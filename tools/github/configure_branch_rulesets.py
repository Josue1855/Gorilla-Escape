#!/usr/bin/env python3
"""Preview or apply non-bypassing GitHub rulesets to Gorilla Escape repos.

--apply requires GORILLA_ADMIN_TOKEN with Administration:write on all repos.
Does not mutate existing rulesets with the same name; detects drift instead.
"""
import argparse
import json
import os
from pathlib import Path
from urllib.error import HTTPError
from urllib.request import Request, urlopen

OWNER = "Josue1855"
CONFIGS = {
    "Gorilla-Escape": ["java-react-foundation", "management-validation"],
    "Gorilla-Escape-Backend": ["backend (ubuntu-latest)", "backend (windows-latest)"],
    "Dise-o_pwa": ["pwa"],
}
NAME = "Gorilla Escape | PR review + CI | main/develop"


def ruleset(checks):
    return {
        "name": NAME,
        "target": "branch",
        "enforcement": "active",
        "bypass_actors": [],
        "conditions": {
            "ref_name": {
                "include": ["refs/heads/main", "refs/heads/develop"],
                "exclude": [],
            }
        },
        "rules": [
            {"type": "deletion"},
            {"type": "non_fast_forward"},
            {
                "type": "pull_request",
                "parameters": {
                    "required_approving_review_count": 1,
                    "dismiss_stale_reviews_on_push": True,
                    "require_code_owner_review": False,
                    "require_last_push_approval": False,
                    "required_review_thread_resolution": True,
                },
            },
            {
                "type": "required_status_checks",
                "parameters": {
                    "strict_required_status_checks_policy": True,
                    "required_status_checks": [{"context": x} for x in checks],
                },
            },
        ],
    }


def request(method, path, token, data=None):
    body = None if data is None else json.dumps(data).encode("utf-8")
    req = Request(
        "https://api.github.com" + path,
        data=body,
        method=method,
        headers={
            "Accept": "application/vnd.github+json",
            "Authorization": "Bearer " + token,
            "X-GitHub-Api-Version": "2022-11-28",
            "Content-Type": "application/json",
            "User-Agent": "GorillaEscapeRulesets",
        },
    )
    try:
        with urlopen(req, timeout=25) as res:
            return json.load(res)
    except HTTPError as exc:
        detail = exc.read(1500).decode("utf-8", "replace")
        raise RuntimeError(f"GitHub {method} {path}: HTTP {exc.code}: {detail}") from exc


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--apply", action="store_true")
    args = parser.parse_args()
    all_rules = {f"{OWNER}/{repo}": ruleset(checks) for repo, checks in CONFIGS.items()}
    Path("ruleset-preview.json").write_text(
        json.dumps(all_rules, indent=2) + "\n", encoding="utf-8"
    )
    if not args.apply:
        print("PREVIEW ONLY. No GitHub settings changed. Inspect ruleset-preview.json.")
        return
    token = os.environ.get("GORILLA_ADMIN_TOKEN", "")
    if not token:
        raise SystemExit("GORILLA_ADMIN_TOKEN (Administration:write for 3 repos) is required")
    for repo, policy in all_rules.items():
        endpoint = f"/repos/{repo}/rulesets"
        existing = request("GET", endpoint, token)
        same = [x for x in existing if x.get("name") == NAME]
        if same:
            if len(same) != 1:
                raise RuntimeError(f"Multiple matching rulesets: {repo}")
            current = request("GET", endpoint + f"/{same[0]['id']}", token)
            if current.get("enforcement") != "active":
                raise RuntimeError(f"{repo}: matching ruleset exists but is not active; review manually")
            current_checks = []
            for rule in current.get("rules", []):
                if rule.get("type") == "required_status_checks":
                    current_checks = [
                        c.get("context")
                        for c in rule.get("parameters", {}).get("required_status_checks", [])
                    ]
            expected = [x["context"] for x in policy["rules"][-1]["parameters"]["required_status_checks"]]
            if sorted(current_checks) != sorted(expected):
                raise RuntimeError(f"{repo}: existing ruleset has different checks; review manually")
            print(f"{repo}: existing ruleset present; no overwrite")
            continue
        created = request("POST", endpoint, token, policy)
        print(f"{repo}: created active ruleset id={created['id']}")


if __name__ == "__main__":
    main()
