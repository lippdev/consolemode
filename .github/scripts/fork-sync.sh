#!/usr/bin/env bash
# Decides whether a sync PR (branch sync/vX, opened by the dojo's keeper agent) can be merged without
# a person, and if so merges it and starts the release. Clean means:
#   - the CI passed on the PR's latest commit;
#   - the upstream tag merges into main without conflicts (git merge-tree);
#   - what the agent changed on top of that clean merge stays inside ALLOWED_FILES, or inside
#     LINE_RULES files on lines matching their regex (versions, translation entries).
# Anything else gets the needs-review label and a comment, and waits for the owner.
# Configured by the calling workflow: UPSTREAM, ALLOWED_FILES, LINE_RULES, VERSION_FILE,
# VERSION_PATTERN, RELEASE_WORKFLOW, RELEASE_INPUTS, plus BRANCH, HEAD_SHA, CONCLUSION.
set -euo pipefail

tag=${BRANCH#sync/}
pr=$(gh pr list --head "$BRANCH" --state open --json number,headRefOid,labels \
  --jq '.[0] | select(.) | "\(.number) \(.headRefOid) \([.labels[].name] | join(","))"')
if [[ -z $pr ]]; then
  echo "No open PR for $BRANCH."
  exit 0
fi
read -r num head labels <<<"$pr"
if [[ ,$labels, == *,needs-review,* ]]; then
  echo "PR #$num is waiting for a review: nothing to do."
  exit 0
fi
if [[ $head != "$HEAD_SHA" ]]; then
  echo "The CI ran on $HEAD_SHA, the PR is at $head: the next CI run decides."
  exit 0
fi
if [[ $CONCLUSION != success ]]; then
  echo "CI $CONCLUSION on PR #$num: the agent or the owner fixes it, the next green run decides."
  exit 0
fi

review() {
  gh label create needs-review --color D93F0B --description "Sync PR that needs a person" 2>/dev/null || true
  gh pr edit "$num" --add-label needs-review
  gh pr comment "$num" --body "Fork sync: not merged automatically. $1"
  echo "$1"
  exit 0
}

git fetch -q origin main "$BRANCH"
git fetch -q --no-tags "https://github.com/$UPSTREAM.git" "refs/tags/$tag:refs/tags/$tag"
head_ref=origin/$BRANCH

git merge-base --is-ancestor "$tag" "$head_ref" || review "The branch does not contain upstream \`$tag\`."
if ! git merge-base --is-ancestor origin/main "$head_ref"; then
  gh pr comment "$num" --body "Fork sync: \`main\` moved after this branch was created. Merge \`main\` into it; the next green CI decides."
  exit 0
fi
if ! out=$(git merge-tree --write-tree origin/main "$tag"); then
  review "Upstream \`$tag\` conflicts with \`main\`: the agent resolved code by hand."
fi
clean=$(head -1 <<<"$out")

declare -A rules=()
while IFS= read -r line; do
  [[ -n $line ]] || continue
  rules[${line%% :: *}]=${line#* :: }
done <<<"$LINE_RULES"

while IFS= read -r file; do
  [[ -n $file ]] || continue
  if grep -qxF -- "$file" <<<"$ALLOWED_FILES"; then
    continue
  fi
  if [[ -n ${rules[$file]:-} ]]; then
    other=$(git diff -U0 "$clean" "$head_ref" -- "$file" | grep -E '^[+-]' | grep -Ev '^(\+\+\+|---) ' | cut -c2- |
      grep -Evx -- "${rules[$file]}" || true)
    [[ -z $other ]] || review "\`$file\` changes lines other than the allowed ones:"$'\n\n```\n'"$other"$'\n```'
    continue
  fi
  review "\`$file\` is not among the files the agent may change on its own."
done < <(git diff --name-only "$clean" "$head_ref")

gh pr merge "$num" --merge --match-head-commit "$HEAD_SHA"
git fetch -q origin main
version=$(git show "origin/main:$VERSION_FILE" | perl -ne 'if (/$ENV{VERSION_PATTERN}/) { print $1; exit }')
[[ -n $version ]] || { echo "Version not found in $VERSION_FILE" >&2; exit 1; }
args=()
while IFS= read -r input; do
  [[ -n $input ]] && args+=(-f "${input//\{version\}/$version}")
done <<<"$RELEASE_INPUTS"
gh workflow run "$RELEASE_WORKFLOW" --ref main "${args[@]}"
echo "Merged PR #$num and started the $version release."
