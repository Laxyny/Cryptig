# Crypting

> Crypting is a modern, secure and high-performance password manager for Windows.  
> Designed to offer strong encryption, custom `.mistig` file support, and complete local control over your credentials.

![GitHub last commit](https://img.shields.io/github/last-commit/laxyny/crypting?style=for-the-badge)
![GitHub issues](https://img.shields.io/github/issues/laxyny/crypting?style=for-the-badge)
![GitHub pull requests](https://img.shields.io/github/issues-pr/laxyny/crypting?style=for-the-badge)
![GitHub license](https://img.shields.io/github/license/laxyny/crypting?style=for-the-badge)

---

## About

Crypting is a file-based password manager using encrypted `.mistig` containers.
All data is locally stored, using robust cryptography (AES-256-GCM + Argon2id), offering zero-knowledge protection by default.

---

## Main Features (WIP)

- Full encryption with Argon2id and AES-256-GCM
- Custom `.mistig` file format (binary, unreadable without decryption)
- Built with .NET / C# for performance and native Windows integration
- Local-only storage for maximum privacy
- Future cloud sync/export with encryption in mind
- Built-in password generator & password strength checker

---

## Installation

Installation packages (MSI/EXE) will be available soon.

For development:
1. Clone the repository:
   ```bash
   git clone https://github.com/laxyny/crypting.git

---

## License

This project is licensed under the MPL-2.0 License - see the [LICENSE](LICENSE) file for details.

## Credits

Developed and maintained by [Kevin Gregoire](https://github.com/laxyny).
