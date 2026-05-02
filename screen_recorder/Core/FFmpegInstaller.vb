Imports System.IO
Imports System.Net
Imports System.IO.Compression
Imports System.Diagnostics
Imports System.Threading.Tasks
Imports System.Windows.Forms

Namespace Core

    Public Class FFmpegInstaller

        Private Const FFMPEG_URL = "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl.zip"
        Private Const FFMPEG_FOLDER = "ffmpeg"

        Public Shared Function IsFFmpegInstalled() As Boolean
            Try
                ' Check if ffmpeg.exe exists in app folder
                Dim localPath = Path.Combine(Application.StartupPath, FFMPEG_FOLDER, "bin", "ffmpeg.exe")
                If File.Exists(localPath) Then
                    Return True
                End If

                ' Check if ffmpeg is in PATH
                Using process = New Process()
                    process.StartInfo.FileName = "ffmpeg"
                    process.StartInfo.Arguments = "-version"
                    process.StartInfo.UseShellExecute = False
                    process.StartInfo.RedirectStandardOutput = True
                    process.StartInfo.CreateNoWindow = True
                    process.Start()
                    process.WaitForExit(3000)
                    Return process.ExitCode = 0
                End Using
            Catch
                Return False
            End Try
        End Function

        Public Shared Function GetFFmpegPath() As String
            ' First check local folder (AppDomain)
            Dim baseDir = AppDomain.CurrentDomain.BaseDirectory
            Dim localPath = Path.Combine(baseDir, "ffmpeg", "bin", "ffmpeg.exe")
            If File.Exists(localPath) Then
                Return localPath
            End If

            ' Check Application.StartupPath (fallback)
            Dim startupPath = Path.Combine(Application.StartupPath, "ffmpeg", "bin", "ffmpeg.exe")
            If File.Exists(startupPath) Then
                Return startupPath
            End If

            ' Check root directory
            Dim rootPath = Path.Combine(baseDir, "ffmpeg.exe")
            If File.Exists(rootPath) Then
                Return rootPath
            End If

            ' Check PATH
            Dim pathEnv = Environment.GetEnvironmentVariable("PATH")
            If pathEnv IsNot Nothing Then
                For Each p In pathEnv.Split(";"c)
                    If Not String.IsNullOrEmpty(p) Then
                        Dim fullPath = Path.Combine(p.Trim(), "ffmpeg.exe")
                        If File.Exists(fullPath) Then
                            Return fullPath
                        End If
                    End If
                Next
            End If

            ' Return just "ffmpeg" if in PATH (system installed)
            Return "ffmpeg"
        End Function

        Public Shared Async Function DownloadAndInstallFFmpeg(progress As IProgress(Of String), cancellationToken As System.Threading.CancellationToken) As Task(Of Boolean)
            Try
                Dim tempPath = Path.GetTempPath()
                Dim zipPath = Path.Combine(tempPath, "ffmpeg_download.zip")
                Dim extractPath = Path.Combine(tempPath, "ffmpeg_extract")
                Dim targetPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FFMPEG_FOLDER)

                progress?.Report("FFmpeg indiriliyor... (Yaklaşık 140 MB)")

                ' Download
                Using client = New WebClient()
                    AddHandler client.DownloadProgressChanged, Sub(s, e)
                                                                   Dim percent = e.ProgressPercentage
                                                                   progress?.Report($"İndiriliyor... %{percent}")
                                                               End Sub
                    Await client.DownloadFileTaskAsync(FFMPEG_URL, zipPath)
                End Using

                progress?.Report("Dosyalar çıkarılıyor...")

                ' Extract
                If Directory.Exists(extractPath) Then
                    Directory.Delete(extractPath, True)
                End If
                ZipFile.ExtractToDirectory(zipPath, extractPath)

                ' Find extracted folder (it has a version number)
                Dim extractedFolders = Directory.GetDirectories(extractPath)
                If extractedFolders.Length = 0 Then
                    Return False
                End If

                Dim sourceBinPath = Path.Combine(extractedFolders(0), "bin")
                If Not Directory.Exists(sourceBinPath) Then
                    Return False
                End If

                progress?.Report("Kurulum yapılıyor...")

                ' Create target directory
                If Directory.Exists(targetPath) Then
                    Directory.Delete(targetPath, True)
                End If
                Directory.CreateDirectory(targetPath)

                ' Copy only necessary files
                Directory.CreateDirectory(Path.Combine(targetPath, "bin"))
                File.Copy(Path.Combine(sourceBinPath, "ffmpeg.exe"), Path.Combine(targetPath, "bin", "ffmpeg.exe"), True)
                File.Copy(Path.Combine(sourceBinPath, "ffplay.exe"), Path.Combine(targetPath, "bin", "ffplay.exe"), True)
                File.Copy(Path.Combine(sourceBinPath, "ffprobe.exe"), Path.Combine(targetPath, "bin", "ffprobe.exe"), True)

                ' Copy necessary DLLs
                For Each dll In Directory.GetFiles(sourceBinPath, "*.dll")
                    Dim fileName = Path.GetFileName(dll)
                    File.Copy(dll, Path.Combine(targetPath, "bin", fileName), True)
                Next

                ' Cleanup
                File.Delete(zipPath)
                Directory.Delete(extractPath, True)

                progress?.Report("Kurulum tamamlandı!")
                Return True

            Catch ex As Exception
                progress?.Report($"Hata: {ex.Message}")
                Return False
            End Try
        End Function

    End Class

End Namespace
