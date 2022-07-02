using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Timers;
using System.Data.SqlClient;
using System.Data.Entity;
using System.Configuration.Install;
using System.Threading;
using System.Timers;
using System.Net.NetworkInformation;

namespace WindowsService2
{
    public partial class Service1 : ServiceBase
    {
        public Service1()
        {
            InitializeComponent();
        }


        string serviceName = "Service1";
        int timeoutMilliseconds = 10;


        public string connectionstring = @"Data Source = DESKTOP-SCRM1QR\SQLEXPRESS01 ; Initial Catalog = DosyaOkumaDeneme ; User ID = sa ; Password = 1234";  //database connection



        protected override void OnStart(string[] args)  // when service started
        {
            string path = @"C:\Users\zehra\AppData\Roaming\Thonny\user_logs";

            FileSystemWatcher watcher = new FileSystemWatcher(path);
            watcher.Created += Watcher_Created;  // when a new file created in that path
            watcher.EnableRaisingEvents = true;

        }


        public static void StopService(string serviceName, int timeoutMilliseconds)  // stops the service in every 10 milliseconds ( its triggered by another service to keep it running)
        {
            ServiceController service = new ServiceController(serviceName);
            TimeSpan timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);
            service.Stop();
            service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
        }

        private static int sayac = 0;


        private void Watcher_Created(object sender, FileSystemEventArgs e) // when a new file created at specified path
        {

            string json = File.ReadAllText(e.FullPath);
            JArray o1 = JArray.Parse(json);

            string[] ayrac = { "time" };

            string[] parcalar = json.Split(ayrac, StringSplitOptions.RemoveEmptyEntries); //splitting json with the word: 'time' and assigning to array: 'parçalar'

            int count = parcalar.Length - 1;
            int errors = 0;

            string time = "";
            string kelime = "stderr";  // if cell contains the phrase 'stderr'
            string kelime2 = "Error";  //to count errors

            string[] verilerStr = new string[count - 1];
            string filename = (string)o1[3]["filename"];


            int ii = 0;

            int i = 0;


            while (ii < count)
            {
                string text1 = (string)o1[ii]["text"];
                string time1 = (string)o1[ii]["time"] + "\n";
                string filename1 = (string)o1[3]["filename"] + "\n";

                if (text1 != null && text1.Contains(kelime2))   //if the 'text' is not null and contains the phrase 'stderr'; get the value of file name, text and time.
                {
                    errors++;  // counting error
                    File.AppendAllText(@"C:\Users\zehra\AppData\Roaming\Thonny\filename.txt", filename1);  //writing file name value to the file 
                    File.AppendAllText(@"C:\Users\zehra\AppData\Roaming\Thonny\text.txt", text1);         //writing text value to the file
                    File.AppendAllText(@"C:\Users\zehra\AppData\Roaming\Thonny\time.txt", time1);         //writing time value to the file
                    ii++;
                }
                else
                {
                    ii++;
                }
            }
            File.WriteAllText(@"C:\Users\zehra\AppData\Roaming\Thonny\errorsayisi.txt", errors.ToString());  //writing the number of errors to the file

            string line;
            string line1;
            string line2;


            using (SqlConnection con = new SqlConnection(@"Data Source = DESKTOP-SCRM1QR\SQLEXPRESS01 ; Initial Catalog = DosyaOkumaDeneme ; User ID = sa ; Password = 1234"))
            {

                //getting the mac adress of the computer
                var macAddr =
                (
                    from nic in NetworkInterface.GetAllNetworkInterfaces()
                    where nic.OperationalStatus == OperationalStatus.Up
                    select nic.GetPhysicalAddress().ToString()
                    ).FirstOrDefault();
                con.Open();

                using (StreamReader file = new StreamReader(@"C:\Users\zehra\AppData\Roaming\Thonny\filename.txt"))  //first part
                {
                    using (StreamReader file1 = new StreamReader(@"C:\Users\zehra\AppData\Roaming\Thonny\time.txt"))   //second part
                    {
                        using (StreamReader file2 = new StreamReader(@"C:\Users\zehra\AppData\Roaming\Thonny\text.txt"))  //third part
                        {
                            while ((line = file.ReadLine()) != null && (line1 = file1.ReadLine()) != null && (line2 = file2.ReadLine()) != null)
                            {
                                string[] fields = line.Split('\n');
                                string[] fields1 = line1.Split('\n');
                                string[] fields2 = line2.Split('\n');

                                SqlCommand cmd = new SqlCommand("INSERT INTO Dosya( DosyaYolu, Tarih, mesaj, mac ) VALUES ( @dy, @tr, @msj, @mac )", con); // inserting data to the database
                                cmd.Parameters.AddWithValue("@dy", fields[0].ToString());
                                cmd.Parameters.AddWithValue("@tr", fields1[0].ToString());
                                cmd.Parameters.AddWithValue("@msj", fields2[0].ToString());
                                cmd.Parameters.AddWithValue("@mac", macAddr.ToString());
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }

            string[] TextStr = new string[errors];
            string[] TimeStr = new string[errors];

            while (i < count)
            {
                string tags = (string)o1[i]["tags"];

                if (tags != null && tags.Contains(kelime))
                {
                    time = (string)o1[i]["time"];

                    JObject veriler = new JObject(
                    new JProperty("{0}. satır:", i),
                    new JProperty("filename:", filename),
                    new JProperty("Text:", (string)o1[i]["text"]),
                    new JProperty("Tags:", (string)o1[i]["tags"]),
                    new JProperty("Time:", (string)o1[i]["time"]));


                    verilerStr[i] = veriler.ToString();

                    File.WriteAllLines(@"C:\Users\zehra\AppData\Roaming\Thonny\veriler.txt", verilerStr);

                    string text = (string)o1[i]["text"];

                    i++;
                }
                else
                {
                    i++;
                }
            }
            //cleaning the files
            TextWriter tw = new StreamWriter(@"C:\Users\muhammed\AppData\Roaming\Thonny\text.txt");
            tw.Write(""); 
            tw.Close();
            TextWriter tw1 = new StreamWriter(@"C:\Users\muhammed\AppData\Roaming\Thonny\time.txt");
            tw1.Write("");
            tw1.Close();
            TextWriter tw2 = new StreamWriter(@"C:\Users\muhammed\AppData\Roaming\Thonny\filename.txt");
            tw2.Write("");
            tw2.Close();
            StopService(serviceName, timeoutMilliseconds);  //stopping the service
        }

        protected override void OnStop()
        {

        }
    }
}
