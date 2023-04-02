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
            public static Boolean connected = false;
            public static string DllFileName = APIFactory.GetAPIinfo().First().Filename;
            public static SAE.J2534.Channel Channel;
            public static bool gmlan = false;
            public static bool vpw = false;
            public static bool Canbus = true;
            public static bool vpw4x = false;
            public static string filterstring="";
            public static bool deviceselected = false;
            public static bool nolog = false;

        }
        [STAThread]
        static void Main()
        {
            LogMenu();
        }

        public static void LogMenu()
        {
            int choice;
            Console.WriteLine("1 - Log data for Engine ECU(7E0,7E8,7DF,101): ");
            Console.WriteLine("2 - Log data for Transmission ECU(7E2,7EA,7DF,101): ");
            Console.WriteLine("3 - Log data for Engine AND Transmission ECU(7E0,7E8,7E2,7EA,7DF,101): ");
            Console.WriteLine("4 - Log data for with ID's you define, ex.7E0/7E2,101");
            Console.WriteLine("5 - Log ALL data");
            if (Debug) Console.WriteLine("6 - Debug enabled, data captured will scroll on screen");
            else Console.WriteLine("6 - Toggle Debug mode(shows data captured on screen, may cause buffer issues with lots of data)");
            Console.WriteLine("7 - Exit");
            if (A.nolog) Console.WriteLine("8 - No log file created, just show on screen. Press 8 to change");
            else Console.WriteLine("8 - Logging to file, Press 8 to show log on screen only");
            if (A.gmlan) Console.WriteLine("9 - Logging GM lan(Low speed pin 1), press 9 to change");
            if (A.vpw) Console.WriteLine("9 - Logging vpw bus(pin 2) press 9 to change");
            if (A.Canbus) Console.WriteLine("9 - Logging High speed(6&14), press 9 to change");
            if (A.vpw4x) Console.WriteLine("9 - Logging vpw 4x bus(pin 2) press 9 to change");
            choice = int.Parse(Console.ReadLine());
            if ((choice < 1 ) | ( choice >9))
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
                        Console.WriteLine("Enter ID headers or search string in messages seperated with commas to be saved, all else will be deleted ");
                        A.filterstring = Console.ReadLine().ToUpper();
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
                        Main();
                        break;
                    }
                case 7:
                    {
                        Environment.Exit(0); // Exit(0);
                        break;
                    }
                case 8:
                    {
                        A.nolog = !A.nolog;
                        LogMenu();
                        break;
                    }
                case 9: // if canbus then gmlan // if gmlan then vpw // if vpw then vpw4x // if vpw4x then canbus
                    {
                        Console.WriteLine(A.Canbus);
                        if (A.Canbus)
                        {
                            Console.WriteLine("canbus turning off");
                            A.Canbus = false;
                            A.gmlan = true;
                        
                            break;
                        }
                        if (A.gmlan) // if gmlan then switch to vpw
                        {
                            A.gmlan = false;
                            A.vpw = true;
                            break;
                          
                        }

                        if (A.vpw)
                        {
                            A.vpw = false;
                            A.vpw4x = true;
                            break;
                           
                        }

                        if (A.vpw4x)
                        {
                            Console.WriteLine($"Canbus={A.Canbus} and vpw4x={A.vpw4x}");
                            A.vpw4x = false; // if vpw then switch to high speed
                            A.Canbus = true;
                            break;
                           
                        }
                        
                        LogMenu();
                        break;
                    }



            }
          
            Main();
        }

        static void Logdata()
        {
            try
            {
               
                Connect();
                if (A.nolog) Nologdata();
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
                            Console.WriteLine("Press the p key to pause log, all monitoring will pause");
                            Console.WriteLine("Press any key to stop logging");
                            Console.WriteLine("starting log in 2 seconds");
                            System.Threading.Thread.Sleep(1000);
                            Console.WriteLine("starting log in 1 second");
                            System.Threading.Thread.Sleep(1000);
                            Console.WriteLine("Starting log");
                            for (; ; )
                            {
                                while (Console.KeyAvailable == false)
                                {

                                    GetMessageResults Logdata = A.Channel.GetMessage();
                                    if (Logdata.Result == 0)
                                    {
                                        String line = BitConverter.ToString(Logdata.Messages[0].Data).Replace("-", "");
                                        if ((!A.gmlan) & (!A.vpw) & (!A.vpw4x)) line = line.Remove(0, 5);
                                        switch (filter) // read line by line, remove the first 5 zeros.. 
                                        {
                                            case 0: // No filter.. allow all
                                                {
                                                    w.WriteLine(line);
                                                    if (Debug) Console.WriteLine(line);
                                                    else Console.WriteLine(l);
                                                    break;
                                                }
                                            case 1:  // Engine
                                                {
                                                    if (line.StartsWith("7E0") || line.StartsWith("7E8") || line.StartsWith("7DF") || line.StartsWith("101"))
                                                    {
                                                        w.WriteLine(line);
                                                        if (Debug) Console.WriteLine(line);
                                                        else Console.WriteLine(l);
                                                    }
                                                    break;
                                                }
                                            case 2:  // Transmission
                                                {
                                                    if (line.StartsWith("7E2") || line.StartsWith("7EA") || line.StartsWith("7DF") || line.StartsWith("101"))
                                                    {
                                                        w.WriteLine(line);
                                                        if (Debug) Console.WriteLine(line);
                                                        else Console.WriteLine(l);
                                                    }
                                                    break;
                                                }
                                            case 3:  // Engine and Transmission
                                                {
                                                    if (line.StartsWith("7E0") || line.StartsWith("7E8") || line.StartsWith("7E2") || line.StartsWith("7EA") || line.StartsWith("7DF") || line.StartsWith("101"))
                                                    {
                                                        w.WriteLine(line);
                                                        if (Debug) Console.WriteLine(line);
                                                        else Console.WriteLine(l);
                                                    }
                                                    break;
                                                }
                                            case 4:
                                                {
                                                    String[] filters = A.filterstring.Split(',');
                                                    foreach (String element in filters)
                                                    {

                                                        if (line.Contains(element))
                                                        {
                                                            w.WriteLine(line);
                                                            if (Debug) Console.WriteLine(line);
                                                            else Console.WriteLine(l);
                                                        }
                                                    }
                                                    break;
                                                }

                                        }
                                        if ((A.vpw) & (line.Contains("6CFEF0A1")))
                                        {
                                            switchto4x();
                                            w.WriteLine("Switching to 4x speed");
                                        }
                                        if ((A.vpw4x) & (line.Contains("6C10F020")))
                                        {
                                            switchto1x();
                                            w.WriteLine("Switching to 1x speed");
                                        }
                                        l++;
                                    }

                                }

                                if (Console.ReadKey().Key != ConsoleKey.P) break;
                                Console.WriteLine("logging is paused");
                                Console.WriteLine("Press any key to restart");
                                Console.ReadKey(); // wait here

                            }


                            Console.WriteLine("finished, your file was saved to " + filePath);
                            Console.WriteLine("Press any key to exit program, please note your log file location");
                            w.Close();
                            Console.ReadKey();        // delay
                            disconnect();
                        }
                    }
                }
            }
                  catch { 
                          Console.WriteLine("Some kind of error, what did you do?");
                          disconnect();
                        }
        }


        static void Nologdata()
        {
            try { 
            Console.WriteLine("Press the p key to pause log, all monitoring will pause");
            Console.WriteLine("Press any key to stop logging");
            Console.WriteLine("starting log in 1 second");
            System.Threading.Thread.Sleep(1000);
            Console.WriteLine("Starting log");
            for (; ; )
            {
                while (Console.KeyAvailable == false)
                {

                    GetMessageResults Logdata = A.Channel.GetMessage();
                    if (Logdata.Result == 0)
                    {
                        String line = BitConverter.ToString(Logdata.Messages[0].Data).Replace("-", "");
                            if ((!A.gmlan) & (!A.vpw) & (!A.vpw4x)) line = line.Remove(0, 5);
                            switch (filter) // read line by line, remove the first 5 zeros.. 
                        {
                            case 0: // No filter.. allow all
                                {
                                    Console.WriteLine(line);
                                    break;
                                }

                            case 1:  // Engine
                                {
                                    if (line.StartsWith("7E0") || line.StartsWith("7E8") || line.StartsWith("7DF") || line.StartsWith("101"))
                                    {
                                        Console.WriteLine(line);
                                    }
                                    break;
                                }
                            case 2:  // Transmission
                                {
                                    if (line.StartsWith("7E2") || line.StartsWith("7EA") || line.StartsWith("7DF") || line.StartsWith("101"))
                                    {
                                        Console.WriteLine(line);
                                    }
                                    break;
                                }
                            case 3:  // Engine and Transmission
                                {
                                    if (line.StartsWith("7E0") || line.StartsWith("7E8") || line.StartsWith("7E2") || line.StartsWith("7EA") || line.StartsWith("7DF") || line.StartsWith("101"))
                                    {
                                        Console.WriteLine(line);
                                    }
                                    break;
                                }
                            case 4:
                                {
                                    String[] filters = A.filterstring.Split(',');
                                    foreach (String element in filters)
                                    {

                                        if (line.Contains(element))
                                        {
                                            Console.WriteLine(line);
                                        }
                                    }
                                    break;
                                }

                            } // end of filter
                            if ((A.vpw) & (line.Contains("6CFEF0A1"))) switchto4x();
                            if ((A.vpw4x) & (line.Contains("6C10F020"))) switchto1x();
                        }

                }

                if (Console.ReadKey().Key != ConsoleKey.P) break;
                Console.WriteLine("logging is paused");
                Console.WriteLine("Press any key to restart");
                Console.ReadKey(); // wait here

            }



            Console.WriteLine("Press any key to exit program");
            Console.ReadKey();        // delay
            disconnect();
            LogMenu();
        }
            catch
            {
                Console.WriteLine("Some kind of error, what did you do?");
                disconnect();
            }


        }
        public static void disconnect()
        {
            APIFactory.StaticDispose();
            A.connected = false;
        }

        public static void SelectJ2534() // public static string DllFileName = APIFactory.GetAPIinfo().First().Filename;
        {
            for (int c = 1; c <= APIFactory.GetAPIinfo().Count(); c++)
            {
                Console.WriteLine(c + " - " + APIFactory.GetAPIinfo().ElementAt(c - 1).Name);

            }

            Console.Write("\nInput Device Number:");
            int index ;

            try { index = Convert.ToInt32(Console.ReadLine()); }
            catch { index = 1; }
            if ((index > APIFactory.GetAPIinfo().Count()) | (index < 1)) index = 1;

            A.DllFileName = APIFactory.GetAPIinfo().ElementAt(index - 1).Filename;
            A.deviceselected = true;
        }

        public static void switchto4x()
        {
            disconnect();
            Console.WriteLine("switching to 4x");
            
            A.vpw = false;
            A.vpw4x = true;
            
            Connect();
        }

        public static void switchto1x()
        {
            disconnect();
            Console.WriteLine("switching to 1x");
            
            A.vpw = true;
            A.vpw4x = false;
            Connect();
        }
        public static void Connect()
        {
            try
            {
                if (!A.deviceselected) SelectJ2534();
                if (A.vpw) A.Channel = APIFactory.GetAPI(A.DllFileName).GetDevice().GetChannel(Protocol.J1850VPW, Baud.J1850VPW, ConnectFlag.NONE);
                if (A.gmlan)
                {
                    A.Channel = APIFactory.GetAPI(A.DllFileName).GetDevice().GetChannel(Protocol.SW_CAN_PS, Baud.CAN_33333, ConnectFlag.NONE);
                    A.Channel.SetConfig(Parameter.J1962_PINS, 256);
                }
                if (A.Canbus) A.Channel = APIFactory.GetAPI(A.DllFileName).GetDevice().GetChannel(Protocol.CAN, Baud.CAN, ConnectFlag.CAN_29BIT_ID);
                if (A.vpw4x) A.Channel = APIFactory.GetAPI(A.DllFileName).GetDevice().GetChannel(Protocol.J1850VPW, (Baud) 41600, ConnectFlag.NONE);

                A.Channel.StartMsgFilter(new MessageFilter(UserFilterType.PASSALL, new byte[] { 0, 0 }));
                A.Channel.DefaultTxTimeout = 350;
                A.Channel.DefaultRxTimeout = 500;
                A.connected = true;
            }

            catch (Exception e)
            {
                Console.WriteLine("Error Connecting to MDI");
                Console.WriteLine("Exception Error source: {0}", e.Source);
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
                A.connected = false;
                APIFactory.StaticDispose(); // disconnect MDI
                Main(); //  Environment.Exit(0); // Exit(0);
            }



        } // end of Connect
    }
}
