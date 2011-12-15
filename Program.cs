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

        static List<Type> GetSubTypes(Type T, BindingFlags flags, bool recurse, bool Protected = false, bool Private = false)
        {
            List<Type> retSet = new List<Type>();
            if (Protected || Private) flags = flags | BindingFlags.NonPublic;
            Type[] tmp = T.GetNestedTypes(flags);
            foreach (Type t in tmp)
            {
                if (t.IsPublic || (Protected && t.IsNotPublic && t.IsNestedFamily) || (Private && t.IsNotPublic && !t.IsNestedAssembly))
                    retSet.Add(t);
            }
            if (recurse)
            {
                foreach (Type t in retSet)
                {
                    retSet.AddRange(GetSubTypes(t, flags, recurse, Protected, Private));
                }
            }
            return retSet;
        }

        static IEnumerable<string> ExpandPath(string path)
        {
            string dirPath = path.LastIndexOf('\\') >= 0 ? path.Substring(0, path.LastIndexOf('\\')) : String.Empty;
            string prePath = path.Substring(0, path.IndexOf('*'));
            string postPath = path.Substring(path.IndexOf('*') + 1);
            if (dirPath.Equals(String.Empty))
            {
                dirPath = Directory.GetCurrentDirectory();
            }

            List<string> returning = new List<string>();
            IEnumerable<string> files = Directory.EnumerateFiles(dirPath);
            foreach (string file in files)
            {
                if (file.StartsWith(prePath) && file.EndsWith(postPath))
                    returning.Add(Path.Combine(dirPath, file));
            }
            return returning;
        }

        static Assembly LoadAssembly(string path)
        {
            try
            {
                if (!File.Exists(path))
                    path = Path.Combine(Directory.GetCurrentDirectory(), path);
                return Assembly.LoadFrom(path);
            }
            catch (Exception e)
            {
                ConsoleColor current = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Out.WriteLine("Failed to load file: " + path);
                Console.ForegroundColor = current;
                throw e;
            }
        }

        static void Main(string[] args)
        {
            BindingFlags BaseFlags = BindingFlags.Public | BindingFlags.FlattenHierarchy;
            BindingFlags MethodFlags = BindingFlags.Instance | BindingFlags.Static;
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
            {
                string path = args[i];
                if (path.Contains("*"))
                    foreach (string s in ExpandPath(path))
                    {
                        try
                        {
                            Libs.Add(LoadAssembly(s));
                        }
                        catch (Exception E)
                        {
                            Console.Error.WriteLine(E.Message);
                        }
                    }
                else
                    try
                    {
                        Libs.Add(LoadAssembly(path));
                    }
                    catch (Exception E)
                    {}
            }

            foreach (Assembly A in Libs)
            {
                List<Type> tmpTypes;
                try
                {
                    tmpTypes = new List<Type>(A.GetExportedTypes());
                }
                catch (ReflectionTypeLoadException e)
                {
                    Console.Error.WriteLine("Error loading types from assembly '" + A.GetName() + "':");
                    foreach (Exception E in e.LoaderExceptions)
                    {
                        Console.Error.WriteLine(E.Message);
                    }
                    Console.Error.WriteLine(e.StackTrace);
                    continue;
                }
                catch (FileNotFoundException e)
                {
                    Console.Error.WriteLine(e.Message);
                    Console.Error.WriteLine(e.StackTrace);
                    continue;
                }
                bool AsmListed = false;
                List<Type> types = new List<Type>(tmpTypes);
                foreach (Type T in tmpTypes)
                {
                    if ((!Protected && (T.IsNestedFamily || T.IsNotPublic)) || (!Private && T.IsNestedPrivate))
                        types.Remove(T);
                }
                List<Type> Subtypes = new List<Type>();
                foreach (Type type in types)
                {
                    Subtypes.AddRange(GetSubTypes(type, BaseFlags, true, Protected, Private));
                }
                types.AddRange(Subtypes);

                foreach (Type T in types)
                {
                    List<MethodInfo> methods = new List<MethodInfo>();
                    List<string> methodNames = new List<string>();
                    List<MethodInfo> tmp = new List<MethodInfo>(T.GetMethods(BaseFlags | MethodFlags));
                    foreach (MethodInfo M in tmp)
                    {
                        if ((M.IsPublic || (Protected && M.IsFamily) || (Private && M.IsPrivate))
                            && !methodNames.Contains(String.Concat(M.Name,M.ReturnType.Name)))
                        {
                            methods.Add(M);
                            methodNames.Add(String.Concat(M.Name, M.ReturnType.Name));
                        }
                    }
                    if (methods.Count > 0)
                    {
                        if (!AsmListed)
                        {
                            Console.Out.WriteLine("Assembly: " + A.FullName);
                            AsmListed = true;
                        }
                        Console.Out.WriteLine("\tType:  " + T.FullName + " (" + ( T.IsPublic ? "public" : 
                                                                              ( T.IsNotPublic ? "non-public": "???"))
                                                                              +") --");
                        foreach (MethodInfo I in methods)
                        {
                            string s = String.Empty;
                                if (I.IsStatic)
                                    s += " S";
                            s += "\t\t";
                            if (I.IsPublic) s += "public ";
                            else if (I.IsFamily) s += "protected ";
                            else if (I.IsPrivate) s += "private ";
                            else s += "??? ";
                            s += I.Name;
                            s += " : " + I.ReturnType.Name;
                            Console.Out.WriteLine(s);
                        }
                    }
                }

            }
        }
    }
}
