public enum Operation { Provision, Delete, Start, Shutdown, Capture }

public class AzureOperation
    {
        public const string Windows = "Windows";
        public const string Linux = "Linux";

        public Operation Operation;
        public string Prefix { get; set; }
        public int UserID { get; set; }
        public int CourseID { get; set; }
        public int VEProfileID { get; set; }
        public string ImageName { get; set; }
        public string OSFamily { get; set; }
        public string Suffix { get; set; }
        public string ImageSize { get; set; }
        public string Protocol { get; set; }
        public string AzurePort { get; set; }
        public string GuacConnection { get; set; }
        public string WebApiUrl { get; set; }
        public string GuacamoleUrl { get; set; }
        public string Region { get; set; }
        public string MachineName { get; set; }
        public string RoleName
        {
            get
            {
                if (string.IsNullOrEmpty(Prefix))
                {
                    return string.IsNullOrEmpty(Suffix)
                        ? $"{CourseID.ToString()}-{VEProfileID.ToString()}-{UserID.ToString()}"
                        : $"{CourseID.ToString()}-{VEProfileID.ToString()}-{UserID.ToString()}-{Suffix}";
                }

                return string.IsNullOrEmpty(Suffix)
                    ? $"{Prefix}-{CourseID.ToString()}-{VEProfileID.ToString()}-{UserID.ToString()}"
                    : $"{Prefix}-{CourseID.ToString()}-{VEProfileID.ToString()}-{UserID.ToString()}-{Suffix}";
            }
        }
        public string ImageType { get; set; }

    }