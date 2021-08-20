# NTFS USN Parser

 A command utility to monitor and filter NTFS USN Journals.

## Usage

```
NTFS USN Journal parser 0.1.1

A command utility to monitor and filter NTFS USN Journals.

Usage: UsnParser [options] <Volume>

Arguments:
  Volume          Volume pathname. <Required>

Options:
  --version       Show version information
  -?|-h|--help    Show help information
  -m|--monitor    Monitor real-time USN journal
  -s|--search     Search NTFS Master File Table
  -f|--filter     Filter USN journal by entry name
  -fo|--FileOnly  Get only the file entries
  -do|--DirOnly   Get only the directory entries
```

### Example

```bash
# Search Master File Table of volume C, print out all paths who's file name is "Readme.md"
UsnParser -s -f "Readme.md" C: 
```

```bash
# Print out all the USN records of file "Readme.md" in volume C.
UsnParser -f "Readme.md" C: 
```

```bash
# Monitor realtime USN reacords of volume C.
UsnParser -m C: 
```

```bash
# Monitor realtime USN reacords of volume C, only print out txt files whose name starts with "abc".
UsnParser -r C: -f abc*.txt 
```

## Dependencies 

* [DotNet.Glob](https://github.com/dazinator/DotNet.Glob)

* [McMaster.Extensions.CommandLineUtils](https://github.com/natemcmaster/CommandLineUtils)

