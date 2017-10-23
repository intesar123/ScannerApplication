using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;
using WIATest;

namespace Scannerapplication
{
    public partial class Form1 : Form
    {
        private const int BufferSize = 1024;
        public string hostip = string.Empty;
        public string hostport = string.Empty;
        public string dbserver = string.Empty;
        public string dbuser = string.Empty;
        public string dbpassword = string.Empty;
        public string dbname = string.Empty;
        public string tablename = string.Empty;
        public string fieldname = string.Empty;
        public string keyfield = string.Empty;
        public string keyvalue = string.Empty;
        public int isinsert = 0; 
        public int isdb = 0;
        public Image ImageScanned=null;

        public Form1()
        {
            InitializeComponent();

        }
        public void loadSetting()
        {

            try
            {
                string[] arguments = Environment.GetCommandLineArgs();
                foreach (string str in arguments)
                {
                    if(str.Contains("OpenForm"))
                    {
                        string[] strarr = str.Split('=');
                        if(strarr.Length>0)
                        {
                            keyvalue = strarr[1].ToString();
                           // MessageBox.Show("Key Value"+ keyvalue);
                        }
                    }
                    //MessageBox.Show(str);
                    //keyvalue = "111";
                }
                string path = Path.Combine(Application.StartupPath, "hostsetting.txt");
                if (File.Exists(path))
                {
                    using (StreamReader sr = File.OpenText(path))
                    {
                        string s = "";
                        while ((s = sr.ReadLine()) != null)
                        {
                            string[] strarr = s.Split('|');
                            if (strarr.Length >= 2)
                            {
                                hostip = strarr[0];
                                hostport = strarr[1];
                                dbserver = strarr[2];
                                dbname = strarr[3];
                                dbuser = strarr[4];
                                dbpassword = strarr[5];
                                isdb = Convert.ToInt32(strarr[6]);
                                tablename = strarr[7];
                                fieldname = strarr[8];
                                keyfield = strarr[9];
                                isinsert = Convert.ToInt32(strarr[6]);
                                label1.Text = "Host: " + hostip;
                                label2.Text = "Port: " + hostport;
                                int port = 0;
                                if (hostport.Length > 0 && hostip.Length > 0)
                                {
                                    try
                                    {
                                        port = Convert.ToInt32(hostport);
                                        bool issend = sendData(hostip, port, path);
                                        if (issend)
                                        {
                                            Application.Exit();
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        MessageBox.Show("Message : " + ex.ToString());
                                    }

                                }
                                else
                                {
                                    MessageBox.Show("Host IP or Port not found");
                                }

                            }
                            else
                            {
                                MessageBox.Show("Setting Parameters are not correct: " + s);
                            }
                        }
                    }

                }
                else
                {
                    MessageBox.Show("Host Setting Not Found at Location: " + path);
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString());
            }
        }
        public bool sendData(string hostaddr, int portaddr, string path)
        {
            string pathfile = Path.Combine(Path.GetDirectoryName(path), "scannedimg.jpeg");
        
            if(isdb==1)
            {
                UpdateScannedImage(ImageScanned );
            }
            else
            {
                ImageScanned.Save(pathfile);
                if (File.Exists(pathfile))
                {
                    SendTCP(pathfile, hostaddr, portaddr);
                }
                else
                {
                    MessageBox.Show("Unable to Send to TCP: " + pathfile);
                    return false;
                }
            }
            return true;
        }
        public void SendTCP(string M, string IPA, Int32 PortN)
        {

            
                byte[] SendingBuffer = null;
                TcpClient client = null;
                lblStatus.Text = "";
                NetworkStream netstream = null;
                try
                {
                    client = new TcpClient(IPA, PortN);
                    lblStatus.Text = "Connected to the Server...\n";
                    netstream = client.GetStream();
                    FileStream Fs = new FileStream(M, FileMode.Open, FileAccess.Read);
                    int NoOfPackets = Convert.ToInt32(Math.Ceiling(Convert.ToDouble(Fs.Length) / Convert.ToDouble(BufferSize)));
                    progressBar1.Maximum = NoOfPackets;
                int TotalLength = (int)Fs.Length, CurrentPacketLength;
                    for (int i = 0; i < NoOfPackets; i++)
                    {
                        if (TotalLength > BufferSize)
                        {
                            CurrentPacketLength = BufferSize;
                            TotalLength = TotalLength - CurrentPacketLength;
                        }
                        else
                            CurrentPacketLength = TotalLength;
                        SendingBuffer = new byte[CurrentPacketLength];
                        Fs.Read(SendingBuffer, 0, CurrentPacketLength);
                        netstream.Write(SendingBuffer, 0, (int)SendingBuffer.Length);
                        if (progressBar1.Value >= progressBar1.Maximum)
                            progressBar1.Value = progressBar1.Minimum;
                        progressBar1.PerformStep();
                    }

                    lblStatus.Text = lblStatus.Text + "Sent " + Fs.Length.ToString() + " bytes to the server";
                    Fs.Close();
                    netstream.Close();
                    client.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                finally
                {


                }

            
            
        }

        public bool UpdateScannedImage(Image img)
        {
            if(keyvalue.Length==0)
            {
                MessageBox.Show("Unable to get Key Field Value.");
                return false;
            }
            byte[] imgdata = getImage(img);
            String str = "Data Source="+dbserver+";Initial Catalog="+dbname+";Integrated Security=False;User Id="+dbuser+";Password="+dbpassword+";MultipleActiveResultSets=True";
            String query = "";
            if(isinsert==1)
            {
                query = "insert into " + tablename + " ("+ keyfield + ","+fieldname+") values(@" + keyfield + ",@" + fieldname +")";
            }
            else
            {
                query = "Update " + tablename + " set " + fieldname + "=@" + fieldname + " where " + keyfield + "='" + keyvalue + "'";

            }
            try
            {
                SqlConnection con = new SqlConnection(str);
                con.Open();
                
                SqlCommand cmd = new SqlCommand(query, con);
                if(isinsert == 1)
                {
                    cmd.Parameters.Add(new SqlParameter("@" + keyfield, keyvalue));
                }
                cmd.Parameters.Add(new SqlParameter("@"+ fieldname, imgdata));
                int row=cmd.ExecuteNonQuery();
                con.Close();
                if (row==0)
                {
                    throw new Exception("Unable to Save to DataBase.");
                }
                return true;
            }
            catch (Exception es)
            {
                MessageBox.Show(es.Message);
            }

            return false;
        }
public byte[] getImage(Image img)
{
    ImageConverter converter = new ImageConverter();
    return (byte[])converter.ConvertTo(img, typeof(byte[]));
}
//button click event
private void btn_scan_Click(object sender, EventArgs e)
{
    try
    {


        //get list of devices available
        List<string> devices = WIAScanner.GetDevices();

        foreach (string device in devices)
        {
            lbDevices.Items.Add(device);
        }
        //check if device is not available
        if (lbDevices.Items.Count == 0)
        {
            MessageBox.Show("You do not have any WIA devices.");
            DialogResult dr = MessageBox.Show("Would you like to Capture Screen?", "Alert", MessageBoxButtons.YesNo);
            if (dr == DialogResult.Yes)
            {
                CaptureScrn();
                loadSetting();
                return;
            }
            else
            {
                Application.Exit();
            }
        }
        else
        {
            lbDevices.SelectedIndex = 0;
        }
        //get images from scanner
        List<Image> images = WIAScanner.Scan((string)lbDevices.SelectedItem);
        foreach (Image image in images)
        {
            pic_scan.Image = image;
                    ImageScanned = image;
            pic_scan.Show();
            pic_scan.SizeMode = PictureBoxSizeMode.StretchImage;
            //save scanned image into specific folder
            // image.Save(@"D:\" + DateTime.Now.ToString("yyyy-MM-dd HHmmss") + ".jpeg", ImageFormat.Jpeg);
        }
        loadSetting();
    }
    catch (Exception exc)
    {
        MessageBox.Show(exc.Message);
    }
}


private void Home_SizeChanged(object sender, EventArgs e)
{
    int pheight = this.Size.Height - 153;
    pic_scan.Size = new Size(pheight - 150, pheight);
}

private void pnl_capture_Paint(object sender, PaintEventArgs e)
{

}

private void Form1_Load(object sender, EventArgs e)
{
    btn_scan_Click(null, null);

}

private void button1_Click(object sender, EventArgs e)
{
    this.WindowState = FormWindowState.Minimized;
    if (this.WindowState == FormWindowState.Minimized)
    {
        CaptureScrn();
    }

}
public void CaptureScrn()
{
    try
    {
        //Creating a new Bitmap object
        Bitmap captureBitmap = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height, PixelFormat.Format32bppArgb);

        //Bitmap captureBitmap = new Bitmap(int width, int height, PixelFormat);
        //Creating a Rectangle object which will  
        //capture our Current Screen
        Rectangle captureRectangle = Screen.AllScreens[0].Bounds;

        //Creating a New Graphics Object
        Graphics captureGraphics = Graphics.FromImage(captureBitmap);

        //Copying Image from The Screen
        captureGraphics.CopyFromScreen(captureRectangle.Left, captureRectangle.Top, 0, 0, captureRectangle.Size);

        //Saving the Image File (I am here Saving it in My E drive).

        //captureBitmap.Save(@"E:\Capture.jpg", ImageFormat.Jpeg);

        pic_scan.InitialImage = null;
        pic_scan.Image = (Image)captureBitmap;
                ImageScanned= (Image)captureBitmap;
                pic_scan.Show();
        pic_scan.SizeMode = PictureBoxSizeMode.StretchImage;
        this.WindowState = FormWindowState.Maximized;
    }
    catch (Exception ex)
    {
        MessageBox.Show(ex.ToString());
    }
    //Console.WriteLine("DEBUG: Server--> Entra nel While");
    //ScreenCapture sc = new ScreenCapture(); // capture entire screen
    //img = sc.CaptureScreen();
    //img1 = (Image)img.Clone();
    //padre.setImage(img1);
    //if (img != null) //If you choosed an image
    //{
    //videoServer.SendImage(img); //Send the image


}
    }
}
