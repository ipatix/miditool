# miditool
MIDI Batch Processing Tool

I created this tool in order to batch process MIDI files for things like the following:

- Remap Instruments
- Remap Drums
- Delete certain events

The featureset is by far incomplete. Feel free to do a pull request for new filters. 
It uses my "csmidi" library for MIDI parsing and therefore should be really easily extendable.

Usage:
```
$ miditool input.mid output.mid [options]
```

For a list of available options execute the program without any arguments.
