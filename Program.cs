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
                    sbyte** metadata = W1.wrap_get_media_file_meta_data(inputfile);
                    int i = 0;
                    while(true)
                    {
                        String s1 = new String(metadata[i++]);
                        Console.WriteLine(s1);
                        if (s1 == "end") break;
                    }
                    //W1.wrap_convert_wav_to_mp3(inputfile, outputfile);
                }
            }
        }
    }
}
