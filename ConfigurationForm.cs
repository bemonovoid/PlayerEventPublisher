using System;
using System.Windows.Forms;
using static MusicBeePlugin.Plugin;

namespace MusicBeePlugin
{
    public partial class ConfigurationForm : Form
    {
        public ConfigurationForm()
        {
            InitializeComponent();
        }

        private void ConfigurationForm_Load(object sender, EventArgs e)
        {

        }

        private void testConnectionBtn_Click(object sender, EventArgs e)
        {
            var response = EventPublisherClient.PublishHealthCheckNotification();
            
            if (response.Success)
            {
                MessageBox.Show("Connection successfull.", "Test connection", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(response.Message, "Test connection", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void applyBtn_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
