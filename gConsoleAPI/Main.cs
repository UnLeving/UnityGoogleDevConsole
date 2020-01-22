using Google.Apis.AndroidPublisher.v3;
using Google.Apis.AndroidPublisher.v3.Data;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Upload;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using gConsoleAPI.Helpers;
using static Google.Apis.AndroidPublisher.v3.EditsResource.ApksResource;

namespace gConsoleAPI
{
    public class GAPublisher
    {
        EditsResource _edits;
        string _editsId;
        long _versionCode;
        string _apkUploadSource = "from API";

        string jsonKeyFilePath;
        string apkFilePath;
        string applicationName;
        string packageName;
        string localizedText;
        UploadType uploadType;

        public GAPublisher(string jsonKeyFilePath, string applicationName, string packageName, string localizedText, UploadType uploadType = UploadType.INTERNAL, string apkFilePath = null)
        {
            this.jsonKeyFilePath = jsonKeyFilePath;
            this.apkFilePath = apkFilePath;
            this.applicationName = applicationName;
            this.packageName = packageName;
            this.localizedText = localizedText;
            this.uploadType = uploadType;
        }

        void InitEditService()
        {
            Logger.WrightLog("Init start...");
            JObject cr = null;
            try
            {
                cr = (JObject)JsonConvert.DeserializeObject(File.ReadAllText(jsonKeyFilePath));
            }
            catch (FileNotFoundException IOex)
            {
                Logger.WrightLog("Init: json file not found exception: " + IOex.Message);
            }
            catch (Exception ex)
            {
                Logger.WrightLog("Init: exception: " + ex.Message);
            }


            var xCred = new ServiceAccountCredential(new ServiceAccountCredential.Initializer((string)cr.GetValue(ServiceAccountCred.EMAIL))
            {
                Scopes = new[] { AndroidPublisherService.Scope.Androidpublisher }
            }.FromPrivateKey((string)cr.GetValue(ServiceAccountCred.KEY)));

            var service = new AndroidPublisherService(new BaseClientService.Initializer
            {
                HttpClientInitializer = xCred,
                ApplicationName = applicationName
            });

            _edits = service.Edits;
            AppEdit appEdit = _edits.Insert(null, packageName).Execute();
            if (appEdit != null)
            {
                Logger.WrightLog("Init: service created");
                _editsId = appEdit.Id;
            }
            else
                Logger.WrightLog("Init: service FAILED");
        }

        public int RetreiveLastBuildNumber()
        {
            InitEditService();
            ApksListResponse apksListResponse = _edits.Apks.List(packageName, _editsId).Execute();
            if (apksListResponse == null)
            {
                Logger.WrightLog("Retreive number failed");
                return -1;
            }
            else
                return apksListResponse.Apks[apksListResponse.Apks.Count - 1].VersionCode.Value;
        }

        public void Publish()
        {
            InitEditService();
            Logger.WrightLog("Upload start...");
            string apkPath = apkFilePath;
            UploadMediaUpload uploadMedia;

            try
            {
                using (Stream stream = new FileStream(apkPath, FileMode.Open, FileAccess.Read))
                {
                    uploadMedia = _edits.Apks.Upload(packageName, _editsId, stream, MediaType.ARCHIVE);
                    uploadMedia.ProgressChanged += UploadMedia_ProgressChanged;
                    uploadMedia.ResponseReceived += UploadMedia_ResponseReceived;
                    uploadMedia.Upload();

                }
            }
            catch (FileNotFoundException IOex)
            {
                Logger.WrightLog("Upload apk exception: " + IOex.Message);
            }
            catch (Exception ex)
            {
                Logger.WrightLog("Upload exception: " + ex.Message);
            }
        }

        void UploadMedia_ResponseReceived(Apk obj)
        {
            _versionCode = obj.VersionCode.Value;
            Logger.WrightLog("Upload: version code: " + _versionCode);

            if (uploadType == UploadType.library)
            {
                Logger.WrightLog("Upload finished. Find it in Console artifact library");
                return;
            }
            UpdateTrack(uploadType.ToString().ToLower());
        }

        void UploadMedia_ProgressChanged(IUploadProgress obj)
        {
            Logger.WrightLog("Upload status: " + obj.Status);
            if (obj.Exception != null)
                Logger.WrightLog("Upload exception: " + obj.Exception.Message);
        }

        void UpdateTrack(string trackType)
        {
            Logger.WrightLog("Update start...");
            var release = new TrackRelease
            {
                Name = _apkUploadSource,
                VersionCodes = new long?[] { _versionCode },
                ReleaseNotes = new List<LocalizedText> { new LocalizedText { Language = "en-US", Text = localizedText } },
                Status = TrackReleaseStatus.COMPLETED
            };

            var track = _edits.Tracks.Update(new Track { Releases = new List<TrackRelease> { release } }, packageName, _editsId, trackType).Execute();
            if (track != null)
            {
                Logger.WrightLog("Update OK");
                Commit();
            }
            else
                Logger.WrightLog("Update FAILED");
        }

        void Commit()
        {
            Logger.WrightLog("Commit start...");
            var response = _edits.Commit(packageName, _editsId).Execute();
            if (response != null)
                Logger.WrightLog("Commit OK");
            else
                Logger.WrightLog("Commit FAILED");
        }

        public static void Log(string text)
        {
            Logger.WrightLog(text);
        }
    }
}
