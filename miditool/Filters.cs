using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using csmidi;

namespace miditool
{
    static class Filters
    {
        public static void MaxExpr(MidiFile midi)
        {
            /******************
             *  maximizes expression to peak level
             */
            // get expression multipliers
            foreach (MidiTrack trk in midi.midiTracks)
            {
                int maxlevel = -1;
                foreach (MidiEvent ev in trk.midiEvents)
                {
                    if (ev is MessageMidiEvent)
                    {
                        MessageMidiEvent mev = ev as MessageMidiEvent;
                        // if controller is expression
                        if (mev.type == NormalType.Controller && mev.parameter1 == 11)
                        {
                            maxlevel = Math.Max(maxlevel, mev.parameter2);
                        }
                    }
                }
                if (maxlevel <= 0)
                    continue;
                
                double multiplier = 127.0f / maxlevel;

                foreach (MidiEvent ev in trk.midiEvents)
                {
                    if (ev is MessageMidiEvent)
                    {
                        MessageMidiEvent mev = ev as MessageMidiEvent;

                        // boost expression
                        if (mev.type == NormalType.Controller && mev.parameter1 == 11)
                        {
                            mev.parameter2 = (byte)Math.Max(0, Math.Min(127, Math.Round((double)mev.parameter2 * multiplier, MidpointRounding.AwayFromZero)));
                        }
                        // attenuate volume by the same degree
                        else if (mev.type == NormalType.Controller && mev.parameter1 == 7)
                        {
                            mev.parameter2 = (byte)Math.Max(0, Math.Min(127, Math.Round((double)mev.parameter2 / multiplier, MidpointRounding.AwayFromZero)));
                        }
                    }
                }
            }
        }

        public static void InstrMap(MidiFile midi, string mapping)
        {
            /******************
             *  maps instruments and drums using a map
             */
            byte[] instrMap = new byte[128];
            sbyte[] transposeMap = new sbyte[128];
            byte[] drumMap = new byte[128];
            bool[] isDrum = new bool[128];

            // parse mapping string
            //
            // format:
            // drum=127,imap=53:70,imap=30:25,dmap=40:50,trans=50:-12

            string[] cfg = mapping.Split(',');
            for (int i = 0; i < cfg.Length; i++)
            {
                string[] parts = cfg[i].Split('=');
                if (parts.Length != 2)
                    throw new Exception("Invalid mapping option: " + String.Join("=", parts));
                switch (parts[0])
                {
                    case "drum":
                        {
                            int drum = Convert.ToInt32(parts[1]);
                            if (drum < 0 || drum > 127)
                                throw new Exception("drum instrument number out of range: " + drum.ToString());
                            isDrum[drum] = true;
                        }
                        break;
                    case "imap":
                        {
                            string[] fromTo = parts[1].Split(':');
                            if (fromTo.Length != 2)
                                throw new Exception("Invalid instrument map: " + String.Join(":", fromTo));
                            int from = Convert.ToInt32(fromTo[0]);
                            if (from < 0 || from > 127)
                                throw new Exception("instrument 'from' out of range: " + from.ToString());
                            int to = Convert.ToInt32(fromTo[1]);
                            if (to < 0 || to > 127)
                                throw new Exception("instrument 'to' out of range: " + to.ToString());
                            instrMap[from] = (byte)to;
                        }
                        break;
                    case "dmap":
                        {
                            string[] fromTo = parts[1].Split(':');
                            if (fromTo.Length != 2)
                                throw new Exception("Invalid drum map: " + String.Join(":", fromTo));
                            int from = Convert.ToInt32(fromTo[0]);
                            if (from < 0 || from > 127)
                                throw new Exception("drum 'from' out of range: " + from.ToString());
                            int to = Convert.ToInt32(fromTo[1]);
                            if (to < 0 || to > 127)
                                throw new Exception("drum 'to' out of range: " + to.ToString());
                            drumMap[from] = (byte)to;
                        }
                        break;
                    case "trans":
                        {
                            string[] trans = parts[1].Split(':');
                            if (trans.Length != 2)
                                throw new Exception("Invalid Transpose: " + String.Join(":", trans));
                            int instr = Convert.ToInt32(trans[0]);
                            if (instr < 0 || instr > 127)
                                throw new Exception("Transpose instrument out of range: " + instr.ToString());
                            int shift = Convert.ToInt32(trans[1]);
                            if (shift < -128 || shift > 127)
                                throw new Exception("Transpose shift out of range: " + shift.ToString());
                            transposeMap[instr] = (sbyte)shift;
                        }
                        break;
                    default:
                        throw new Exception("Invalid mapping type: " + parts[0]);
                }
            }

            // update events

            foreach (MidiTrack trk in midi.midiTracks)
            {
                int curProg = -1;
                foreach (MidiEvent ev in trk.midiEvents)
                {
                    if (ev is MessageMidiEvent)
                    {
                        MessageMidiEvent mev = ev as MessageMidiEvent;
                        if (mev.type == NormalType.Program) 
                        {
                            curProg = mev.parameter1;
                            mev.parameter1 = instrMap[mev.parameter1];
                        } 
                        else if (mev.type == NormalType.NoteON || mev.type == NormalType.NoteOFF)
                        {
                            if (curProg != 0)
                            {
                                if (isDrum[curProg]) 
                                {
                                    mev.parameter1 = drumMap[mev.parameter1];
                                }
                                else
                                {
                                    mev.parameter1 = (byte)(mev.parameter1 + transposeMap[curProg]);
                                }
                            }
                        }
                    }
                }
            }
        }

        public static void ClearCtrl(MidiFile midi, string types)
        {
            /******************
             *  removes controller events of the specified kind
             */
            bool[] controllerMap = new bool[128];
            // parse types
            Console.WriteLine("types is " + types);
            string[] controller = types.Split(',');
            Console.WriteLine("split types is " + String.Join(",", controller));
            foreach (string s in controller)
            {
                int n = Convert.ToInt32(s);
                if (n < 0 || n > 127)
                    throw new Exception("Invalid controller number " + n.ToString());
                Console.WriteLine("Invalidating all controllers with ID " + n.ToString());
                controllerMap[n] = true;
            }

            // delete events
            foreach (MidiTrack trk in midi.midiTracks)
            {
                trk.midiEvents.RemoveAll(x =>
                    x is MessageMidiEvent &&
                    (x as MessageMidiEvent).type == NormalType.Controller
                    && controllerMap[(x as MessageMidiEvent).parameter1]);
            }
        }

        public static void Trim(MidiFile midi)
        {
            /******************
             *  removes empty tracks and redundant controller/voice events
             */
            midi.midiTracks.RemoveAll(TrackIsOmitable);
            foreach (MidiTrack tr in midi.midiTracks)
            {
                // int array for storing the last set values of the controller values
                int[] controllerValues = new int[128];
                for (int i = 0; i < controllerValues.Length; i++)
                    controllerValues[i] = -1;
                int lastVoice = -1;
                int lastPitchBendA = -1;
                int lastPitchBendB = -1;
                for (int i = 0; i < tr.midiEvents.Count;)
                {
                    if (tr.midiEvents[i] is MessageMidiEvent)
                    {
                        MessageMidiEvent mev = tr.midiEvents[i] as MessageMidiEvent;
                        if (mev.type == NormalType.Program)
                        {
                            if (mev.parameter1 == lastVoice)
                            {
                                tr.midiEvents.RemoveAt(i);
                                continue;
                            }
                            lastVoice = mev.parameter1;
                        }
                        else if (mev.type == NormalType.PitchBend)
                        {
                            if (mev.parameter1 == lastPitchBendA && mev.parameter2 == lastPitchBendB)
                            {
                                tr.midiEvents.RemoveAt(i);
                                continue;
                            }
                            lastPitchBendA = mev.parameter1;
                            lastPitchBendB = mev.parameter2;
                        }
                        else if (mev.type == NormalType.Controller)
                        {
                            if (controllerValues[mev.parameter1] == mev.parameter2)
                            {
                                tr.midiEvents.RemoveAt(i);
                                continue;
                            }
                            controllerValues[mev.parameter1] = mev.parameter2;
                        }
                    }
                    // only count up if the current event didn't get deleted
                    i++;
                }
            }
        }

        // util functions
        private static bool TrackIsOmitable(MidiTrack tr)
        {
            foreach (MidiEvent ev in tr.midiEvents)
            {
                if (ev is MetaMidiEvent)
                {
                    MetaMidiEvent mev = ev as MetaMidiEvent;
                    if (mev.getMetaType() == MetaType.TempoSetting)
                        return false;
                }
                else if (ev is MessageMidiEvent)
                {
                    MessageMidiEvent mev = ev as MessageMidiEvent;
                    if (mev.type == NormalType.NoteON)
                        return false;
                    if (mev.type == NormalType.NoteOFF)
                        return false;
                }
            }
            return true;
        }
    }
}
