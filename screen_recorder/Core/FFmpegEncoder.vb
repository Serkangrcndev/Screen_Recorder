Imports System.Diagnostics
Imports System.IO
Imports System.IO.Pipes
Imports System.Threading

Namespace Core

    Public Class FFmpegEncoder
        Implements IDisposable

        Private _process As Process
        Private _inputWriter As BinaryWriter
        Private _settings As Models.RecordingSettings
        Private _width As Integer
        Private _height As Integer
        Private _outputPath As String
        Private _frameQueue As New Concurrent.ConcurrentQueue(Of Byte())()
        Private _processingThread As Thread
        Private _cancellationTokenSource As CancellationTokenSource
        Private _frameCount As Integer = 0
        Private _isRunning As Boolean = False

        Public ReadOnly Property FrameCount As Integer
            Get
                Return _frameCount
            End Get
        End Property

        Public Sub New(settings As Models.RecordingSettings, width As Integer, height As Integer, outputPath As String)
            _settings = settings
            _width = width
            _height = height
            _outputPath = outputPath
        End Sub

        Public Sub Start()
            If _isRunning Then Return

            _cancellationTokenSource = New CancellationTokenSource()
            _isRunning = True

            Dim ffmpegArgs = BuildFFmpegArguments()

            Dim startInfo As New ProcessStartInfo()
            startInfo.FileName = GetFFmpegPath()
            startInfo.Arguments = ffmpegArgs
            startInfo.UseShellExecute = False
            startInfo.CreateNoWindow = True
            startInfo.RedirectStandardInput = True
            startInfo.RedirectStandardError = True
            startInfo.RedirectStandardOutput = False

            _process = New Process()
            _process.StartInfo = startInfo
            _process.EnableRaisingEvents = True

            Try
                File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg_log.txt"), "Starting FFmpeg with args: " & ffmpegArgs & Environment.NewLine)
            Catch
            End Try

            AddHandler _process.ErrorDataReceived, Sub(s, e)
                                                       If Not String.IsNullOrEmpty(e.Data) Then
                                                           Debug.WriteLine($"FFmpeg: {e.Data}")
                                                           Try
                                                               File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg_log.txt"), e.Data & Environment.NewLine)
                                                           Catch
                                                           End Try
                                                       End If
                                                   End Sub

            Try
                _process.Start()
                _process.BeginErrorReadLine()
                
                _inputWriter = New BinaryWriter(_process.StandardInput.BaseStream)
            Catch ex As Exception
                Try
                    File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg_log.txt"), "Process.Start Exception: " & ex.Message & Environment.NewLine)
                Catch
                End Try
                Throw
            End Try

            _processingThread = New Thread(AddressOf ProcessFrameQueue)
            _processingThread.IsBackground = True
            _processingThread.Start()
        End Sub

        Private Function BuildFFmpegArguments() As String
            Dim args = "-y -f rawvideo -vcodec rawvideo "
            args &= $"-s {_width}x{_height} "
            args &= "-pix_fmt bgra "
            args &= $"-r {_settings.FrameRate} "
            args &= "-thread_queue_size 512 "
            args &= "-i - "

            ' Audio support using dshow is problematic on many systems (e.g. virtual-audio-capturer missing)
            ' This causes FFmpeg to crash instantly. Audio muxing should be done post-recording.
            ' If _settings.RecordSystemAudio OrElse _settings.RecordMicrophone Then
            '     args &= BuildAudioArguments()
            ' End If

            args &= GetVideoCodecArguments()
            args &= $" -pix_fmt yuv420p "
            args &= $" -movflags +faststart "
            args &= $" -threads 0 "
            args &= $" ""{_outputPath}"""

            Return args
        End Function

        Private Function BuildAudioArguments() As String
            Dim args = ""

            If _settings.RecordSystemAudio Then
                args &= " -f dshow -i audio=""virtual-audio-capturer"" "
            End If

            If _settings.RecordMicrophone AndAlso Not String.IsNullOrEmpty(_settings.MicrophoneDevice) Then
                args &= $" -f dshow -i audio=""{_settings.MicrophoneDevice}"" "
            End If

            args &= " -c:a aac -b:a 192k -ac 2 "

            Return args
        End Function

        Private Function GetVideoCodecArguments() As String
            Select Case _settings.VideoCodec
                Case Models.VideoCodec.H264
                    Return " -c:v libx264 -preset fast -crf " & _settings.VideoQuality.ToString()
                Case Models.VideoCodec.H265
                    Return " -c:v libx265 -preset fast -crf " & _settings.VideoQuality.ToString()
                Case Models.VideoCodec.Lossless
                    Return " -c:v libx264 -preset ultrafast -crf 0 -pix_fmt bgr24"
                Case Else
                    Return " -c:v libx264 -preset fast -crf 23"
            End Select
        End Function

        Private Function GetFFmpegPath() As String
            ' Check ffmpeg/bin/ffmpeg.exe (installer location)
            Dim localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg", "bin", "ffmpeg.exe")
            If File.Exists(localPath) Then
                Return localPath
            End If

            ' Check root directory
            Dim rootPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg.exe")
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

            Return "ffmpeg"
        End Function

        Public Sub WriteFrame(frameData As Byte())
            If Not _isRunning Then Return
            _frameQueue.Enqueue(frameData)
        End Sub

        Private Sub ProcessFrameQueue()
            Try
                While Not _cancellationTokenSource.Token.IsCancellationRequested
                    Dim frame As Byte() = Nothing
                    If _frameQueue.TryDequeue(frame) AndAlso frame IsNot Nothing Then
                        Try
                            _inputWriter.Write(frame)
                            _frameCount += 1

                            If _frameCount Mod 30 = 0 Then
                                _inputWriter.Flush()
                            End If
                        Catch ex As Exception
                            Debug.WriteLine($"Frame yazma hatası: {ex.Message}")
                        End Try
                    Else
                        Thread.Sleep(1)
                    End If
                End While
            Catch ex As Exception
                Debug.WriteLine($"Frame kuyruğu işleme hatası: {ex.Message}")
            End Try
        End Sub

        Public Sub [Stop]()
            If Not _isRunning Then Return

            _isRunning = False
            _cancellationTokenSource?.Cancel()

            Thread.Sleep(100)

            Try
                _inputWriter?.Flush()
                _inputWriter?.Close() ' Closing stdin tells FFmpeg to finish encoding
            Catch
            End Try

            Try
                If _process IsNot Nothing AndAlso Not _process.HasExited Then
                    _process.WaitForExit(5000)

                    If Not _process.HasExited Then
                        _process.Kill()
                    End If
                End If
            Catch ex As Exception
                Debug.WriteLine($"FFmpeg durdurma hatası: {ex.Message}")
            End Try

            _processingThread?.Join(2000)
            _process?.Dispose()
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            [Stop]()
            _inputWriter?.Dispose()
            _cancellationTokenSource?.Dispose()
        End Sub

    End Class

End Namespace
