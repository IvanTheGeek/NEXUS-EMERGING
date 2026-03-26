# Git Auth for Codex

This guide explains why Git pushes can fail from Codex even when GitKraken can push successfully, and how to fix that in a way that survives reboot.

## Why This Happened Here

In this repository, the Git remote is currently HTTPS:

```text
https://github.com/IvanTheGeek/NEXUS-EMERGING.git
```

Codex uses command-line Git from the shell.

On this machine, the shell currently has:

- no Git credential helper configured
- no `gh` GitHub CLI login available
- no default SSH key configured for GitHub

So when Codex tried to push, Git failed with:

```text
fatal: could not read Username for 'https://github.com': No such device or address
```

GitKraken can still work because it may use its own stored credentials or GUI auth flow. That does not automatically give command-line Git credentials to Codex.

## Recommended Fix

Use SSH for GitHub access from the shell.

This is the cleanest option for Codex because:

- it avoids interactive HTTPS username or token prompts
- it works well across reboots once your key is configured
- it keeps Codex, terminal Git, and other CLI tools aligned

## One-Time Setup

### 1. Create an SSH key

Run:

```bash
mkdir -p ~/.ssh
ssh-keygen -t ed25519 -C "ivan@nexus-emerging" -f ~/.ssh/id_ed25519
```

If you want the least friction for Codex, you can leave the passphrase empty.

If you want better security, use a passphrase and pair it with an SSH agent or desktop keyring.

### 2. Show the public key

Run:

```bash
cat ~/.ssh/id_ed25519.pub
```

Copy the full line.

### 3. Add the key to GitHub

In GitHub:

- go to `Settings`
- go to `SSH and GPG keys`
- choose `New SSH key`
- paste the public key

### 4. Trust GitHub over SSH

Run:

```bash
ssh -T git@github.com
```

The first time, answer `yes` to trust the host.

If it is working, GitHub will confirm the SSH authentication.

### 5. Change the repo remote to SSH

From the repo root:

```bash
git remote set-url origin git@github.com:IvanTheGeek/NEXUS-EMERGING.git
```

Verify:

```bash
git remote -v
```

You should now see:

```text
origin  git@github.com:IvanTheGeek/NEXUS-EMERGING.git (fetch)
origin  git@github.com:IvanTheGeek/NEXUS-EMERGING.git (push)
```

### 6. Push the branch

```bash
git push -u origin <branch-name>
```

## Making It Work After Reboot

There are two practical options.

### Option A: No passphrase on the SSH key

This is the simplest path for Codex.

Once the key exists and the remote uses SSH, pushes should keep working after reboot with no extra login step.

Tradeoff:

- easiest for automation
- lower security if someone gets local access to your account or key files

### Option B: Use a passphrase and an SSH agent

This is more secure.

Typical per-login flow:

```bash
eval "$(ssh-agent -s)"
ssh-add ~/.ssh/id_ed25519
```

After that, Codex and shell Git can use the loaded key during that login session.

Tradeoff:

- better security
- one extra unlock step after reboot or login unless your desktop auto-loads the key

## Optional SSH Config

You can add a small SSH config file to make the key choice explicit:

Create or edit `~/.ssh/config`:

```sshconfig
Host github.com
  HostName github.com
  User git
  IdentityFile ~/.ssh/id_ed25519
  IdentitiesOnly yes
  AddKeysToAgent yes
```

Recommended permissions:

```bash
chmod 700 ~/.ssh
chmod 600 ~/.ssh/config ~/.ssh/id_ed25519
chmod 644 ~/.ssh/id_ed25519.pub
```

## Alternative Fixes

Other options are possible, but they are less direct here:

- configure an HTTPS credential helper
- install and authenticate `gh`
- use a personal access token with HTTPS

Those can work, but this machine currently has none of that configured, so SSH is the shortest path.

## Summary

Best practical fix for Codex on this machine:

1. create `~/.ssh/id_ed25519`
2. add the public key to GitHub
3. switch the repo remote to SSH
4. optionally use an SSH agent if you keep a passphrase

Once that is done, command-line Git used by Codex should be able to push normally.
