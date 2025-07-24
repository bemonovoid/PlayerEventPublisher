using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms;

namespace MusicBeePlugin
{
    public partial class Plugin
    {
        private const string configFileName = "PlayerEventPublisher.xml";

        private MusicBeeApiInterface mbApiInterface;
        private PluginInfo about = new PluginInfo();
        private TextBox endpointUrlTextBox;

        public PluginInfo Initialise(IntPtr apiInterfacePtr)
        {
            mbApiInterface = new MusicBeeApiInterface();
            mbApiInterface.Initialise(apiInterfacePtr);
            about.PluginInfoVersion = PluginInfoVersion;
            about.Name = "Player Event Publisher";
            about.Description = "Publishes player event notifications to rest api endpoint";
            about.Author = "Gregory Kosik";
            about.TargetApplication = "";
            about.Type = PluginType.General;
            about.VersionMajor = 1;
            about.VersionMinor = 0;
            about.Revision = 1;
            about.MinInterfaceVersion = MinInterfaceVersion;
            about.MinApiRevision = MinApiRevision;
            about.ReceiveNotifications = (ReceiveNotificationFlags.PlayerEvents | ReceiveNotificationFlags.TagEvents);
            about.ConfigurationPanelHeight = 20;
            return about;
        }

        public bool Configure(IntPtr panelHandle)
        {
            string dataPath = mbApiInterface.Setting_GetPersistentStoragePath();
            if (panelHandle != IntPtr.Zero)
            {
                Panel configPanel = (Panel)Panel.FromHandle(panelHandle);
                Label endpointUrlLabel = new Label();
                endpointUrlLabel.AutoSize = true;
                endpointUrlLabel.Location = new Point(0, 0);
                endpointUrlLabel.Text = "Endpoint url:";
                endpointUrlTextBox = new TextBox();
                endpointUrlTextBox.Text = Configuration.EndpointUrl;
                endpointUrlTextBox.Bounds = new Rectangle(100, 0, 400, endpointUrlTextBox.Height);
                configPanel.Controls.AddRange(new Control[] { endpointUrlLabel, endpointUrlTextBox });
            }
            return false;
        }
       
        public void SaveSettings()
        {
            string dataPath = Path.Combine(mbApiInterface.Setting_GetPersistentStoragePath(), configFileName);

            if (endpointUrlTextBox != null)
            {
                Console.WriteLine($"Saving config... Old endpointUrl: {Configuration.EndpointUrl}, new endpointUrl: {endpointUrlTextBox.Text}");

                Configuration.EndpointUrl = endpointUrlTextBox.Text;
                Configuration.SaveConfig(dataPath);

                PublishNotification("healthcheckfile", NotificationType.HealthCheck, new Dictionary<string, string>());
            }
        }

        public void Close(PluginCloseReason reason)
        {
            string dataPath = Path.Combine(mbApiInterface.Setting_GetPersistentStoragePath(), configFileName);
            Configuration.SaveConfig(dataPath);
        }

        public void Uninstall()
        {
            string dataPath = Path.Combine(mbApiInterface.Setting_GetPersistentStoragePath(), configFileName);
            if (File.Exists(dataPath))
                File.Delete(dataPath);
        }

        public void ReceiveNotification(string sourceFileUrl, NotificationType type)
        {
            var data = new Dictionary<string, string>();
           
            switch (type)
            {
                case NotificationType.PluginStartup:

                    string dataPath = Path.Combine(mbApiInterface.Setting_GetPersistentStoragePath(), configFileName);
                    Configuration.LoadConfig(dataPath);

                    PublishNotification("healthcheckfile", NotificationType.HealthCheck, data);

                    switch (mbApiInterface.Player_GetPlayState())
                    {
                        case PlayState.Playing:
                        case PlayState.Paused:
                            // ...
                            break;
                    }
                    break;
                case NotificationType.TrackChanging:
                    string artist = mbApiInterface.NowPlaying_GetFileTag(MetaDataType.Artist);
                    string trackTitle = mbApiInterface.NowPlaying_GetFileTag(MetaDataType.TrackTitle);
                    Console.WriteLine("TrackChanging. " + artist + " - " + trackTitle);
                    data.Add("prop1", "v1");
                    data.Add("prop2", "v2");
                    break;
                case NotificationType.TrackChanged:
                    string newArtist = mbApiInterface.NowPlaying_GetFileTag(MetaDataType.Artist);
                    string newTrackTrackTitle = mbApiInterface.NowPlaying_GetFileTag(MetaDataType.TrackTitle);
                    // MessageBox.Show("Artist: " + artist
                    //"Playing Artist", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    Console.WriteLine("TrackChanged. " + newArtist + " - " + newTrackTrackTitle);
                    break;
                case NotificationType.RatingChanging:
                    //string pendingRatingChanging = mbApiInterface.Pending_GetFileTag(MetaDataType.Rating);
                    //Console.WriteLine("pendingRatingChanging: " + pendingRatingChanging);
                    //string ratingBefore = mbApiInterface.NowPlaying_GetFileTag(MetaDataType.Rating);
                    //string ratingLoveBefore = mbApiInterface.NowPlaying_GetFileTag(MetaDataType.RatingLove);
                    //Console.WriteLine("Rating before change: " + ratingBefore + ". Love before change: " + ratingLoveBefore);
                    break;
                case NotificationType.RatingChanged:
                    //string pendingRatingAfterChange = mbApiInterface.Pending_GetFileTag(MetaDataType.Rating);
                    //Console.WriteLine("pendingRatingAfterChange: " +  pendingRatingAfterChange);
                    //Console.WriteLine("Rating changed in file:" + sourceFileUrl);
                    //string rating = mbApiInterface.NowPlaying_GetFileTag(MetaDataType.Rating);
                    //string ratingLove = mbApiInterface.NowPlaying_GetFileTag(MetaDataType.RatingLove);
                    //Console.WriteLine("New rating: " + rating + ". Love rating: " + ratingLove);
                    break;
            }
            if (sourceFileUrl != null && sourceFileUrl.Length > 0) 
            {
                PublishNotification(sourceFileUrl, type, data);
            }
        }

        // POSTs json payload (with notification name, file url and additional properties map if available) to endpoint url.
        // It is expected the server responds with '201 - Accepted'. Server can process request asynchronously. Logs an error if request fails.
        // Note: Endpoint authentication is not yet supported. 
        private void PublishNotification(string sourceFileUrl, NotificationType type, Dictionary<string, string> data)
        {
            if (Configuration.Suspended && NotificationType.HealthCheck != type) {
                Console.WriteLine($"Plugin was suspended because it had problems connecting to endpoint url: {Configuration.EndpointUrl}");
                return;
            }
            using (var wb = new WebClient())
            {
                var url = Configuration.EndpointUrl;
                wb.Headers[HttpRequestHeader.ContentType] = "application/json";

                var fileUrlBase64Encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(sourceFileUrl));

                var dataAsJsonString = string.Join(",", data.Select(entry => "\"" + entry.Key + "\":\"" + entry.Value + "\""));

                try 
                {
                    Console.WriteLine("Sending " + type.ToString() + " notification request ...");
                    var response = wb.UploadString(url, "POST", "{\"type\":\"" + type.ToString() + "\",\"filePath\":\"" + fileUrlBase64Encoded + "\",\"additionalProperties\":{" + dataAsJsonString + "}}");
                    Console.WriteLine(type.ToString() + " notification request has successfully been sent. " + response);
                    
                    if (NotificationType.HealthCheck == type) 
                    {
                        Configuration.Suspended = false;
                        Console.WriteLine("Endpoint health check is OK.");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Request failed: " + e);

                    if (NotificationType.HealthCheck == type && !Configuration.Suspended)
                    {
                        Console.WriteLine("Endpoint health check failed. Plugin will be suspended.");
                        Configuration.Suspended = true;
                    }
                } 

            }
        }
    }
}