
using System;
using System.Windows.Media;

namespace Runt
{
    partial class Icons
    {
        private static Lazy<ImageSource> _folderNormal = new Lazy<ImageSource>(() => GetImage("Folder_6222.ico"));
        public static ImageSource FolderNormal { get { return _folderNormal.Value; } }

        private static Lazy<ImageSource> _folderOpen = new Lazy<ImageSource>(() => GetImage("Folder_6221.ico"));
        public static ImageSource FolderOpen { get { return _folderOpen.Value; } }

        private static Lazy<ImageSource> _document = new Lazy<ImageSource>(() => GetImage("document_16xLG.png"));
        public static ImageSource Document { get { return _document.Value; } }

        private static Lazy<ImageSource> _project = new Lazy<ImageSource>(() => GetImage("CSharpProject_SolutionExplorerNode.png"));
        public static ImageSource Project { get { return _project.Value; } }

        private static Lazy<ImageSource> _cSFile = new Lazy<ImageSource>(() => GetImage("CSharpFile_SolutionExplorerNode.png"));
        public static ImageSource CSFile { get { return _cSFile.Value; } }

        private static Lazy<ImageSource> _reference = new Lazy<ImageSource>(() => GetImage("reference_16xLG.png"));
        public static ImageSource Reference { get { return _reference.Value; } }

    }
}

