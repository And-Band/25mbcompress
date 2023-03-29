// See https://aka.ms/new-console-template for more information
using System.Diagnostics;
using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static System.Net.Mime.MediaTypeNames;
using System.ComponentModel.Design;

namespace VideosCompressor
{
    class Program
    {
        static void Main(string[] args)
        {
            string strExePath = AppDomain.CurrentDomain.BaseDirectory;

            string FirstInput = string.Join(" ", args);
            //string FirstInput = args[0];
            FirstInput = FirstInput.ToLower();

            if(FirstInput == "-help" || FirstInput == "-h" || FirstInput == "")
            {
                Console.WriteLine("Drag and drop on to 8mbc or specifing file from command line.");
                Console.WriteLine("");
                Console.WriteLine("");
                Console.WriteLine("use \"-createjson\" create a json full of option;");
                Console.WriteLine("  1=application's directory");
                Console.WriteLine("  2=working directory");
                Console.WriteLine("  8mbc will prefer in the working directory over the application's directory.");
                Console.WriteLine("");
                Console.WriteLine("8mbc requires ffmpeg to be installed.");
                Console.WriteLine("Easiest way is with \"winget install ffmpeg\".");
                Console.WriteLine("");
                Console.WriteLine("");
                Console.WriteLine("");
                Console.WriteLine("Icon is from Icons8:");
                Console.WriteLine("https://icons8.com/icon/56331/compress");
                if(FirstInput == "") { Console.ReadKey(); }
                Environment.Exit(0);
            }


            if (FirstInput == "-createjson 2")
            {
                OptionsCreateJson(Directory.GetCurrentDirectory());
                Environment.Exit(0);
            }
            else if(FirstInput == "-createjson" || FirstInput == "-createjson 1")
            {
                OptionsCreateJson(strExePath);
                Environment.Exit(0);
            }

            FirstInput = FirstInput.Replace("\"", ""); // removes quotes from the string
            FirstInput = FirstInput.Replace("/", "\\"); // This is windows god damn it!

            if(FirstInput.Substring(1,2) != ":\\")
            {
                FirstInput = Directory.GetCurrentDirectory() + "\\" + FirstInput;
            }


            string Input = FirstInput; // why? so I don blah blah blah
            string InputNoExt = Input.Remove(Input.LastIndexOf(".")); // removes file extention

            if (File.Exists(Input) == false)
            {
                Console.WriteLine("Input propor file location :)-(--<");
                Console.WriteLine(Input);
                Console.Read();
                Environment.Exit(0);
            }



            string ffprobeArg = "-print_format json -show_format -show_streams \"" + Input + "\"";

            Console.WriteLine("ffprobe args: " + ffprobeArg);

            Process ffprobe = new Process();
            ffprobe.StartInfo.FileName = "ffprobe.exe";
            ffprobe.StartInfo.Arguments = ffprobeArg;
            ffprobe.StartInfo.UseShellExecute = false;
            ffprobe.StartInfo.RedirectStandardOutput = true;
            ffprobe.Start();

            string ffprobeOutput = ffprobe.StandardOutput.ReadToEnd();
            //Console.WriteLine(ffprobe.StandardOutput);

            ffprobe.WaitForExit();

            //ffprobe json creation
            int ffprobeOutputJsonStart = ffprobeOutput.IndexOf("\"streams\": [");
            ffprobeOutput = ffprobeOutput.Substring(ffprobeOutputJsonStart, (ffprobeOutput.Length - ffprobeOutputJsonStart));
            ffprobeOutput = "{" + Environment.NewLine + "    " + ffprobeOutput;
            //getting data from ffprobe json
            dynamic ffprobejson = JsonConvert.DeserializeObject(ffprobeOutput);

            //getting stuff from
            float duration = ffprobejson["format"]["duration"];
            float size = ffprobejson["format"]["size"];
            int width = ffprobejson["streams"][0]["width"];
            int height = ffprobejson["streams"][0]["height"];
            string pix_fmt = ffprobejson["streams"][0]["pix_fmt"];
            string channels = ffprobejson["streams"][1][""];


            //user defined stuff
            dynamic Options;
            if(File.Exists(Directory.GetCurrentDirectory() + "\\8mbCompress.json")) //will use json in the working directory if availible
            {
                Options = JsonConvert.DeserializeObject(File.ReadAllText(Directory.GetCurrentDirectory() + "\\8mbCompress.json"));
            }
            else if (File.Exists(strExePath + "\\8mbCompress.json"))
            {
                Options = JsonConvert.DeserializeObject(File.ReadAllText(strExePath + "\\8mbCompress.json"));
            }
            else
            {
                Options = JsonConvert.DeserializeObject(DefaultOptionsJson());
            }



            float SizeLimit    = Options["SizeLimit"];
            float AudioAlcPer  = Options["AudioAlcPercent"];
            float MaxBitrateA  = Options["MaxBitrateAudio"];
            float MinBitrateA  = Options["MinBitrateAudio"];
            int MaxResH        = Options["MaxVerticalRes"];
            bool ForceMono     = Options["ForceMono"];
            string OptionsArgs  = Options["FfmpegArgs"];

            float newBitrateA = (SizeLimit * AudioAlcPer) / duration;   
            float newBitrateArolloff = 0;                             //Makes sure the audio bitrate is within limits and gives or takes from video bitrate
            if (newBitrateA > MaxBitrateA)                            //
            {                                                         //Does it work? idk to tired to figur it out
                newBitrateArolloff = newBitrateA - MaxBitrateA;       //
                newBitrateA = MaxBitrateA;                            //I just realized this is stupid but works
            }                                                         //I might change it
            if (newBitrateA < MinBitrateA)                            //
            {                                                         //
                newBitrateArolloff = newBitrateA - MinBitrateA;       //
                newBitrateA = MinBitrateA;                            //
            }



            float newBitrateV = ((SizeLimit - (SizeLimit*AudioAlcPer)) / duration) + newBitrateArolloff;  //I know this is stupid see comment above
            float newWidth = width;
            float newHeight = height;
            if (height >= MaxResH)
            {
                newHeight = MaxResH;
                newWidth = width * (newHeight / height);
            }

            string argBitrates = "-b:v " + newBitrateV + "k -b:a " + newBitrateA + "k ";
            string argScale = " ";
            if (height != newHeight)
            {
                argScale = " -vf scale=" + newWidth + ":" + newHeight + " ";
            }

            //force mono
            if (ForceMono ==  true) { OptionsArgs = OptionsArgs + " -ac 1 ";}


            string ffmpegArg = " -i \"" + Input + "\" " + OptionsArgs + argBitrates + argScale + "\"" + InputNoExt + "-lq.mp4\"";
            Console.WriteLine("Starting ffmpeg with args: " + ffmpegArg);

            Process ffmpeg = new Process();
            ffmpeg.StartInfo.FileName = "ffmpeg.exe";
            ffmpeg.StartInfo.Arguments = ffmpegArg;
            ffmpeg.StartInfo.UseShellExecute = false;
            ffmpeg.StartInfo.RedirectStandardOutput = true;
            ffmpeg.Start();

            //Console.WriteLine(ffmpeg.StandardOutput);





            ffmpeg.WaitForExit();
            Environment.Exit(0);
        }
        public static void OptionsCreateJson(string Dir)
        {
            if (File.Exists(Dir + "\\8mbcompress.json")) //AAAAAAAAAAAAAHHHHHHHHHHHHHHHHHHHHHHH!!!!!!!!!!!!!!!!!!!!! WHERE IS THE APLICATION DIRECTORY!?!?!??!!??!?!!?
            {
                Console.WriteLine("8mbCompress.json already exists. Delete it and try again"); 
            }
            else
            {
                using (StreamWriter sw = File.CreateText(Dir + "\\8mbcompress.json"))
                {
                    sw.WriteLine(DefaultOptionsJson());
                    Console.WriteLine("Created default json in: " + Dir);
                }
            }

        }

        public static string DefaultOptionsJson()
        {
            return
@"{
    ""_comment"": ""bitrate is in kbit; If audio does not work change libopus to aac"",
    ""SizeLimit"": ""64000"",
    ""AudioAlcPercent"": ""0.125"",
    ""MaxBitrateAudio"": ""96"",
    ""MinBitrateAudio"": ""16"",
    ""ForceMono"": ""False"",
    ""MaxVerticalRes"": ""720"",
    ""FfmpegArgs"": "" -strict -2 -c:v libx264 -preset slower -c:a libopus ""
}";
        }


    }
}
