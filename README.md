# NTFS USN Parser

 A simple tool written in C# to monitor and filter NTFS Change Journals.

## Usage

```bash
NTFS USN Journal parser 1.0.0

Parse NTFS USN Journal.

Usage: NtfsJournal [options] <Volume>

Arguments:
  Volume        Volume pathname. <Required>

Options:
  --version     Show version information
  -?|-h|--help  Show help information
  -f|--filter   Filter USN journal
  -r|--read     Read real-time USN journal
```

### Example

```bash
# Print all the USN records of txt files in volume D.
UsnParser -f *.txt D: 
```

```bash
# Read realtime USN reacords of volume D.
Usn Parser -r D: 
```

```c
# Read realtime USN reacords of volume D, only print out txt files whose name starts with "abc".
Usn Parser -r D: -f abc*.txt 
```

## Third party notices

* [DotNet.Glob](https://github.com/dazinator/DotNet.Glob)

* [McMaster.Extensions.CommandLineUtils](https://github.com/natemcmaster/CommandLineUtils)