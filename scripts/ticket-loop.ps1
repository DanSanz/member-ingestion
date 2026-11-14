<#
ticket-loop.ps1

Runs every ticket that is ready for an agent, or with -Feature the ready tickets of one feature, with no one at the keyboard.
Each ticket gets a worktree, a fresh implement session, the repo check,
a fresh review session and a pull request. Then the script stops. A human merges.

Run it from inside the repo:
  .\scripts\ticket-loop.ps1 -Tracker local -Check 'python -m pytest -q' -DryRun
  .\scripts\ticket-loop.ps1 -Tracker github -Check 'python -m pytest -q' -Parallel 2

Pinned to Claude Code 2.1.325 and Copilot CLI 1.0.111. The headless options change between versions.
Windows PowerShell 5.1. No Docker, no WSL.
#>
param(
    [string] $Check,                  # the repo check command. You must pass it.
    [ValidateSet('claude', 'copilot')] [string] $Agent = 'claude',
    [ValidateSet('github', 'local')] [string] $Tracker = 'local',
    [string] $Base,                   # the branch each worktree starts from. The default is the repo's default branch.
    [int] $Parallel = 1,              # how many tickets run at once
    [switch] $DryRun,                 # print the plan and call no agent
    [string] $WorktreeRoot = 'C:\wt', # a short root, because some repos fail to build from a long path
    [int] $MaxTurns = 40,             # turn cap for each agent session
    [double] $Minutes = 30,           # time limit for each agent session. 0.1 is six seconds.
    [string] $Feature                 # only this feature's tickets. On GitHub they also carry this label. Locally they sit in .scratch\<feature>.
)
if ($args) { Write-Host "The script does not know $args. Check the spelling against the param block at the top of this script, then run it again."; exit 1 }
$ReadyLabel = 'ready-for-agent'       # the role string in docs/agents/triage-labels.md

function Main {
    if (-not $Check) { Stop-Run "Pass the repo check command, for example -Check 'python -m pytest -q'." }
    $repo = git rev-parse --show-toplevel 2>$null
    if (-not $repo) { Stop-Run "This folder is not in a git repo. Go to the repo folder and run the script from there." }
    $repo = $repo.Trim() -replace '/', '\'
    if (-not $Base) { $Base = Get-DefaultBranch }
    $c = @{ Agent = $Agent; Tracker = $Tracker; Check = $Check; Repo = $repo; Base = $Base; Feature = $Feature
            Root = Join-Path $WorktreeRoot (Split-Path $repo -Leaf); MaxTurns = $MaxTurns; Minutes = $Minutes }
    Write-Host "Tracker $Tracker, agent $Agent, base $Base, $Parallel at a time, check $Check$(if ($Feature) { ", feature $Feature" })"
    Assert-Setup $c
    $tickets = @(Get-ReadyTickets $c)
    if (-not $tickets) { Show-NoTicket $c; return }
    # The check must pass on the base before any agent starts, or a red base looks like the agent's fault.
    if (-not $DryRun) { New-Item -ItemType Directory -Force $c.Root | Out-Null; $c.Before = Test-Base $c }
    # The main checkout and the tracker folder as they are now. No session may change them.
    if (-not $DryRun) { $c.Outside = @(Get-OutsideState $c) }
    $jobs = @()
    foreach ($t in $tickets) {
        # Each worktree starts from the base, and only a human merges. So a ticket that waits on an open
        # ticket cannot build yet. Skip it and say so. implement-spec uses an integration branch. This loop does not.
        $blockers = @(Get-OpenBlockers $t $c)
        if ($blockers) { Write-Host "SKIP $($t.Id), blocked by open ticket $($blockers -join ', ')"; continue }
        if (git -C $repo branch --list "loop/$($t.Id)") { Show-Skip $t $c; continue }
        if ($DryRun) { Show-Plan $t $c; continue }
        while ($jobs.Count -ge $Parallel) { $jobs = @(Receive-Finished $jobs) }
        Write-Host "START $($t.Id), $($t.Title). Log at $(Get-LogPath $t $c)"
        $jobs += Start-TicketJob $t $c
    }
    while ($jobs.Count) { $jobs = @(Receive-Finished $jobs) }
    if ($script:Failed) { exit 1 }
}

# This function runs the six steps for one ticket. Each ticket runs in its own background job.
function Invoke-Ticket($t, $c) {
    $branch = "loop/$($t.Id)"; $wt = Join-Path $c.Root $t.Id; $log = Get-LogPath $t $c
    "==== run started $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" | Write-Log $log

    # 1. Isolation by worktree. The ticket gets its own folder and its own branch, cut from the base.
    git -C $c.Repo worktree add -b $branch $wt $c.Base 2>&1 | Write-Log $log
    if ($LASTEXITCODE) { return "FAIL $($t.Id), git could not make the worktree. See $log" }

    # 2. Fresh context per ticket. Each session is new and has a turn cap, a time limit and a short tool list.
    #    Isolation comes from the worktree, the tool list and the VDI. There is no container.
    #    The agent may edit files in its worktree, run the tests and commit. It may not push, merge or use the network.
    #    But the tests run code the agent wrote, and that code can do what you can do. So the limits lower the risk.
    #    They do not remove it. After each session the script checks the main checkout and the tracker folder.
    #    It does not check the rest of the machine. Your review before you merge is the last control.
    $impl = Invoke-Agent 'implement' $t $wt $c $log
    if (-not $impl.Ok) { return "FAIL $($t.Id), $($impl.Why). See $log" }
    $broke = Test-Outside $c $log; if ($broke) { return "FAIL $($t.Id), $broke" }
    if (-not (git -C $wt log --oneline "$($c.Base)..HEAD")) { return "FAIL $($t.Id), the agent made no commit. See $log" }

    # 3. A check a command can confirm. The script runs it. The agent's word is not enough.
    $after = Invoke-Check $wt $c $log "on $branch"
    if ($after.Code -ne 0) { return "FAIL $($t.Id), the check failed, so no pull request. See $log" }

    # 4. Review in a fresh context. A second session that did not write the code reads the diff.
    $review = Invoke-Agent 'review' $t $wt $c $log
    if (-not $review.Ok) { return "FAIL $($t.Id), $($review.Why), so no pull request. See $log" }
    $broke = Test-Outside $c $log; if ($broke) { return "FAIL $($t.Id), $broke" }

    # 5. Evidence before merge. The pull request shows the check output before and after.
    $pr = Publish-PullRequest $t $wt $c $branch (New-PrBody $t $wt $c $after $review.Text)
    if (-not $pr) { return "FAIL $($t.Id), the pull request was not opened. See $log" }

    # 6. A human merges. The script stops here.
    if ($c.Tracker -eq 'local') { return "DONE $($t.Id), body file at $pr" }
    return "DONE $($t.Id), pull request at $pr"
}

# ---- Helpers. Read these only when you want the detail. ----

# The only place where the two agents differ. A fix for either agent goes here.
# The allow lists are the agent's boundary. A tool call that is not on them is refused.
function Invoke-Agent($role, $t, $wt, $c, $log) {
    $note = 'You run unattended. Nobody will answer questions, so make each call yourself and say what you chose.'
    if ($role -eq 'implement') {
        $prompt = "/implement $($t.Ref) $note Skip the code-review step, because a fresh session reviews your work next. Commit to the current branch. The repo check is $($c.Check)"
        $tools = 'Read,Edit,Write,Glob,Grep,Skill,TodoWrite,Bash'
        $allow = 'Read(./**)', 'Edit(./**)', 'Skill', 'TodoWrite', 'Bash(git status *)', 'Bash(git diff *)', 'Bash(git log *)',
                 'Bash(git add *)', 'Bash(git commit *)', 'Bash(python -m pytest *)', 'Bash(dotnet test *)', "Bash($($c.Check))"
        $copilot = 'write', 'shell(git status)', 'shell(git diff)', 'shell(git log)', 'shell(git add)', 'shell(git commit)', 'shell(pytest)', 'shell(dotnet test)'
    } else {
        $prompt = "/code-review $($c.Base) The spec is the ticket below. The loop already ran $($c.Check) on this branch and it passed, so do not run it. $note Report only. Change no files."
        $tools = 'Read,Glob,Grep,Skill,Agent,Bash'
        $allow = 'Read(./**)', 'Skill', 'Agent', 'Bash(git diff *)', 'Bash(git log *)', 'Bash(git rev-parse *)'
        $copilot = 'shell(git diff)', 'shell(git log)', 'shell(git rev-parse)'
    }
    $prompt += "`n`nThe ticket follows.`n`n$($t.Body)"
    if ($c.Agent -eq 'claude') {
        # ./** means inside the worktree. Read covers Glob and Grep. Edit covers Write.
        # dontAsk refuses every tool call that is not on the allow list. The deny list names what must never run.
        # --setting-sources project loads the repo's skills and skips your own settings, so your own allow rules do not apply.
        # git diff and git log can write a file anywhere with --output, so that option is denied too.
        $deny = 'Edit(./.claude/**)', 'Bash(git push *)', 'Bash(git merge *)', 'Bash(git config *)', 'Bash(git branch *)', 'Bash(git * --output*)',
                'Bash(curl *)', 'WebFetch', 'WebSearch'
        $argv = '-p', $prompt, '--setting-sources', 'project', '--tools', $tools, '--allowedTools', ($allow -join ','), '--disallowedTools', ($deny -join ','),
                '--permission-mode', 'dontAsk', '--permission-prompts', 'none', '--max-turns', $c.MaxTurns, '--output-format', 'json'
    } else {
        # UNTESTED. This branch is built from copilot --help, version 1.0.111. Copilot has no turn cap, so only the time limit applies.
        # Copilot keeps file access inside the working folder by default. --disallow-temp-dir takes away the temp folder too.
        # A Copilot rule cannot name python -m pytest alone, so python is refused and the agent runs pytest.
        $deny = 'shell(git push)', 'shell(git merge)', 'shell(git config)', 'shell(git branch)', 'shell(python)', 'shell(powershell)', 'shell(pwsh)', 'shell(curl)', 'url'
        $argv = @('-p', "$prompt`n`nRun the tests with pytest or dotnet test.", '-s', '--no-ask-user', '--trust-folder', '--disallow-temp-dir') +
                @($copilot | ForEach-Object { "--allow-tool=$_" }) + @($deny | ForEach-Object { "--deny-tool=$_" })
    }
    "== $role session, $($c.Agent)" | Write-Log $log
    $run = Start-Agent $c.Agent $argv $wt $c.Minutes $log
    $run.Why = if ($run.TimedOut) { "the $role session reached the time limit set by -Minutes $($c.Minutes)" } else { "the $role session stopped with an error" }
    if ($c.Agent -eq 'claude' -and $run.Text) {
        try { $r = $run.Text | ConvertFrom-Json; $run.Text = $r.result
              if ($r.is_error) { $run.Ok = $false; $run.Why = "the $role session ended with $($r.subtype)" }
              "session $($r.session_id), $($r.num_turns) turns, $($r.subtype)" | Write-Log $log
              $r.permission_denials | ForEach-Object { "REFUSED $($_.tool_name) $(($_.tool_input | ConvertTo-Json -Compress -Depth 3) -replace '^(.{300}).+$', '$1...')" } | Write-Log $log
        } catch { }
    }
    $run.Text | Write-Log $log
    $run
}

# Starts the agent as a child process and stops it when the time limit runs out.
function Start-Agent($exe, $argv, $wt, $minutes, $log) {
    $path = (Get-Command $exe -CommandType Application | Select-Object -First 1).Source
    $line = ($argv | ForEach-Object { '"' + ("$_" -replace '(\\*)"', '$1$1\"' -replace '(\\+)$', '$1$1') + '"' }) -join ' '
    New-Item "$log.in" -ItemType File -Force | Out-Null
    $p = Start-Process $path $line -WorkingDirectory $wt -NoNewWindow -PassThru `
        -RedirectStandardInput "$log.in" -RedirectStandardOutput "$log.out" -RedirectStandardError "$log.err"
    $null = $p.Handle
    $inTime = $p.WaitForExit([int]($minutes * 60000))
    if (-not $inTime) { taskkill /T /F /PID $p.Id 2>&1 | Out-Null; $null = $p.WaitForExit(10000); "TIMED OUT at the limit set by -Minutes $minutes" | Write-Log $log }
    $text = Get-Content "$log.out" -Raw -Encoding UTF8; Get-Content "$log.err" -Encoding UTF8 | Write-Log $log
    Remove-Item "$log.in", "$log.out", "$log.err"
    @{ Ok = ($inTime -and $p.ExitCode -eq 0); Text = $text; TimedOut = -not $inTime }
}

function Invoke-Check($wt, $c, $log, $label) {
    Push-Location $wt
    # A line on standard error comes back as an error record. Its message is the text of the line.
    $out = powershell -NoProfile -ExecutionPolicy Bypass -Command $c.Check 2>&1 |
        ForEach-Object { if ($_ -is [Management.Automation.ErrorRecord]) { $_.Exception.Message } else { "$_" } }
    # PowerShell gives 1 for any failing command, so the result is said in words and not as a number.
    $code = $LASTEXITCODE; $result = if ($code) { 'failed' } else { 'passed' }
    Pop-Location
    "== check $label, $result" | Write-Log $log; $out | Write-Log $log
    @{ Code = $code; Result = $result; Label = $label; Tail = ($out | Select-Object -Last 5) -join "`n" }
}

# Runs the check once on the base in a spare worktree. A broken check or a red base stops the run here.
function Test-Base($c) {
    $wt = Join-Path $c.Root '_base'; $log = Join-Path $c.Root '_base.log'
    "==== run started $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" | Write-Log $log
    if (Test-Path $wt) { git -C $c.Repo worktree remove --force $wt 2>&1 | Write-Log $log }
    git -C $c.Repo worktree add --detach $wt $c.Base 2>&1 | Write-Log $log
    $before = Invoke-Check $wt $c $log "on $($c.Base)"
    git -C $c.Repo worktree remove --force $wt 2>&1 | Write-Log $log
    if ($before.Code -ne 0) { Stop-Run "The check fails on $($c.Base) before any agent runs. Fix the base or the check first. See $log" }
    $before
}

# Stops before any agent runs when the setup is wrong. A different agent version only gives a warning.
function Assert-Setup($c) {
    $pin = @{ claude = '2.1.325'; copilot = '1.0.111' }[$c.Agent]
    $have = "$(& $c.Agent --version 2>$null)"
    if ($have -notmatch [regex]::Escape($pin)) { Write-Warning "This script is pinned to $($c.Agent) $pin and found $have. The headless options change between versions." }
    $exe = ($c.Check.Trim() -split '\s+')[0]
    if (-not (Get-Command $exe -ErrorAction SilentlyContinue)) { Stop-Run "The check command $exe does not exist here. Pass a command that runs, for example -Check 'python -m pytest -q'." }
    $local = $c.Base -replace '^origin/', ''
    if (-not (Test-Ref $c "refs/heads/$($c.Base)")) {
        if (Test-Ref $c "refs/heads/$local") { Stop-Run "The base $($c.Base) is a remote branch. Pass -Base $local, the local branch." }
        if (Test-Ref $c "refs/remotes/origin/$local") { Stop-Run "The base $($c.Base) is not a local branch in this clone. Run git branch $local origin/$local to make it, then run the script again with -Base $local." }
        Stop-Run "The branch $($c.Base) does not exist here or on origin. Pass -Base with a branch that exists."
    }
    # The agent may not set a git identity, so without one it cannot commit.
    if (-not (git -C $c.Repo config user.name) -or -not (git -C $c.Repo config user.email)) {
        Stop-Run "Git has no user name or email for this repo, so the agent cannot commit. Run these two commands, then run the script again:`n  git config user.name `"Your Name`"`n  git config user.email you@example.com"
    }
}

# Prints why the run stops, with no PowerShell error detail, and ends with exit code 1.
function Stop-Run($why) { Write-Host $why; exit 1 }

function Test-Ref($c, $ref) { git -C $c.Repo show-ref --verify --quiet $ref; $LASTEXITCODE -eq 0 }

# Says why no ticket was found, so a run that does nothing still tells the person what to change.
function Show-NoTicket($c) {
    if ($c.Tracker -eq 'github') { Write-Host "No open issue has the label $ReadyLabel$(if ($c.Feature) { " and the label $($c.Feature)" }). Label each ticket that is ready, then run the script again." }
    else { Write-Host "No ticket file in .scratch$(if ($c.Feature) { "\$($c.Feature)" }) has the line Status: $ReadyLabel. Set that line in each ticket that is ready, then run the script again." }
}

# Prints the SKIP line for a branch left from an earlier run. After DONE the work waits for review. After FAIL the person clears it first.
function Show-Skip($t, $c) {
    $wt = Join-Path $c.Root $t.Id
    $first = if (Test-Path $wt) { "git worktree remove --force $wt" } else { 'git worktree prune' }
    Write-Host "SKIP $($t.Id), the branch loop/$($t.Id) exists from an earlier run. If that run ended DONE, its work waits for a human to review and merge. If it failed, run these two commands, then run the loop again:"
    Write-Host "  $first`n  git branch -D loop/$($t.Id)"
}

# The remote's default branch when there is a remote. Otherwise the branch checked out now.
function Get-DefaultBranch {
    if (git remote) {
        $m = [regex]::Match(((git ls-remote --symref origin HEAD 2>$null) -join "`n"), 'ref: refs/heads/(\S+)\s+HEAD')
        if ($m.Success) { return $m.Groups[1].Value }
    }
    (git symbolic-ref --short HEAD).Trim()
}

# What no session may change: the main checkout's branch, its files and the tracker folder.
function Get-OutsideState($c) {
    "main checkout on $(git -C $c.Repo symbolic-ref -q --short HEAD) at $(git -C $c.Repo rev-parse HEAD)"
    git -C $c.Repo --no-optional-locks status --porcelain | ForEach-Object { "main checkout file $_" }
    $tracker = Join-Path $c.Repo '.scratch'
    if (Test-Path $tracker) {
        Get-ChildItem $tracker -Recurse -File | Get-FileHash | ForEach-Object { "tracker file $($_.Path.Replace("$($c.Repo)\", '')) $($_.Hash.Substring(0, 12))" }
    }
}

# Compares the state now with the state at the start. Gives back a reason to stop, or nothing.
function Test-Outside($c, $log) {
    $changed = @(Compare-Object @($c.Outside) @(Get-OutsideState $c) |
        ForEach-Object { $(if ($_.SideIndicator -eq '=>') { 'Now ' } else { 'Was ' }) + $_.InputObject })
    if (-not $changed) { return }
    "== something outside the worktree changed" | Write-Log $log; $changed | Write-Log $log
    "something outside the worktree changed, so no pull request. $($changed -join '. '). See $log"
}

function New-PrBody($t, $wt, $c, $after, $review) {
    $f = '```'
    $commits = (git -C $wt log --oneline "$($c.Base)..HEAD") -join "`n"
    $stat = (git -C $wt diff --stat "$($c.Base)...HEAD") -join "`n"
    $closes = if ($t.Number) { "`n`nCloses #$($t.Number)" } else { '' }
    # The review brings its own headings. Two more # put them under Review. A line inside a code fence stays as it is.
    $fence = $false
    $review = ($review -split "`n" | ForEach-Object { if ($_ -match '^\s*(```|~~~)') { $fence = -not $fence }
        if (-not $fence -and $_ -match '^#{1,4} ') { "##$_" } else { $_ } }) -join "`n"
    @"
## Summary

Ticket: $($t.Title), $($t.Ref)$closes

${f}text
$commits

$stat
$f

## Evidence

**Before:** ``$($c.Check)`` $($c.Before.Label), $($c.Before.Result)

${f}text
$($c.Before.Tail)
$f

**After:** ``$($c.Check)`` $($after.Label), $($after.Result)

${f}text
$($after.Tail)
$f

## Review

$review

Opened by scripts/ticket-loop.ps1. A human merges.
"@
}

# The body file goes beside the log. On GitHub the script also pushes the branch and opens the pull request.
function Publish-PullRequest($t, $wt, $c, $branch, $body) {
    $file = (Get-LogPath $t $c) -replace '\.log$', '.pr.md'
    [IO.File]::WriteAllText($file, $body)
    if ($c.Tracker -eq 'local') { return $file }
    git -C $wt push -u origin $branch 2>&1 | Write-Log (Get-LogPath $t $c)
    Push-Location $wt
    $url = gh pr create --base $c.Base --head $branch --title ($t.Title -replace '"', "'") --body-file $file
    Pop-Location
    return $url
}

function Get-ReadyTickets($c) {
    if ($c.Tracker -eq 'github') {
        $also = if ($c.Feature) { '--label', $c.Feature }   # gh takes an issue only when it has every label named
        $issues = gh issue list --label $ReadyLabel @also --state open --json number,title,body | ConvertFrom-Json
        if ($LASTEXITCODE) { Stop-Run "gh could not list the issues, so the script stops. Run gh auth status and log in again if it asks, then run the script again." }
        foreach ($i in $issues) {
            [pscustomobject]@{ Id = "issue-$($i.number)"; Number = $i.number; Title = $i.title; Ref = "#$($i.number)"
                               Body = "# $($i.title)`n`n$($i.body)"; File = $null }
        }
        return
    }
    $scratch = Join-Path $c.Repo '.scratch'
    if (-not (Test-Path $scratch)) { Stop-Run "No .scratch folder in $($c.Repo). Run /setup-matt-pocock-skills and /to-tickets first." }
    Get-ChildItem $scratch -Recurse -Filter '*.md' |
        Where-Object { $_.Directory.Name -eq 'issues' -and $_.Name -match '^\d+-[^.]+\.md$' } |
        Where-Object { -not $c.Feature -or $_.Directory.Parent.Name -eq $c.Feature } |
        Where-Object { (Get-Field $_.FullName 'Status') -eq $ReadyLabel } |
        ForEach-Object {
            $feature = $_.Directory.Parent.Name; $feature = $feature.Substring(0, [Math]::Min(12, $feature.Length))
            [pscustomobject]@{ Id = "$feature-$(($_.BaseName -split '-')[0])"; Number = $null
                               Title = (Get-Content $_.FullName -TotalCount 1) -replace '^#\s*', ''
                               Ref = $_.FullName.Replace("$($c.Repo)\", '').Replace('\', '/'); Body = Get-Content $_.FullName -Raw; File = $_.FullName }
        }
}

# A blocker is open while its ticket is not done, closed or resolved.
function Get-OpenBlockers($t, $c) {
    if ($c.Tracker -eq 'github') {
        $deps = gh issue view $t.Number --json blockedBy 2>$null | ConvertFrom-Json
        $deps.blockedBy.nodes | Where-Object { $_.state -eq 'OPEN' } | ForEach-Object { "#$($_.number)" }
        return
    }
    $line = Get-Field $t.File 'Blocked by'
    foreach ($n in [regex]::Matches("$line", '\d+') | ForEach-Object { $_.Value }) {
        $f = Get-ChildItem (Split-Path $t.File) -Filter ('{0:D2}-*.md' -f [int]$n) | Select-Object -First 1
        if ($f -and (Get-Field $f.FullName 'Status') -notmatch '^(done|closed|resolved)') { $f.BaseName }
    }
}

# Reads a line such as "Status: ready-for-agent" or "**Blocked by:** 01" from a ticket file.
function Get-Field($file, $name) {
    foreach ($l in Get-Content $file) {
        if ($l -match "^\s*\**$name\s*:\**\s*(.*)$") { return $matches[1].Trim().Trim('*').Trim() }
    }
}

function Show-Plan($t, $c) {
    Write-Host "PLAN $($t.Id), $($t.Title)"
    Write-Host "  1 worktree  $(Join-Path $c.Root $t.Id), branch loop/$($t.Id) from $($c.Base)"
    Write-Host "  2 implement $($c.Agent) /implement $($t.Ref), at most $($c.MaxTurns) turns and $($c.Minutes) minutes, short tool list"
    Write-Host "  3 check     $($c.Check), on $($c.Base) before any agent runs and on the branch after"
    Write-Host "  4 review    $($c.Agent) /code-review $($c.Base), then a check of the main checkout and the tracker folder"
    $pr = if ($c.Tracker -eq 'local') { 'no remote, body file only' } else { "gh pr create --base $($c.Base)" }
    Write-Host "  5 PR        $pr, body at $((Get-LogPath $t $c) -replace '\.log$', '.pr.md')"
    Write-Host "  6 stop      a human merges"
    Write-Host "  log         $(Get-LogPath $t $c)"
}

function Get-LogPath($t, $c) { Join-Path $c.Root "$($t.Id).log" }

function Write-Log($Path) { process { if ($null -ne $_) { Add-Content -Path $Path -Value "$_" -Encoding UTF8 } } }

# A background job starts empty, so it loads this file's text without running Main, then runs one ticket.
function Start-TicketJob($t, $c) {
    Start-Job -ScriptBlock { param($code, $t, $c) . ([scriptblock]::Create($code)) -Check $c.Check; Invoke-Ticket $t $c } `
        -ArgumentList (Get-Content $PSCommandPath -Raw), $t, $c
}

# Prints each ticket's result as soon as it ends. Gives back the jobs still running.
function Receive-Finished($jobs) {
    Wait-Job $jobs -Any | Out-Null
    foreach ($j in $jobs) {
        if ($j.State -eq 'Running') { $j; continue }
        $out = @(Receive-Job $j 2>&1 | ForEach-Object { "$_" })
        $out | ForEach-Object { Write-Host $_ }
        if (-not ($out -match '^DONE')) { $script:Failed = $true }
        Remove-Job $j
    }
}

# A background job loads this file with a dot. Then Main must not run.
if ($MyInvocation.InvocationName -ne '.') { Main }
