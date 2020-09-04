using System;
using System.IO;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text;
using SAE.J2534;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Linq.Expressions;
using System.Diagnostics.Eventing.Reader;

/*  This is used monitor the high speed canbus of gm vehicles...  
 *  using dll from https://github.com/BrianHumlicek/J2534-Sharp
 *  Copyright(c) 2018, Brian Humlicek https://github.com/BrianHumlicek
 *  Simple to use, instructions are in the program and guide you as it runs
 *  it's a simple command line program
 *  
 * 
 * 
 */

namespace MDI_Canbus_Logger
{
    static class Program
    {
        static int filter;
        static Boolean Debug;
        static class A
        {
            public static byte[] data = new byte[2097152];
            public static byte[] b = new byte[2097152];
            public static Boolean _isconnected = false;
            public static string DllFileName = APIFactory.GetAPIinfo().First().Filename;
            public static SAE.J2534.Channel Channel;

        }
        [STAThread]
        static void Main()
        {
            LogMenu();
        }

        public static void LogMenu()
        {
            int choice;

            //  int Checksumfilesize;

            StringComparer stringComparer = StringComparer.OrdinalIgnoreCase;
            //  Thread readThread = new Thread(Read);


            //  _continue = true;
            //  readThread.Start();

            Console.WriteLine("1 - Log data for Engine ECU(7E0,7E8,7DF,101): ");
            Console.WriteLine("2 - Log data for Transmission ECU(7E2,7EA,7DF,101): ");
            Console.WriteLine("3 - Log data for Transmission ECU(7E2,7EA,7DF,101): ");
            Console.WriteLine("4 - Log data for BCM(7EB,641,7DF,101");
            Console.WriteLine("5 - Log ALL data");
            if (Debug) Console.WriteLine("6 - Debug enabled, data captured will scroll on screen");
            else Console.WriteLine("6 - Toggle Debug mode(shows data captured on screen, may cause buffer issues with lots of data)");
            Console.WriteLine("7 - Exit");
            choice = int.Parse(Console.ReadLine());
            if ((choice < 1 ) | ( choice >7))
            {
                Console.WriteLine("Please select one of the choices");
                Main();

            }
            switch (choice)
            {
                case 1:
                    {
                        filter = 1;
                        Logdata();
                        break;
                    }
                case 2:
                    {
                        filter = 2;
                        Logdata();
                        break;
                    }
                case 3:
                    {
                        filter = 3;
                        Logdata();
                        break;
                    }
                case 4:
                    {
                        filter = 4;
                        Logdata();
                        break;
                    }
                case 5:
                    {
                        filter = 0;
                        Logdata();
                        break;
                    }
                case 6:
                    {
                        Debug = !Debug;
                        
                        break;
                    }
                case 7:
                    {
                        Environment.Exit(0); // Exit(0);
                        break;
                    }



            }
            // Keep the console open in debug mode.
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();        
                                      
            Main();
        }

        static void Logdata()
        {
            Connect();
            //  System.Threading.Thread.Sleep(40);
            //// now its connected.. now what?? Can I read messages and wait to see a certain message? then read and log all messages?
            var filePath = string.Empty;
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.InitialDirectory = "c:\\";
                saveFileDialog.Filter = "txt files (*.txt)|*.txt";
                saveFileDialog.FilterIndex = 2;
                saveFileDialog.RestoreDirectory = true;
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    filePath = saveFileDialog.FileName;
                    File.Delete(filePath);
                    using (StreamWriter w = File.AppendText(filePath))
                    {
                        int l = 0;
                        Console.WriteLine("Press any key to stop logging");
                        Console.WriteLine("starting log in 2 seconds");
                        System.Threading.Thread.Sleep(1000);
                        Console.WriteLine("starting log in 1 second");
                        System.Threading.Thread.Sleep(1000);
                        Console.WriteLine("Starting log");

                        while (Console.KeyAvailable == false)
                        {
                            GetMessageResults Logdata = A.Channel.GetMessage();
                            if (Logdata.Result == 0)
                            {
                                String line = BitConverter.ToString(Logdata.Messages[0].Data).Replace("-", "").Remove(0, 5);
                                switch (filter)
                                {
                                    case 0:

                                        {
                                            w.WriteLine(line);
                                            if (Debug) Console.WriteLine(line);
                                            Console.WriteLine(l);
                                            break;
                                        }
                                    case 1:
                                        {
                                            if (line.StartsWith("7E0") || line.StartsWith("7E8") || line.StartsWith("7DF") || line.StartsWith("101"))
                                            {
                                                w.WriteLine(line);
                                                if (Debug) Console.WriteLine(line);
                                                Console.WriteLine(l);
                                            }
                                            break;
                                        }
                                    case 2:
                                        {
                                            if (line.StartsWith("7E2") || line.StartsWith("7EA") || line.StartsWith("7DF") || line.StartsWith("101"))
                                            {
                                                w.WriteLine(line);
                                                if (Debug) Console.WriteLine(line);
                                                Console.WriteLine(l);
                                            }
                                            break;
                                        }
                                    case 3:
                                        {
                                            if (line.StartsWith("7E0") || line.StartsWith("7E8") || line.StartsWith("7E2") || line.StartsWith("7EA") || line.StartsWith("7DF") || line.StartsWith("101"))
                                            {
                                                w.WriteLine(line);
                                                if (Debug) Console.WriteLine(line);
                                                Console.WriteLine(l);
                                            }
                                            break;
                                        }
                                    case 4:
                                        {
                                            if (line.StartsWith("7DF") || line.StartsWith("101") || line.StartsWith("7EB") || line.StartsWith("641"))
                                            {
                                                w.WriteLine(line);
                                                if (Debug) Console.WriteLine(line);
                                                Console.WriteLine(l);
                                            }
                                            break;
                                        }
                                }
                                
                                l++;
                            }
                           
                        }
                        Console.WriteLine("finished, your file was saved to " + filePath);
                        Console.WriteLine("Press any key to exit program, please note your log file location");
                        
                        w.Close();
                        Console.ReadKey();        // delay

                        APIFactory.StaticDispose();

                    }
                }
            }
        }

        public static void Connect()
        {
            try
            {
                A.Channel = APIFactory.GetAPI(A.DllFileName).GetDevice().GetChannel(Protocol.CAN, Baud.CAN, ConnectFlag.CAN_29BIT_ID);
                A.Channel.StartMsgFilter(new MessageFilter(UserFilterType.PASSALL, new byte[] { 0x00, 0x00, 0x07, 0xE8 }));
                A.Channel.DefaultTxTimeout = 350;
                A._isconnected = true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception Error source: {0}", e.Source);
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
                Environment.Exit(0); // Exit(0);
            }

        }

    }
}
