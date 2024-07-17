using System.ComponentModel;
using Render.TempFromVessel.Extensions;

namespace Render.TempFromVessel
{
    /// <summary>
    /// Exception class for general services.
    /// </summary>
    public class ServiceException : Exception
    {
        /// <summary>
        /// Error codes for general services.
        /// </summary>
        public enum ErrorCode
        {
            [Description("Exception.Service.SaveFailure")]
            SaveFailure,
            [Description("Exception.Service.RetrieveFailure")]
            RetrieveFailure,
            [Description("Exception.Service.DeleteFailure")]
            DeleteFailure,
            [Description("Exception.Service.IsNotConnected")]
            IsNotConnected,
            [Description("Exception.Service.NoParatextProjectsFound")]
            NoParatextProjectsFound,
            // The download from the DBP failed. This may be due to timeout, or to some error on the
            // remote server, or to a data transmission error.
            [Description("Exception.Service.BibleDownloadFailed")]
            BibleDownloadFailed,
            [Description("Exception.Service.ImportFailure")]
            ImportFailure,
            [Description("Exception.Service.LoginFailure")]
            LoginFailure,
            [Description("Exception.Service.UserDoesNotExistFailure")]
            UserDoesNotExistFailure,
            [Description("Exception.Service.InvalidPassword")]
            InvalidPassword
        }

        /// <summary>
        /// Optional additional information that could be useful for logging/debugging an issue.
        /// </summary>
        public string AdditionalInformation { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceException"/> class.
        /// </summary>
        public ServiceException(
            ErrorCode errorCode,
            Exception ex = null,
            string additionalInformation = null) :
            base(errorCode.GetDescription(), ex)
        {
            AdditionalInformation = additionalInformation;
        }

    }
}