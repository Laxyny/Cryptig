# Cryptig

> Cryptig is a modern, secure and high-performance password manager for Windows.  
> Designed to offer strong encryption, custom `.mistig` file support, and complete local control over your credentials.

![GitHub last commit](https://img.shields.io/github/last-commit/laxyny/Cryptig?style=for-the-badge)
![GitHub issues](https://img.shields.io/github/issues/laxyny/Cryptig?style=for-the-badge)
![GitHub pull requests](https://img.shields.io/github/issues-pr/laxyny/Cryptig?style=for-the-badge)
![GitHub license](https://img.shields.io/github/license/laxyny/Cryptig?style=for-the-badge)

---

## About

Cryptig is a file-based password manager using encrypted `.mistig` containers.
All data is locally stored, using robust cryptography (AES-256-GCM + Argon2id), offering zero-knowledge protection by default.

---

## Main Features (WIP)

- Full encryption with Argon2id and AES-256-GCM
- Custom `.mistig` file format (binary, unreadable without decryption)
- Built with .NET / C# for performance and native Windows integration
- Local-only storage for maximum privacy
- Future cloud sync/export with encryption in mind
- Built-in password generator & password strength checker
- Encrypted `FileVault` for securing any type of file
- Explorer-style file vault interface with previews and rename options

### FileVault Overview

The `FileVault` format allows storing arbitrary files in a single encrypted
container. Internally, the files are zipped together and protected with
Argon2id-derived keys and AES-256-GCM encryption. Only users with the correct
master password can decrypt or view the contents.

This feature is experimental and offers a simple way to keep documents or
images private alongside your passwords.

From the main window, use **File > Create File Vault** or **File > Open File Vault**
to manage encrypted containers for your personal files.
The vault window now supports drag & drop of files, double-click to preview
items and daily backups just like the main password vault. Version 2 adds a
modern explorer-like interface with file previews, rename support and context
menus.
File vaults are stored per-user in `%AppData%\Cryptig\filevaults` and are
bound to the account that created them.

---

## Installation

Installation packages (MSI/EXE) will be available soon.

For development:
1. Clone the repository:
   ```bash
   git clone https://github.com/laxyny/Cryptig.git

---

## License

This project is licensed under the MPL-2.0 License - see the [LICENSE](LICENSE) file for details.

## Credits

Developed and maintained by [Kevin Gregoire](https://github.com/laxyny).
