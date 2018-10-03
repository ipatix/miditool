using System;
using System.Linq;
using csmidi;
using System.IO;

namespace miditool
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Error();
            }
            try
            {
                string[] flags = new string[args.Length - 2];
                Array.Copy(args, 2, flags, 0, flags.Length);
                ArgList al = new ArgList(flags);
                MidiFile workpiece = new MidiFile();
                workpiece.loadMidiFromFile(args[0]);

                if (al.checkFlag("quantize-bpm"))
                {
                    Console.WriteLine("Quantizing Tempo Events");
                    Filters.QuantizeTempo(workpiece);
                }

                if (al.checkFlag("trim"))
                {
                    Console.WriteLine("Removing redundant Events...");
                    Filters.Trim(workpiece);
                }

                if (al.checkFlag("clear-ctrl"))
                {
                    Console.WriteLine("Clearing Controller Events");
                    Filters.ClearCtrl(workpiece, al.getOption("clear-ctrl"));
                }

                if (al.checkFlag("maximize"))
                {
                    Console.WriteLine("Maximizing Volume and Velocity");
                    Filters.Maximize(workpiece);
                }

                if (al.checkFlag("map"))
                {
                    Console.WriteLine("Mapping Instruments...");
                    Filters.InstrMap(workpiece, al.getOption("map"));
                }

                if (File.Exists(args[1]))
                    File.Delete(args[1]);
                workpiece.saveMidiToFile(args[1]);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
                Error();
            }
        }

        class ArgList
        {
            private string[] args;

            public ArgList(string[] args) {
                this.args = args;
                verify();
            }

            private void verify()
            {
                for (int i = 0; i < args.Length; i++)
                {
                    string s = args[i];
                    switch (s)
                    {
                        case "--quantize-bpm":
                        case "--trim":
                        case "--maximize":
                            break;
                        case "--map":
                            if (i + 1 >= args.Length)
                                throw new Exception("--map missing parameter string");
                            i++;
                            break;
                        case "--clear-ctrl":
                            if (i + 1 >= args.Length)
                                throw new Exception("--clear-ctrl missing parameter string");
                            i++;
                            break;
                        default:
                            throw new Exception("Invalid input flag: " + s);
                    }
                }
            }

            public bool checkFlag(string flag)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    string s = args[i];
                    switch (s)
                    {
                        case "--quantize-bpm":
                            if (flag == "quantize-bpm")
                                return true;
                            break;
                        case "--trim":
                            if (flag == "trim")
                                return true;
                            break;
                        case "--maximize":
                            if (flag == "maximize")
                                return true;
                            break;
                        case "--map":
                            if (flag == "map")
                                return true;
                            i++;
                            break;
                        case "--clear-ctrl":
                            if (flag == "clear-ctrl")
                                return true;
                            i++;
                            break;
                        default:
                            throw new Exception("Invalid check flag: " + flag);
                    }
                }
                return false;
            }

            public string getOption(string option)
            {
                for (int i = 0; i < args.Length; i++) {
                    string s = args[i];
                    switch (s)
                    {
                        case "--trim":
                        case "--maximize":
                        case "--quantize-bpm":
                            break;
                        case "--map":
                        case "--clear-ctrl":
                            if (option == "map" || option == "clear-ctrl")
                                return args[i+1];
                            i++;
                            break;
                        default:
                            throw new Exception("Invalid get option: " + option);
                    }
                }
                throw new Exception("Option <" + option + "> not specified");
            }
        }

        static void About()
        {
            Console.WriteLine("miditool (c) 2017 by ipatix");
        }

        static void Error()
        {
            Console.Error.WriteLine("Usage: miditool <input.mid> <output.mid> [flags...]");
            Console.Error.WriteLine("Valid Flags: --maximize");
            Console.Error.WriteLine("                  ^~~~ Maximize the volumes on each track but remain relative scale");
            Console.Error.WriteLine("             --trim");
            Console.Error.WriteLine("                  ^~~~ Remove redundant Midi Events");
            Console.Error.WriteLine("             --map drum=127,imap=50:49,dmap=60:36,trans=50:-12");
            Console.Error.WriteLine("                  ^~~~ MIDI prog 127 is a drum");
            Console.Error.WriteLine("                        map prog 50 to prog 49");
            Console.Error.WriteLine("                        map drum key 60 to key 36");
            Console.Error.WriteLine("                        transpose prog 50 by -12 semi tones (-1 octave)");
            Console.Error.WriteLine("             --clear-ctrl 7");
            Console.Error.WriteLine("                  ^~~~ Clear controller events of type 7 (volume)");
            Console.Error.WriteLine("             --quantize-bpm");
            Console.Error.WriteLine("                  ^~~~ Round BPM tempo values to nearest integer");
            Environment.Exit(1);
        }
    }
}
