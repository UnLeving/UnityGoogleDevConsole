namespace gConsoleAPI.Helpers
{
    /// <summary>"Edits.tracks release status enums"</summary>
    public static class TrackReleaseStatus
    {
        /// <summary>"completed"</summary>
        public static readonly string COMPLETED = "completed";
        /// <summary>"draft"</summary>
        public static readonly string DRAFT = "draft";
        /// <summary>"halted"</summary>
        public static readonly string HALTED = "halted";
        /// <summary>"inProgress"</summary>
        public static readonly string INPROGRESS = "inProgress";
    }
    /// <summary>"Edits.tracks types enums"</summary>
    public enum UploadType
    {
        INTERNAL,
        alpha,
        beta,
        production,
        library
    }
    /// <summary>"service account credentials enums"</summary>
    public static class ServiceAccountCred
    {
        /// <summary>"client_email"</summary>
        public static readonly string EMAIL = "client_email";
        /// <summary>"private_key"</summary>
        public static readonly string KEY = "private_key";
    }
    /// <summary>"Media MIME types enums"</summary>
    public static class MediaType
    {
        /// <summary>"application/octet-stream"</summary>
        public static readonly string STREAM = "application/octet-stream";
        /// <summary>"application/vnd.android.package-archive"</summary>
        public static readonly string ARCHIVE = "application/vnd.android.package-archive";
    }
}