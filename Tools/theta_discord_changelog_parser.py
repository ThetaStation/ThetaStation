"""
Changelog parser.
Triggered by a Github action, when something is pushed into main repo's branch,
posting parsed PR's description to a webhook in the format specified by test cases below.
"""

from os import environ
from requests import post
from github import Github
from github.Commit import Commit
from github.PullRequest import PullRequest
from typing import Optional, Tuple, List

GITHUB_REPOSITORY_NAME = "ThetaStation/ThetaStation"

def test_case_best_case_scenario() -> Tuple[str, str, bool]:
    input_case = """
This PR is about something something.

:cl: Luduk
add: Blah
del: Blah
fix: Blah
mod: Blah
"""
    output_case = """
:cl: Luduk
- :new: Blah
- :wastebasket: Blah
- :wrench: Blah
- :scales: Blah"""
    result = parse_changelog(input_case, "Luduk")
    return output_case, result, len(result) == 1 and result[0] == output_case

def test_case_alternative_case_scenario() -> Tuple[str, str, bool]:
    input_case = """
This PR is about something something.

🆑 Luduk
- add: Blah
- del: Blah
- fix: Blah
- mod: Blah
"""
    output_case = """
:cl: Luduk
- :new: Blah
- :wastebasket: Blah
- :wrench: Blah
- :scales: Blah"""
    result = parse_changelog(input_case, "Luduk")
    return output_case, result, len(result) == 1 and result[0] == output_case

def test_case_no_description() -> Tuple[str, str, bool]:
    input_case = """:cl: Luduk
add: Blah
del: Blah
fix: Blah
mod: Blah
"""
    output_case = """
:cl: Luduk
- :new: Blah
- :wastebasket: Blah
- :wrench: Blah
- :scales: Blah"""
    result = parse_changelog(input_case, "Luduk")
    return output_case, result, len(result) == 1 and result[0] == output_case

def test_case_no_changelog() -> Tuple[str, str, bool]:
    input_case = """
This PR is about something something.

Blah blah.
"""
    output_case = """"""
    result = parse_changelog(input_case, "Luduk")
    return output_case, result, len(result) == 0

def test_case_empty_changelog() -> Tuple[str, str, bool]:
    input_case = """
:cl: Luduk
"""
    output_case = """"""
    result = parse_changelog(input_case, "Luduk")
    return output_case, result, len(result) == 0

def test_case_multiple_changelogs() -> Tuple[str, str, bool]:
    input_case = """
:cl: Author1
add: Blah
del: Blah
/:cl:

:cl: Author2
fix: Blah
mod: Blah
/:cl:
"""
    output_case0 = """
:cl: Author1
- :new: Blah
- :wastebasket: Blah"""
    output_case1 = """
:cl: Author2
- :wrench: Blah
- :scales: Blah"""
    result = parse_changelog(input_case, "Luduk")
    return output_case0 + output_case1, result, len(result) == 2 and result[0] == output_case0 and result[1] == output_case1

def test_case_multiple_same_author() -> Tuple[str, str, bool]:
    input_case = """
:cl: Luduk
add: Blah
del: Blah
/:cl:

:cl: Luduk
fix: Blah
mod: Blah
/:cl:
"""
    output_case0 = """
:cl: Luduk
- :new: Blah
- :wastebasket: Blah"""
    output_case1 = """
:cl: Luduk
- :wrench: Blah
- :scales: Blah"""
    result = parse_changelog(input_case, "Luduk")
    return output_case0 + output_case1, result, len(result) == 2 and result[0] == output_case0 and result[1] == output_case1

def test_case_implicit_author() -> Tuple[str, str, bool]:
    input_case = """
:cl:
add: Blah
del: Blah
fix: Blah
mod: Blah
"""
    output_case = """
:cl: Luduk
- :new: Blah
- :wastebasket: Blah
- :wrench: Blah
- :scales: Blah"""
    result = parse_changelog(input_case, "Luduk")
    return output_case, result, len(result) == 1 and result[0] == output_case

def test_case_multiple_implicit_authors() -> Tuple[str, str, bool]:
    input_case = """
:cl:
add: Blah
del: Blah
/:cl:

:cl:
fix: Blah
mod: Blah
/:cl:
"""
    output_case0 = """
:cl: Luduk
- :new: Blah
- :wastebasket: Blah"""
    output_case1 = """
:cl: Luduk
- :wrench: Blah
- :scales: Blah"""
    result = parse_changelog(input_case, "Luduk")
    return output_case0 + output_case1, result, len(result) == 2 and result[0] == output_case0 and result[1] == output_case1

def get_test_cases() -> List[Tuple[str, str, bool]]:
    return [
        test_case_best_case_scenario,
        test_case_alternative_case_scenario,
        test_case_no_description,
        test_case_no_changelog,
        test_case_empty_changelog,
        test_case_multiple_changelogs,
        test_case_multiple_same_author,
        test_case_implicit_author,
        test_case_multiple_implicit_authors,
    ]

def parse_change_line(line: str) -> str:
    POSSIBLE_TAGS = {
        ("add", "new"): ":new:",
        ("del", "delete", "remove"): ":wastebasket:",
        ("fix", "bugfix"): ":wrench:",
        ("mod", "modify","tweak"): ":scales:"
    }
    POSSIBLE_TAG_PREFIXES = ["", "-", "- ", "-\t"]

    for tag_group, emoji in POSSIBLE_TAGS.items():
        for tag in tag_group:
            for tag_prefix in POSSIBLE_TAG_PREFIXES:
                if line.startswith(tag_prefix + tag + ":"):
                    return "\n- " + line.replace(tag_prefix + tag + ":", emoji)

    return ""

def get_pull_request_by_commit(commit: Commit) -> Optional[PullRequest]:
    pulls = commit.get_pulls()
    if pulls.totalCount == 0:
        return None

    return pulls[0]

def parse_changelog(changelog: str, author: str) -> List[str]:
    parses = []
    current_author = ""
    current_parse = ""

    in_changelog_scope = False

    for i, line in enumerate(changelog.split("\n")):
        if len(line) == 0 or line.isspace():
            continue
        if line.startswith(":cl:"):
            in_changelog_scope = True
            current_author = line.removeprefix(":cl:")
            if current_author == "" or current_author.isspace():
                current_author = " " + author
            continue
        if line.startswith("🆑"):
            in_changelog_scope = True
            current_author = line.removeprefix("🆑")
            if current_author == "" or current_author.isspace():
                current_author = " " + author
            continue
        if line.startswith("/:cl:") or line.startswith("/🆑"):
            if current_parse != "":
                parses.append(f"\n:cl:{current_author}" + current_parse)
                current_parse = ""
            in_changelog_scope = False
            continue
        if in_changelog_scope:
            parsed_line = parse_change_line(line)
            if parsed_line == "":
                raise RuntimeError(f"No tag found at line {i}.")
            current_parse += parsed_line

    if current_parse != "":
        parses.append(f"\n:cl:{current_author}" + current_parse)
    return parses

def run_tests():
    errors = False
    for case in get_test_cases():
        expected_case, output_case, success = case()
        if not success:
            printout = ""
            for text in output_case:
                printout += text
            print(f"ERROR {case}:\nEXPECTED:\n{expected_case}\nGOT:\n{printout}")
            errors = True

    if not errors:
        print("No errors.")

def parse_pr_changelog():
    try:
        hook_url = environ.get("HOOK_URL")
    except KeyError:
        raise RuntimeError("Hook URL is missing (HOOK_URL). Add it to the step environment.")
    try:
        commit_sha = environ.get("GITHUB_SHA")
    except KeyError:
        raise RuntimeError("Target commit hash is missing (GITHUB_SHA). Something went really wrong.")

    github_api = Github()
    repo_handler = github_api.get_repo(GITHUB_REPOSITORY_NAME)

    print(f"Target commit: {commit_sha}")
    target_pr = get_pull_request_by_commit(repo_handler.get_commit(commit_sha))

    if target_pr is None:
        raise RuntimeError("Target commit does not belong to any pull request.")

    changelog_parses = parse_changelog(target_pr.body, target_pr.user.login)
    if len(changelog_parses) == 0:
        print(f"No changelogs detected.")
        return

    print(f"Changelogs generated successfully:")
    for parse in changelog_parses:
        print(f"\n---\n{parse}\n---")

        discord_response = post(hook_url, json={"content": parse})
        if discord_response.ok:
            print("Successfully posted to hook.")
        else:
            print(f"Failed to post to hook, status code: {discord_response.status_code}")

def main():
    # run_tests()
    parse_pr_changelog()

if __name__ == "__main__":
    main()
