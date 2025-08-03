using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

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

            string dataPath = Path.Combine(mbApiInterface.Setting_GetPersistentStoragePath(), configFileName);
            Configuration.LoadConfig(dataPath);

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
                configurationForm = new ConfigurationForm();
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
            var fileUrl = sourceFileUrl;
            var data = new Dictionary<string, string>();

            switch (type)
            {
                case NotificationType.PlayCountersChanged:
                    var libraryPlayCount = mbApiInterface.Library_GetFileProperty(fileUrl, FilePropertyType.PlayCount);
                    if (libraryPlayCount != null && libraryPlayCount.Length > 0)
                    {
                        data.Add("libraryPlayCount", libraryPlayCount);
                    }
                    //data.Add("nowPlayingPlayCount", mbApiInterface.NowPlaying_GetFileProperty(FilePropertyType.PlayCount));
                    //data.Add("pendingPlayCount", mbApiInterface.Pending_GetFileProperty(FilePropertyType.PlayCount));
                    break;
                case NotificationType.PlayStateChanged:
                    data.Add("playState", mbApiInterface.Player_GetPlayState().ToString());
                    break;
                case NotificationType.PluginStartup:
                    Configuration.Enabled = true; // Plugin starts only when it is enabled (Plugin button Enable/Disable).
                    break;
                case NotificationType.RatingChanging:
                case NotificationType.RatingChanged:
                    var libraryRating = mbApiInterface.Library_GetFileTag(fileUrl, MetaDataType.Rating);
                    if (libraryRating != null && libraryRating.Length > 0)
                    {
                        data.Add("libraryRating", libraryRating);
                    }
                    //data.Add("nowPlayingRating", mbApiInterface.NowPlaying_GetFileTag(MetaDataType.Rating));
                    //data.Add("pendingRating", mbApiInterface.Pending_GetFileTag(MetaDataType.Rating));
                    break;
                //case NotificationType.TagsChanging:
                case NotificationType.TagsChanged:
                    var libraryLyrics = mbApiInterface.Library_GetFileTag(fileUrl, MetaDataType.Lyrics);
                    if (libraryLyrics != null && libraryLyrics.Length > 0) 
                    {
                        data.Add("libraryLyrics", ToBase64String(libraryLyrics));
                    }
                    var libraryRatingLove = mbApiInterface.Library_GetFileTag(fileUrl, MetaDataType.RatingLove);
                    if (libraryRatingLove != null && libraryRatingLove.Length > 0)
                    {
                        data.Add("libraryRatingLove", libraryRatingLove);
                    }
                    break;
                case NotificationType.TrackChanging:
                    fileUrl = mbApiInterface.NowPlaying_GetFileUrl();
                    break;
                case NotificationType.TrackChanged:
                    break;
            }
            if (fileUrl != null && fileUrl.Length > 0 && Configuration.Enabled) 
            {
                EventPublisherClient.PublishNotification(fileUrl, type, data);
            }
        }

        private static String ToBase64String(string value)
        {
            if (value == null) return null;
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
        }
    }
}