using System;
using System.Collections.Generic;

namespace LanControl.Core.Models
{
    public enum FileTransferAction
    {
        UploadToAgent = 1,
        DownloadFromAgent = 2,
        DeleteFile = 3,
        CreateDirectory = 4,
        ExecuteFile = 5
    }

    [Serializable]
    public class FileTransferRequest
    {
        public string TransferId { get; set; }
        public string AgentId { get; set; }
        public string TargetVlan { get; set; }
        public FileTransferAction Action { get; set; }
        public string RemotePath { get; set; }
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public bool Overwrite { get; set; }
        public bool ExecuteAfterTransfer { get; set; }
        public string ExecuteArguments { get; set; }

        public FileTransferRequest()
        {
            TransferId = Guid.NewGuid().ToString("N");
            Action = FileTransferAction.UploadToAgent;
            Overwrite = true;
        }
    }

    [Serializable]
    public class FileChunkMeta
    {
        public string TransferId { get; set; }
        public string AgentId { get; set; }
        public int ChunkIndex { get; set; }
        public int TotalChunks { get; set; }
        public long Offset { get; set; }
        public int Length { get; set; }
        public bool IsLastChunk { get; set; }
    }

    [Serializable]
    public class FileTransferProgressResponse
    {
        public string TransferId { get; set; }
        public string AgentId { get; set; }
        public string Hostname { get; set; }
        public string FileName { get; set; }
        public long BytesTransferred { get; set; }
        public long TotalBytes { get; set; }
        public int Percentage { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public string SavedPath { get; set; }
    }

    [Serializable]
    public class RemoteFileSystemItem
    {
        public string Name { get; set; }
        public string FullPath { get; set; }
        public bool IsDirectory { get; set; }
        public long Size { get; set; }
        public string LastModified { get; set; }
        public string Extension { get; set; }
    }

    [Serializable]
    public class RemoteDirectoryListRequest
    {
        public string AgentId { get; set; }
        public string Path { get; set; }
    }

    [Serializable]
    public class RemoteDirectoryListResponse
    {
        public string AgentId { get; set; }
        public string CurrentPath { get; set; }
        public string ParentPath { get; set; }
        public List<string> Drives { get; set; }
        public List<RemoteFileSystemItem> Items { get; set; }
        public string ErrorMessage { get; set; }

        public RemoteDirectoryListResponse()
        {
            Drives = new List<string>();
            Items = new List<RemoteFileSystemItem>();
        }
    }
}
