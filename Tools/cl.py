#changelog generator. triggered by action, when something is pushed into master, posting parsed PR's description to webhook.

repoName = "ThetaStation/ThetaStation"

from os import environ
from requests import post
from github import Github
from github.Commit import Commit
from github.PullRequest import PullRequest

try: hookUrl = environ.get("HOOK_URL")
except KeyError: raise RuntimeError("Hook URL is missing (HOOK_URL). Add it to the step environment.")
try: commitSha = environ.get("GITHUB_SHA")
except KeyError: raise RuntimeError("Target commit hash is missing (GITHUB_SHA). Something went really wrong.")

g = Github()
r = g.get_repo(repoName)

def getPullRequestByCommit(commit: Commit) -> PullRequest:
    pulls = commit.get_pulls()
    if pulls.totalCount == 0: return None
    return pulls[0]

def parsePullRequestDesc(desc: str) -> str:
    tags = {("add","new"):":new:",
            ("del","delete","remove"):":wastebasket:",
            tuple(["fix"]):":wrench:",
            ("mod","modify","tweak"):":scales:"}

    clFound = False
    result = ""
    lines = desc.split("\n")

    for i, line in enumerate(lines):
        if line.startswith(":cl:") or line.startswith("🆑"):
            clFound = True
            result += line + "\n"
            for ti, tagLine in enumerate(lines[i+1:]):
                if len(tagLine) == 0 or tagLine.isspace(): continue

                tagFound = False
                for tag, prefix in tags.items():
                    for variant in tag:
                        if tagLine.startswith(variant+":"):
                            result += prefix + " " + tagLine[len(variant)+1:] + "\n"
                            tagFound = True
                if not tagFound: raise RuntimeError(f"No tag found at line {i+ti+2}.")
            break

    if not clFound: raise RuntimeError(f"No changelog start found.")

    return result

print(f"Target commit: {commitSha}")
pr = getPullRequestByCommit(r.get_commit(commitSha))
if pr is None: raise RuntimeError("Target commit does not belong to any pull request.")

message = parsePullRequestDesc(pr.body)
print(f"Changelog generated successfully.\n---\n{message}\n---")

response = post(hookUrl, json={"content" : message})
if response.ok: print("Successfully posted to hook.")
else: print(f"Failed to post to hook, status code: {response.status_code}")
