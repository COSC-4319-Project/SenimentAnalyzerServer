using System.IO;

namespace SenimentAnalyzerServer
{
    class LexiconLoader
    {
        public static string[] wordLocs = new string[6];
        public static string[] wordLists = new string[6];
        public static int[] listVers = new int[6];

        public static void Load() //Initialization - Loads Lexicons into memory, application path passed from main.
        {
            SetLocations();
            string appPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            for (int i = 0; i < 6; i++)
            {
                wordLists[i] = "";
                string[] lines = File.ReadAllLines(appPath + wordLocs[i]);
                for (int x = 0; x < lines.Length; x++)
                {
                    if (x == 0)//Version Number
                    {
                        listVers[i] = int.Parse(lines[x]);
                    }
                    
                    wordLists[i] += lines[x] + "\n";
                }
            }
        }

        private static void SetLocations()
        {
            wordLocs[0] = "/Lexicon/positive-words.txt";
            wordLocs[1] = "/Lexicon/negative-words.txt";
            wordLocs[2] = "/Lexicon/negation-words.txt";
            wordLocs[3] = "/Lexicon/contrast-words.txt";
            wordLocs[4] = "/Lexicon/vauge-words.txt";
            wordLocs[5] = "/Lexicon/emoji-sentiment.txt";
    }
       
    }
}
