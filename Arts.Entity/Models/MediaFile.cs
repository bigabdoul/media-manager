namespace Arts.Entity.Models
{
    /// <summary>
    /// Represents a media file whose content can be either stored on disk or in a database.
    /// </summary>
    public class MediaFile : MediaEntity
    {
        const long LEN_MB = 0x100000L; // 1 MB = 1,048,576 B
        const long LEN_GB = 0x40000000L; // 1 GB = 1,073,741,824 B

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaFile"/> class.
        /// </summary>
        public MediaFile()
        {
        }

        /// <summary>
        /// Gets or sets the media file name.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Gets or sets the media content length.
        /// </summary>
        public long Length { get; set; }

        /// <summary>
        /// Gets or sets the media content.
        /// </summary>
        public byte[] Content { get; set; }

        /// <summary>
        /// Gets or sets the media content type.
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// Indicates whether the content of the media has been stored in the database.
        /// </summary>
        public bool HasContent { get; set; }

        /// <summary>
        /// Gets or sets the hash code for the media content.
        /// </summary>
        public string HashCode { get; set; }

        /// <summary>
        /// Gets or sets the virtual path to the media.
        /// </summary>
        public virtual string MediaUrl { get; set; }

        /// <summary>
        /// Indicates whether the <see cref="MediaUrl"/> property value is not null or blank.
        /// </summary>
        public bool HasMedia => !string.IsNullOrWhiteSpace(MediaUrl);

        /// <summary>
        /// Returns true if the <see cref="ContentType"/> value starts with 'image'.
        /// </summary>
        public bool IsImage => ContentType?.StartsWith("image", System.StringComparison.OrdinalIgnoreCase) == true;

        /// <summary>
        /// Returns true if the <see cref="ContentType"/> value starts with 'audio'.
        /// </summary>
        public bool IsAudio => ContentType?.StartsWith("audio", System.StringComparison.OrdinalIgnoreCase) == true;

        /// <summary>
        /// Returns true if the <see cref="ContentType"/> value starts with 'video'.
        /// </summary>
        public bool IsVideo => ContentType?.StartsWith("video", System.StringComparison.OrdinalIgnoreCase) == true;

        /// <summary>
        /// Returns a user-friendly display for the <see cref="Length"/> property.
        /// </summary>
        public string LengthDisplay
        {
            get
            {
                var len = Length;
                if (len < 1L) return "0";
                var value = len / 1024L;

                if (value < 1L) return "1 KB";
                return $"{value.ToString("N")} KB";
                /*
                if (len < 1L) return string.Empty;
                if (len < 1024L) return $"{len} B";
                if (len < LEN_MB) return $"{(len / 1024.0).ToString("N2")} KB";
                if (len < LEN_GB) return $"{(len / (double)LEN_MB).ToString("N2")} MB";                
                return $"{(len / (double)LEN_GB).ToString("N2")} GB";
                */
            }
        }

        /// <summary>
        /// Returns a user-friendly display for the <see cref="MediaEntity.DateCreated"/> property.
        /// </summary>
        public string DateCreatedDisplay => DateCreated.ToString("dd-MM-yyyy @ HH:mm");

        /// <summary>
        /// Returns a user-friendly display for the <see cref="MediaEntity.DateModified"/> property.
        /// </summary>
        public string DateModifiedDisplay => DateModified.ToString("dd-MM-yyyy @ HH:mm");

        /// <summary>
        /// Replaces back with forward slashes within the virtual path.
        /// </summary>
        /// <returns></returns>
        public string GetMediaUrl()
        {
            return MediaUrl?.Replace('\\', '/');
        }

        /// <summary>
        /// Returns the string representation of the current media file.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return FileName;
        }
    }
}
