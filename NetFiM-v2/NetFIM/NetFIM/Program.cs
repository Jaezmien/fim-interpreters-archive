using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

using NetFIM.Core;

namespace NetFIM
{
    class Program
    {
        
        static void Main(string[] args)
        {
            NetFIMInterpreter nint = new NetFIMInterpreter();

            if(args.Count() > 0)
            {
                try
                {
                    Console.WriteLine("--==+ ...Interpreting Report... +==--\n");

                    nint.InterpretReport(args[0]);

                    Console.WriteLine();
                    Console.WriteLine(">> Report Info: " + string.Format("\"{0}\" by {1}", nint.ReportName, nint.Writer));

                    Console.WriteLine("--==+ Done! +==--");
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex);
                    Console.WriteLine();

                    Console.WriteLine("--==+\n");
                }
                
            }
            else
            {
                while(true)
                {
                    while(true)
                    {
                        Console.Write("[Run Report]: ");
                        string input = Console.ReadLine();
    
                        try
                        {
                            switch(input)
                            {
                                case "--dir":
                                    {
                                        // deadass lazy
                                        System.IO.Directory.GetDirectories("./").ToList().ForEach(x => Console.WriteLine(">> " + x));
                                        Console.WriteLine("--==+\n");
                                    }
                                    break;
                                case "--cls":
                                    {
                                        Console.Clear();
                                    }
                                    break;
                                default:
                                    {
                                        Console.WriteLine("--==+ ...Interpreting Report... +==--\n");

                                        nint.InterpretReport(input);
                                        Console.WriteLine();
                                        Console.WriteLine(">> Report Info: " + string.Format("\"{0}\" by {1}", nint.ReportName, nint.Writer));

                                        Console.WriteLine("--==+ Done! +==--\n");
                                    }
                                    break;
                            }
                        }
                        catch (FiMException fex)
                        {
                            Console.WriteLine("[!] " + fex.Message);
                            Console.WriteLine();

                            Console.WriteLine("--==+\n");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                            Console.WriteLine();

                            Console.WriteLine("--==+\n");
                        }
                        
                    }
                }
            }

            Thread.Sleep(-1);
        }
    }
}
