using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;

namespace ListMembers
{
    class Program
    {
        static void PrintHelp()
        {
            Console.Out.WriteLine(
                "Usage:  ListMembers [Public|Protected|Private] <Assembly1> [<Assembly2> ...]");
            Console.Out.WriteLine("\tDefaults to \"Public\".\n");
        }

        static List<Type> 


        static void Main(string[] args)
        {
            BindingFlags BaseFlags = BindingFlags.FlattenHierarchy | BindingFlags.Public;
            bool Public = true, Protected = false, Private = false;

            if (args.Length < 1)
            {
                PrintHelp();
                return;
            }
            int pos = 0;
            switch (args[0].ToLower(CultureInfo.CurrentCulture))
            {
                case @"public":
                    pos = 1;
                    break;
                case @"protected":
                    Protected = true;
                    BaseFlags = BaseFlags | BindingFlags.NonPublic;
                    goto case @"public";
                case @"private":
                    Private = true;
                    goto case @"protected";
                default:
                    break;
            }
            
            if (args.Length <= pos)
            {
                PrintHelp();
                return;
            }

            List<Assembly> Libs = new List<Assembly>();

            for (int i = pos; i < args.Length; i++)
                try
                {
                    Libs.Add(Assembly.LoadFile(Path.Combine(Directory.GetCurrentDirectory(),args[i])));
                }
                catch (Exception)
                {
                    ConsoleColor current = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Out.WriteLine("Failed to load file: "+ Path.Combine(Directory.GetCurrentDirectory(),args[i]));
                    Console.ForegroundColor = current;
                }
                

            foreach (Assembly A in Libs)
            {
                List<Type> types = new List<Type>(A.GetTypes());
                foreach (Type type in types)
                {
                    List<Type> subTypes = new List<Type>(type.GetNestedTypes());
                    type.
                }
            }
        }
    }
}
