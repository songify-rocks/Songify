# Syncing the wiki

The [GitHub wiki](https://github.com/songify-rocks/Songify/wiki) is **not** the same Git repository as the application source. It has its own remote:

`https://github.com/songify-rocks/Songify.wiki.git`

Markdown in this repo lives under `docs/wiki/`. To **publish** those files to the live wiki:

1. Clone the wiki (once):

   ```bash
   git clone https://github.com/songify-rocks/Songify.wiki.git songify-wiki
   ```

2. Copy the tracked wiki files from the main repo into the clone (from the **Songify** repo root):

   **PowerShell**

   ```powershell
   Copy-Item -Path "docs\wiki\*" -Destination "..\songify-wiki\" -Recurse -Force
   ```

   Adjust paths if your wiki clone lives elsewhere. Do **not** overwrite `.git` in the wiki folder.

3. Commit and push **in the wiki clone**:

   ```bash
   cd songify-wiki
   git add -A
   git status
   git commit -m "docs: sync wiki from docs/wiki"
   git push origin master
   ```

   If the default branch is `main` instead of `master`, use `git push origin main`.

4. **Permissions:** pushing requires **write access** to the repository (and wiki enabled in repo settings). Use HTTPS with a [personal access token](https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/creating-a-personal-access-token) or SSH as you do for the main repo.

---

## Script (same repo)

From the **Songify** repository root, if you have cloned the wiki next to it (or anywhere):

```powershell
.\scripts\Sync-Wiki.ps1 -WikiClonePath C:\path\to\Songify.wiki
```

Then commit and push **inside** `Songify.wiki`.

---

## Optional: automation

You can add a CI job that pushes to `Songify.wiki` using a stored secret (PAT with `repo` scope). That is not set up in this repository by default; maintainers can add it if they want fully automated wiki deploys on each release.

---

## Wiki file names

GitHub maps `Home.md` to the wiki home page. Other pages use the filename without extension in URLs, e.g. `Getting-started.md` → `…/wiki/Getting-started`.
