namespace InternshipManagementSystem.FileManagement
{
    public class FileUploadResultDto
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public long Size { get; set; }
        public string ContentType { get; set; }
        public string StoredFileName { get; set; }
        public string Folder { get; set; }
        public string Url { get; set; }
    }
  

}
