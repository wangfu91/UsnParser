# Windows USN Change Journal Parser 🚀

A command utility for NTFS/ReFS to search the MFT & monitor the changes of USN Journal.

> **Note:**
> 🦀 **There is also a Rust port here: ([usn-parser-rs](https://github.com/wangfu91/usn-parser-rs)) which is more performant and uses significantly less memory.**

## 📦 Download

Latest version can be downloaded from the [releases/latest](https://github.com/wangfu91/UsnParser/releases/latest) page.

## 🛠️ Usage

```
Usage: UsnParser [command] [options]

Options:
  --version  Show version information.
  -h|--help  Show help information.

Commands:
  monitor    Monitor real-time USN journal changes
  read       Read history USN journal entries
  search     Search the Master File Table

Run 'UsnParser [command] -h|--help' for more information about a command.
```

### 💡 Examples

```bash
# 🔍 Search through Master File Table of volume D, print out all the files whose extension is ".xlsx".
UsnParser search D: -f *.xlsx
```

```bash
# 📄 Print out the change history for file "Report.docx" in the USN journal of volume D.
UsnParser read D: -f Report.docx
```

```bash
# 👀 Monitor realtime USN records of volume C.
UsnParser monitor C: 
```

```bash
# 📝 Monitor realtime USN records of volume C with a filter for txt files whose name starts with "abc".
UsnParser monitor C: -f abc*.txt 
```

## 📚 Dependencies 

* [DotNet.Glob](https://github.com/dazinator/DotNet.Glob)
* [McMaster.Extensions.CommandLineUtils](https://github.com/natemcmaster/CommandLineUtils)

