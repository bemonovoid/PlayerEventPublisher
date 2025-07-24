using System;
using System.Collections.Generic;
using System.Windows.Forms;
using static MusicBeePlugin.Plugin;

namespace MusicBeePlugin
{
    public partial class ConfigurationForm : Form
    {
        private string configFilePath;

        public ConfigurationForm(string configFilePath)
        {
            InitializeComponent();
            this.configFilePath = configFilePath;
        }

        private void ConfigurationForm_Load(object sender, EventArgs e)
        {

        }

        private void testConnectionBtn_Click(object sender, EventArgs e)
        {
            var response = EventPublisherClient.PublishNotification("healthcheckfile", NotificationType.HealthCheck, new Dictionary<string, string>());
            
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
            Configuration.SaveConfig(configFilePath);
            this.Close();
        }
    }
}
