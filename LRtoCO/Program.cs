using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml.Linq;
using System.Text;
using System.Threading.Tasks;
using LuaInterface;



namespace LRtoCO
{
    public class variables
    {
        public static double[] ColorBalanceMagenta = new double[18] { 1, 1, 1, 0, 0, 0, 127,  63, 119, -28.82256699, 28.82256699, -10000000, 10000000, 12, 0, 0, 0, 0 };
        public static double[] ColorBalanceBlue = new double[18]    { 1, 1, 1, 0, 0, 0,  67,  63, 127, -21.97827911, 21.97827911, -10000000, 10000000, 12, 0, 0, 0, 0 };
        public static double[] ColorBalanceCyan = new double[18]    { 1, 1, 1, 0, 0, 0,  63, 118, 127, -22.3223114,  22.3223114,  -10000000, 10000000, 12, 0, 0, 0, 0 };
        public static double[] ColorBalanceGreen = new double[18]   { 1, 1, 1, 0, 0, 0,  69, 127,  63, -38.9258461,  38.9258461,  -10000000, 10000000, 12, 0, 0, 0, 0 };
        public static double[] ColorBalanceYellow = new double[18]  { 1, 1, 1, 0, 0, 0, 127, 115,  63, -13.54243946, 13.54243946, -10000000, 10000000, 12, 0, 0, 0, 0 };
        public static double[] ColorBalanceRed = new double[18] { 1, 1, 1, 0, 0, 0, 127, 68, 63, -18.40856934, 18.40856934, -10000000, 10000000, 12, 0, 0, 0, 0 };
        public static double[] ColorBalanceAll = new double[18] { 0, 1, 1, 0, 0, 0, 0, 128, 0, -2, 2, -10000000, 10000000, 0, 0, 0, 0, 0 };


        public static double[] CustomBalanceRed = new double[17];
        public static double[] CustomBalanceOrange = new double[17];
        public static double[] CustomBalanceYellow = new double[17];
        public static double[] CustomBalanceGreen = new double[17];
        public static double[] CustomBalanceAqua = new double[17];
        public static double[] CustomBalanceBlue = new double[17];
        public static double[] CustomBalancePurple = new double[17];
        public static double[] CustomBalanceMagenta = new double[17];

        public static int ProcessColorBalance = 0;

        public static double[] LevelsShadow = new double[4] { 0, 0, 0, 0 };
        public static double[] LevelsShadowTarget = new double[4] { 0, 0, 0, 0 };
        public static double[] LevelsHighlight = new double[4] { 1, 1, 1, 1 };
        public static double[] LevelsHighlightTarget = new double[4] { 1, 1, 1, 1 };

        public static int ProcessLevels = 0;

        public static string ParentKey = "";

        public static List<int> CurveRGB = new List<int>();

    }
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Please enter LR Template file.");
                Console.WriteLine("Usage: LRtoCO \"filename\"");
                Console.ReadKey();
                return 1;
            }

            Console.WriteLine("This file: {0}", args[0]);

            Dictionary<String, String> keyvalue = new Dictionary<String, String>();

            Lua lua = new Lua();

            lua.DoFile(args[0]);

            ProcessTable(lua.GetTable("s"), keyvalue);

            //Create XML
            XElement root = new XElement("SL", from keyValue in keyvalue
            select //new XAtrribute("Engine", "800"),
            new XElement("E", new XAttribute("K",keyValue.Key), new XAttribute("V", keyValue.Value) )
            );

            // If levels (white/black point) changes made then add XML
            if (variables.ProcessLevels == 1)
            {
                string shadow = string.Join(";", variables.LevelsShadow);
                string shadowtarget = string.Join(";", variables.LevelsShadowTarget);
                string highlight = string.Join(";", variables.LevelsHighlight);
                string highlighttarget = string.Join(";", variables.LevelsHighlightTarget);

                root.Add(new XElement("E", new XAttribute("K", "Shadow"), new XAttribute("V", shadow)));
                root.Add(new XElement("E", new XAttribute("K", "TargetShadow"), new XAttribute("V", shadowtarget)));
                root.Add(new XElement("E", new XAttribute("K", "Highlight"), new XAttribute("V", highlight)));
                root.Add(new XElement("E", new XAttribute("K", "TargetHighlight"), new XAttribute("V", highlighttarget)));
            }

            //If color balance changes made then add to XML
            if (variables.ProcessColorBalance == 1)
            {
                string colorbalance =
                      string.Join(",", variables.ColorBalanceMagenta)
                    + ";" + string.Join(",", variables.ColorBalanceBlue)
                    + ";" + string.Join(",", variables.ColorBalanceCyan)
                    + ";" + string.Join(",", variables.ColorBalanceGreen)
                    + ";" + string.Join(",", variables.ColorBalanceYellow)
                    + ";" + string.Join(",", variables.ColorBalanceRed)
                    + ";" + string.Join(",", variables.ColorBalanceAll);

                //Console.WriteLine(colorbalance);
                root.Add(new XElement("E", new XAttribute("K", "ColorCorrections"), new XAttribute("V", colorbalance)));

                
            }

            string delimiter = ",";
            string gradationcurve = "";

            foreach (int point in variables.CurveRGB)
            {
                gradationcurve += System.Convert.ToString(point/255.0) + delimiter;

                if (delimiter == ",")
                { delimiter = ";";
                }
                else
                { delimiter = ",";
                }

                Console.WriteLine(point);
            }

            //Console.WriteLine(gradationcurve);

            root.Add(new XElement("E", new XAttribute("K", "GradationCurve"), new XAttribute("V", gradationcurve)));

            // Set CO Engine version
            root.SetAttributeValue("Engine", "900");

            //debug
            Console.WriteLine(root);

            // Save CO .costyle XML file
            string filename = Path.GetFileNameWithoutExtension(args[0]);
            filename += ".costyle";
            root.Save(filename);

            Console.ReadKey();

            return 0;
        }
        static void ProcessTable(LuaTable t, Dictionary<String,String> keyvalue)
        {
            foreach (DictionaryEntry d in t)
            {
                if (d.Value.GetType() == typeof(LuaTable)) 
                {
                    variables.ParentKey = System.Convert.ToString(d.Key);
                    ProcessTable((LuaTable)d.Value, keyvalue);
                }
                else
                {
                    string k = System.Convert.ToString(d.Key);
                    string v = System.Convert.ToString(d.Value);

                    switch (k)
                    {
                        case "internalName":
                        case "uuid":
                        case "Exposure2012":
                        case "Contrast2012":
                        case "Vibrance":
                        case "Highlights2012":
                        case "Shadows2012":
                            k = ConvertName(k);
                            v = ConvertValue(k, v);
                            keyvalue.Add(k, v);
                            Console.WriteLine(String.Format("{0}={1}", d.Key, d.Value));
                            break;

                        case "Saturation":
                            int i = System.Convert.ToInt32(v) * 8 / 7;
                            i = (i > 80) ? 80 : i;  //limit saturation to 50 or less
                            variables.ColorBalanceAll[4] = i;  //Saturation
                            variables.ProcessColorBalance = 1;
                            break;

                        case "Blacks2012":
                            double black = (v == "0") ? 0 : System.Convert.ToDouble(v);
                            
                            if (black < 0)
                            {
                                variables.LevelsShadow[3] = -(1.0 / 255.0) * black / 100.0 * 40.0;
                                variables.ProcessLevels = 1;
                            }
                            else
                            {
                                variables.LevelsShadowTarget[3] = (1.0 / 255.0) * black / 100.0 * 40.0;
                                variables.ProcessLevels = 1;
                            }
                            break;
                        case "Whites2012":
                            double white = (v == "0") ? 0 : System.Convert.ToDouble(v);
                            Console.WriteLine(white);
                            if (white < 0)
                            {
                                variables.LevelsHighlightTarget[3] = 1.0 + (1.0 / 255.0) * white / 100.0 * 25.0;
                                variables.ProcessLevels = 1;
                            }
                            else
                            {
                                variables.LevelsHighlight[3] = 1.0 - (1.0 / 255.0) * Math.Pow((white / 100.0), 2) * 166.0;
                                variables.ProcessLevels = 1;
                            }
                            break;
                        case "Clarity2012":
                            k = ConvertName(k);
                            v = ConvertValue(k, v);
                            keyvalue.Add(k, v);
                            keyvalue.Add("ClarityMethod", "2");
                            Console.WriteLine(String.Format("{0}={1}", d.Key, d.Value));
                            break;

                        default:
                            if (variables.ParentKey == "ToneCurvePV2012")
                            {
                                variables.CurveRGB.Add(System.Convert.ToInt32(d.Value));
                            }
                            Console.WriteLine(String.Format("{0}={1}", variables.ParentKey + "_" + d.Key, d.Value));
                            break;
                     }
                   
                    
                }
            }
        }
        static string ConvertName(string name)
        {
            switch (name)
            {
                case "internalName":
                    return "Name";
                case "uuid":
                    return name.ToUpper();
                case "Exposure2012":
                    return "Exposure";
                case "Contrast2012":
                    return "Contrast";
                case "Vibrance":
                    return "Saturation";
                case "Highlights2012":
                    return "HighlightRecovery";
                case "Shadows2012":
                    return "ShadowRecovery";
                case "Clarity2012":
                    return "Clarity";

                default:
                    return name;

            }
                      
        }
        static string ConvertValue(string name, string value)
        {
            switch (name)
            {
                case "Name":
                case "Exposure":
                case "Saturation": //vibrance
                    return value;
                case "UUID":
                    return System.Guid.NewGuid().ToString().ToUpper();
                case "Contrast":
                    return System.Convert.ToString(System.Convert.ToInt32(value)/2);
                case "ShadowRecovery":
                    return (System.Convert.ToInt32(value)< 0) ? "0" : System.Convert.ToString(System.Convert.ToInt32(value) / 2);
                case "HighlightRecovery":
                    return System.Convert.ToString(-System.Convert.ToInt32(value));
                case "Clarity":
                    return (System.Convert.ToInt32(value) >= 50) ? "100" : System.Convert.ToString(System.Convert.ToInt32(value) * 2);

                    
                default:
                    return "NOT";

            }
       

        }
      
    }
}

   
       
   