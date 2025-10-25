# Code signing

## Requirements
- Kleopatra & gpg
  - Windows [gpg4win](https://www.gpg4win.org/)
  - Debian `sudo apt install kleopatra`
- Git

## Setup

Generate key with Kleopatra

Add public key into github profile

Setup key to git
```bash
# clear key
git config --global --unset gpg.format

# list keys
# sec = secret key, algorithm, created, key capabilities, expires
# we need S = signing
# look for 4096R/ABC123DEF456 as id
gpg --list-secret-keys --keyid-format=long

# set key
git config --global user.signingkey KEY_ID

# Configure autosign commits
git config --global commit.gpgsign true

# Configure autosign tags
git config --global tag.gpgSign true
```

