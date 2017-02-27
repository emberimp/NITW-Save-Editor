using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NITW_Save_Editor
{
    public partial class Form1 : Form
    {
        public Dictionary<string, float> vars = new Dictionary<string, float>();
        public Dictionary<string, string> stringVars = new Dictionary<string, string>();
        public Dictionary<string, int> persistentInts = new Dictionary<string, int>();

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Guid localLowId = new Guid("A520A1A4-1780-4FF6-BD18-167343C5AF16");
            txtPath.Text = GetKnownFolderPath(localLowId) + @"\Infinite Fall\Night in the Woods\player.dat";
        }

        string GetKnownFolderPath(Guid knownFolderId)
        {
            IntPtr pszPath = IntPtr.Zero;
            try
            {
                int hr = SHGetKnownFolderPath(knownFolderId, 0, IntPtr.Zero, out pszPath);
                if (hr >= 0)
                    return Marshal.PtrToStringAuto(pszPath);
                throw Marshal.GetExceptionForHR(hr);
            }
            finally
            {
                if (pszPath != IntPtr.Zero)
                    Marshal.FreeCoTaskMem(pszPath);
            }
        }

        [DllImport("shell32.dll")]
        static extern int SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint dwFlags, IntPtr hToken, out IntPtr pszPath);

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            openFileDialog1.FileName = "player.dat";
            openFileDialog1.InitialDirectory = System.IO.Path.GetDirectoryName(txtPath.Text);
            DialogResult result = openFileDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                txtPath.Text = openFileDialog1.FileName;

                string file = Path.GetFileName(openFileDialog1.FileName);
                if (file == "player.dat")
                {
                    radioPlayer.Checked = true;
                    btnLoad.PerformClick();
                } else if(file == "persist.dat")
                {
                    radioPersistent.Checked = true;
                    btnLoad.PerformClick();
                }
            }
        }

        private void resetDataGridView()
        {
            this.dataGridView1.Rows.Clear();
            this.dataGridView1.Refresh();
            this.dataGridView1.Columns["columnValInt"].Visible = true;
            this.dataGridView1.Columns["columnValFloat"].Visible = true;
            this.dataGridView1.Columns["columnValString"].Visible = true;
            this.dataGridView1.Columns["columnValString"].ReadOnly = true;
        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            this.statusLabel.Text = "Opening file...";
            this.dataGridView1.Visible = false;

            resetDataGridView();
            
            FileStream fileStream = null;
            try
            {
                fileStream = new FileStream(txtPath.Text, FileMode.Open);
            }
            catch
            {
                MessageBox.Show("Failed to open file");
            }
            if (fileStream != null)
            {
                try
                {
                    BinaryFormatter binaryFormatter = new BinaryFormatter();
                    this.statusLabel.Text = "Reading values...";
                    if (radioPlayer.Checked)
                    {                        
                        this.vars = (Dictionary<string, float>)binaryFormatter.Deserialize(fileStream);
                        this.stringVars = (Dictionary<string, string>)binaryFormatter.Deserialize(fileStream);

                        DataGridViewCellStyle defaultStyle = new DataGridViewCellStyle();
                        defaultStyle.BackColor = Color.White;
                        DataGridViewCellStyle readOnlyStyle = new DataGridViewCellStyle();
                        readOnlyStyle.BackColor = Color.LightGray;

                        this.dataGridView1.Columns["columnValInt"].Visible = false;

                        this.statusLabel.Text = "Adding floats to data grid...";
                        foreach (KeyValuePair<string, float> entry in this.vars)
                        {
                            this.dataGridView1.Rows.Add(entry.Key, entry.Value);
                        }

                        this.statusLabel.Text = "Adding strings to data grid...";
                        foreach (KeyValuePair<string, string> entry in this.stringVars)
                        {
                            this.dataGridView1.Rows.Add(entry.Key, "", "", entry.Value);

                            this.dataGridView1.Rows[dataGridView1.RowCount - 1].Cells["columnValString"].ReadOnly = false;
                            this.dataGridView1.Rows[dataGridView1.RowCount - 1].Cells["columnValString"].Style = defaultStyle;
                            
                            this.dataGridView1.Rows[dataGridView1.RowCount - 1].Cells["columnValFloat"].ReadOnly = true;
                            this.dataGridView1.Rows[dataGridView1.RowCount - 1].Cells["columnValFloat"].Style = readOnlyStyle;
                        }
                    }
                    else
                    {
                        this.persistentInts = (Dictionary<string, int>)binaryFormatter.Deserialize(fileStream);

                        this.dataGridView1.Columns["columnValFloat"].Visible = false;
                        this.dataGridView1.Columns["columnValString"].Visible = false;

                        this.statusLabel.Text = "Adding integers to data grid...";
                        foreach (KeyValuePair<string, int> entry in this.persistentInts)
                        {
                            this.dataGridView1.Rows.Add(entry.Key, "", entry.Value);
                        }
                    }
                }
                catch (SerializationException ex)
                {
                    MessageBox.Show("Failed to deseralize: \n" + ex.Message);
                    throw;
                }
                finally
                {
                    dataGridView1.Visible = true;
                    fileStream.Close();
                }
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            dataGridView1.Visible = false;
            FileStream fileStream = new FileStream(txtPath.Text, FileMode.Create);
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            try
            {
                this.statusLabel.Text = "Updating dictionary values...";
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    string key = (string)row.Cells["columnKey"].Value;
                    if (row.Cells["columnValFloat"].ReadOnly == false && row.Cells["columnValFloat"].Visible == true)
                    {
                        this.vars[key] = Convert.ToSingle(row.Cells["columnValFloat"].Value);
                    }
                    else if (row.Cells["columnValString"].ReadOnly == false && row.Cells["columnValString"].Visible == true)
                    {
                        this.stringVars[key] = (string)row.Cells["columnValString"].Value;
                    }
                    else if (row.Cells["columnValInt"].ReadOnly == false && row.Cells["columnValInt"].Visible == true)
                    {
                        this.persistentInts[key] = Convert.ToInt32(row.Cells["columnValInt"].Value);
                    }
                }

                this.statusLabel.Text = "Writing values to file...";
                if (radioPlayer.Checked)
                {
                    binaryFormatter.Serialize(fileStream, this.vars);
                    binaryFormatter.Serialize(fileStream, this.stringVars);
                }
                else
                {
                    binaryFormatter.Serialize(fileStream, this.persistentInts);
                }
            }
            catch (SerializationException ex)
            {
                MessageBox.Show("Failed to seralize: \n" + ex.Message);
                throw;
            }
            finally
            {
                resetDataGridView();
                dataGridView1.Visible = true;
                fileStream.Close();
            }
        }
    }
}
