using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Alturos.Yolo;
using System.Drawing.Imaging;
using System.IO;
using System.Diagnostics;

namespace Object_recognition
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            populateCmb(cmbModels, "*.names");
            populateCmb(cmbPretained, "*.cfg");
        }

        /**
         * Button LoacPic click listener
         **/
        private void btnLoadPic_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog op = new OpenFileDialog() { Filter = "JPEG|*.jpg" }) // file type
            {
                if (op.ShowDialog() == DialogResult.OK)
                {
                    picResult.Image = Image.FromFile(op.FileName); // load image
                }
            }
        }

        /**
         * Button add
         **/    
        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (cmbModels.Text != "")
            {
                if (lstData.FindString(cmbModels.Text, -1) == -1)
                {
                    lstData.Items.Add(cmbModels.Text);
                }
            }
        }

        /**
         * Button Clear
         **/
        private void btnClear_Click(object sender, EventArgs e)
        {
            lstData.Items.Clear();
        }

        /**
         * Button Execute
         **/
        private void btnExecute_Click(object sender, EventArgs e)
        {
            saveVOCName();
            executeYOLO();
        }

        /**
         *  Write on voc.names file
         **/
        private void saveVOCName()
        {
            StreamWriter saveFile = new StreamWriter("voc.names");
            foreach (var item in lstData.Items)
            {
                saveFile.WriteLine(item.ToString());
            }
            saveFile.Close();
        }

        private void loadVOCName()
        {
            lstData.Items.Clear();
            string line = "";
            StreamReader loadFile = new StreamReader(cmbModels.Text);
            while ((line = loadFile.ReadLine()) != null)
            {
                lstData.Items.Add(line);
            }
            loadFile.Close();
        }

        private void executeYOLO()
        {
            var configDetect = new ConfigurationDetector();
            var config = configDetect.Detect();
            var sw = new Stopwatch(); // To check runtime
            sw.Start();

            using (var yoloWrap = new YoloWrapper(cmbPretained.Text, cmbPretained.Text.Replace(".cfg", "") + ".weights", cmbModels.Text)) // kalau ada error, check library
                                                                                                                                          // using(var yoloWrap = new YoloWrapper(config)) // kalau ada error, check library
                                                                                                                                          // using(var yoloWrap = new YoloWrapper("yolov3.cfg", "yolov3.weights", "coco.names")) // kalau ada error, check library
            {
                using (MemoryStream mem = new MemoryStream())
                {
                    picResult.Image.Save(mem, ImageFormat.Png);
                    var items = yoloWrap.Detect(mem.ToArray()); // Detect
                    dgTrackedObject.DataSource = items; // Each attribute of the detected object are stored in dgTrackedObject
                    drawRectangle();
                }
            }

            sw.Stop();
            lblStatus.Text = sw.Elapsed.TotalMilliseconds.ToString() + " ms";
            prgStatus.Value = 100;
        }

        /**
         *  Show semua file yang *.names / *.cfg
         **/
        private void populateCmb(ComboBox combo, string fileType)
        {
            DirectoryInfo dInfo = new DirectoryInfo(Path.GetDirectoryName(Application.ExecutablePath));
            FileInfo[] fileList = dInfo.GetFiles(fileType);
            combo.Items.Clear();
            foreach (FileInfo file in fileList)
            {
                combo.Items.Add(file.Name);
            }
        }

        // dekat cmbModels, cari selected index change, double click
        private void cmbModels_SelectedIndexChanged(object sender, EventArgs e)
        {
            loadVOCName();
        }

        private void drawRectangle()
        {
            var img = picResult.Image;

            using (var canvas = Graphics.FromImage(img))
            {
                for (int i = 0; i < dgTrackedObject.Rows.Count; i++)
                {
                    var type = dgTrackedObject.Rows[i].Cells[0].Value;
                    var conf = dgTrackedObject.Rows[i].Cells[1].Value;
                    var x = dgTrackedObject.Rows[i].Cells[2].Value;
                    var y = dgTrackedObject.Rows[i].Cells[3].Value;
                    var width = dgTrackedObject.Rows[i].Cells[4].Value;
                    var height = dgTrackedObject.Rows[i].Cells[5].Value;

                    using (var oy = new SolidBrush(Color.FromArgb(150, 255, 255, 102)))
                    using (var pen = getBrush(Convert.ToDouble(conf), Convert.ToInt32(width)))
                    {
                        // Draw rectangle
                        Rectangle rect;
                        rect = new Rectangle(Convert.ToInt32(x), Convert.ToInt32(y), Convert.ToInt32(width), Convert.ToInt32(height));
                        canvas.DrawRectangle(pen, rect);
                    }
                }
            }
            picResult.Image = img;
        }

        private Pen getBrush(double conf, int width)
        {
            var size = width / 100;
            if (conf > 0.5)
            {
                return new Pen(Brushes.Purple, size);
            }
            else if (conf < 0.3)
            {
                return new Pen(Brushes.Red, size);
            }

            return new Pen(Brushes.LightGreen, size);

        }
    }
}
