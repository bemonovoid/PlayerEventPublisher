using System;
using System.Collections.Generic;
using System.IO;

namespace MusicBeePlugin
{
    public partial class Plugin
    {
        private const string configFileName = "PlayerEventPublisher.xml";

        private MusicBeeApiInterface mbApiInterface;
        private PluginInfo about = new PluginInfo();
        private ConfigurationForm configurationForm;

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
            about.ConfigurationPanelHeight = 0;
            return about;
        }

        public bool Configure(IntPtr panelHandle)
        {
            if (configurationForm == null || configurationForm.IsDisposed) 
            {
                string configFilePath = Path.Combine(mbApiInterface.Setting_GetPersistentStoragePath(), configFileName);
                Configuration.LoadConfig(configFilePath);
                configurationForm = new ConfigurationForm(configFilePath);
            }
            configurationForm.Show();
            return true;
        }
       
        public void SaveSettings()
        {
            string dataPath = Path.Combine(mbApiInterface.Setting_GetPersistentStoragePath(), configFileName);
            Configuration.SaveConfig(dataPath);
        }

        public void Close(PluginCloseReason reason)
        {
            switch (reason)
            {
                case PluginCloseReason.UserDisabled:
                    Configuration.Enabled = false;
                    break;
                case PluginCloseReason.MusicBeeClosing:
                case PluginCloseReason.StopNoUnload:
                    break;
            }
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
            if (!Configuration.Enabled) return;

            var fileUrl = sourceFileUrl;
            var data = new Dictionary<string, string>();
           
            switch (type)
            {
                case NotificationType.PlayCountersChanged:
                    data.Add("playCount", mbApiInterface.Library_GetFileProperty(fileUrl, FilePropertyType.PlayCount));
                    break;
                case NotificationType.PlayStateChanged:
                    data.Add("playState", mbApiInterface.Player_GetPlayState().ToString());
                    break;
                case NotificationType.PluginStartup:
                    string dataPath = Path.Combine(mbApiInterface.Setting_GetPersistentStoragePath(), configFileName);
                    Configuration.LoadConfig(dataPath);
                    Configuration.Enabled = true;
                    break;
                case NotificationType.RatingChanging:
                    break;
                case NotificationType.RatingChanged:
                    data.Add("rating", mbApiInterface.Library_GetFileTag(fileUrl, MetaDataType.Rating));
                    data.Add("ratingLove", mbApiInterface.Library_GetFileTag(fileUrl, MetaDataType.RatingLove));
                    break;
                case NotificationType.TagsChanging:
                    break;
                case NotificationType.TagsChanged:
                    break;
                case NotificationType.TrackChanging:
                    fileUrl = mbApiInterface.NowPlaying_GetFileUrl();
                    break;
                case NotificationType.TrackChanged:
                    break;
            }
            if (fileUrl != null && fileUrl.Length > 0) 
            {
                EventPublisherClient.PublishNotification(fileUrl, type, data);
            }
        }
    }
}