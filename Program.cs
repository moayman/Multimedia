using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FFMPEGWrapper;
using System.Security;

namespace dlltrial
{
    class Program
    {
        static void Main(string[] args)
        {
            Wrapper W1 = new Wrapper();
            
            //SecureString secureinput = new SecureString();
            //string input = "song.wav";
            //foreach (char c in input)
            //    secureinput.AppendChar(c);
            //SecureString secureoutput = new SecureString();
            //string output = "song.mp3";
            //foreach (char c in output)
            //    secureoutput.AppendChar(c);

            
            
            
            
            
            
            
            //byte[] bytes = Encoding.ASCII.GetBytes("song.wav");
            ////convert it to sbyte array
            //sbyte[] inputfilename = new sbyte[bytes.Length];
            //for (int i = 0; i < bytes.Length; i++)
            //    inputfilename[i] = (sbyte)bytes[i];

            //bytes = Encoding.ASCII.GetBytes("song.mp3");
            //sbyte[] outputfilename = new sbyte[bytes.Length];
            //for (int i = 0; i < bytes.Length; i++)
            //    inputfilename[i] = (sbyte)bytes[i];






            string str = "song.wav";
            byte[] bytes = Encoding.ASCII.GetBytes(str);
            string str2 = "song.mp3";
            byte[] bytes2 = Encoding.ASCII.GetBytes(str2);

            unsafe
            {
                fixed (byte* p = bytes, p2 = bytes2)
                {
                    sbyte* inputfile = (sbyte*)p;
                    sbyte* outputfile = (sbyte*)p2;
                    W1.wrap_convert_wav_to_mp3(inputfile, outputfile);
                }

                
            }
           
        }
    }
}
