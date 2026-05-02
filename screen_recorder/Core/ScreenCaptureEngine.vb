Imports System.Drawing
Imports System.Drawing.Imaging
Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Threading
Imports System.Threading.Tasks

Namespace Core

    Public Class ScreenCaptureEngine
        Implements IDisposable

        Private _settings As Models.RecordingSettings
        Private _ffmpegEncoder As FFmpegEncoder
        private _audioCapture As AudioCapture
        Private _captureThread As Thread
        Private _cancellationTokenSource As CancellationTokenSource
        Private _isRecording As Boolean = False
        Private _isPaused As Boolean = False
        Private _frameCount As Integer = 0
        private _startTime As DateTime
        Private _performanceManager As PerformanceManager
        Private _bufferPool As FrameBufferPool
        Private _captureBounds As Rectangle
        Private _targetFps As Integer = 60
        Private _frameInterval As Integer = 16

        Public Event RecordingStarted(sender As Object, e As EventArgs)
        Public Event RecordingStopped(sender As Object, e As EventArgs)
        Public Event RecordingPaused(sender As Object, e As EventArgs)
        Public Event RecordingResumed(sender As Object, e As EventArgs)
        Public Event RecordingError(sender As Object, e As RecordingErrorEventArgs)
        Public Event FrameCaptured(sender As Object, e As FrameCapturedEventArgs)

        Public Sub New()
            _performanceManager = New PerformanceManager()
        End Sub

        Public Sub StartRecording(settings As Models.RecordingSettings)
            If _isRecording Then Return

            Try
                _settings = settings
                _targetFps = settings.FrameRate
                _frameInterval = CInt(1000.0 / _targetFps)
                _cancellationTokenSource = New CancellationTokenSource()

                DetermineCaptureBounds()

                Dim bufferSize = _captureBounds.Width * _captureBounds.Height * 4
                _bufferPool = New FrameBufferPool(bufferSize, 10)
                _performanceManager.PreallocateBuffers(_captureBounds.Width, _captureBounds.Height, 5)

                Dim outputFile = GetOutputFilePath()
                _ffmpegEncoder = New FFmpegEncoder(settings, _captureBounds.Width, _captureBounds.Height, outputFile)
                _ffmpegEncoder.Start()

                If settings.RecordSystemAudio OrElse settings.RecordMicrophone Then
                    _audioCapture = New AudioCapture(settings)
                    AddHandler _audioCapture.RecordingError, AddressOf OnAudioError
                    _audioCapture.Start()
                End If

                PerformanceManager.OptimizeForScreenRecording()

                _isRecording = True
                _isPaused = False
                _startTime = DateTime.Now
                _frameCount = 0

                _captureThread = New Thread(AddressOf CaptureLoop)
                _captureThread.IsBackground = True
                _captureThread.Priority = ThreadPriority.Highest
                _captureThread.Start()

                RaiseEvent RecordingStarted(Me, EventArgs.Empty)

            Catch ex As Exception
                RaiseEvent RecordingError(Me, New RecordingErrorEventArgs($"Kayıt başlatma hatası: {ex.Message}"))
                Cleanup()
            End Try
        End Sub

        Private Sub DetermineCaptureBounds()
            If _settings.CaptureRegion.HasValue Then
                _captureBounds = _settings.CaptureRegion.Value
            Else
                Dim screenBounds = Screen.PrimaryScreen.Bounds
                _captureBounds = screenBounds
            End If

            _captureBounds.Width = Math.Max(2, (_captureBounds.Width \ 2) * 2)
            _captureBounds.Height = Math.Max(2, (_captureBounds.Height \ 2) * 2)
        End Sub

        Private Sub CaptureLoop()
            Try
                Dim stopwatch = New Stopwatch()
                Dim frameTime = TimeSpan.FromMilliseconds(_frameInterval)

                While Not _cancellationTokenSource.Token.IsCancellationRequested
                    stopwatch.Restart()

                    If Not _isPaused Then
                        CaptureFrame()
                    End If

                    Dim elapsed = stopwatch.Elapsed
                    Dim delay = frameTime - elapsed

                    If delay > TimeSpan.Zero Then
                        Thread.Sleep(delay)
                    End If
                End While

            Catch ex As Exception
                If _isRecording Then
                    RaiseEvent RecordingError(Me, New RecordingErrorEventArgs($"Yakalama hatası: {ex.Message}"))
                End If
            End Try
        End Sub

        Private Sub CaptureFrame()
            Try
                Dim frameBuffer = _bufferPool.Rent()

                Try
                    Using bitmap = New Bitmap(_captureBounds.Width, _captureBounds.Height, PixelFormat.Format32bppArgb)
                        Using g = Graphics.FromImage(bitmap)
                            g.CompositingMode = Drawing2D.CompositingMode.SourceCopy
                            g.CompositingQuality = Drawing2D.CompositingQuality.HighSpeed
                            g.InterpolationMode = Drawing2D.InterpolationMode.NearestNeighbor
                            g.SmoothingMode = Drawing2D.SmoothingMode.None
                            g.PixelOffsetMode = Drawing2D.PixelOffsetMode.HighSpeed

                            g.CopyFromScreen(_captureBounds.Location, Point.Empty, _captureBounds.Size, CopyPixelOperation.SourceCopy)

                            If _settings.EnablePrivacyMode AndAlso _settings.PrivacyRegions.Any() Then
                                ApplyPrivacyMasks(g)
                            End If
                        End Using

                        Dim bitmapData = bitmap.LockBits(
                            New Rectangle(0, 0, bitmap.Width, bitmap.Height),
                            ImageLockMode.ReadOnly,
                            PixelFormat.Format32bppArgb)

                        Try
                            Dim bytes = Math.Abs(bitmapData.Stride) * bitmapData.Height
                            Dim rawData = New Byte(bytes - 1) {}
                            Marshal.Copy(bitmapData.Scan0, rawData, 0, bytes)
                            _ffmpegEncoder?.WriteFrame(rawData)
                        Finally
                            bitmap.UnlockBits(bitmapData)
                        End Try
                    End Using

                    _frameCount += 1
                    _performanceManager.NotifyFrameProcessed()

                    If _frameCount Mod _targetFps = 0 Then
                        RaiseEvent FrameCaptured(Me, New FrameCapturedEventArgs(_frameCount, Nothing))
                    End If

                Finally
                    _bufferPool.ReturnBuffer(frameBuffer)
                End Try

            Catch ex As Exception
                Debug.WriteLine($"Frame yakalama hatası: {ex.Message}")
            End Try
        End Sub

        Private Sub ApplyPrivacyMasks(g As Graphics)
            For Each region In _settings.PrivacyRegions
                Select Case region.MaskType
                    Case Models.MaskType.SolidBlack
                        Using brush = New SolidBrush(Color.Black)
                            g.FillRectangle(brush, region.Bounds)
                        End Using

                    Case Models.MaskType.SolidWhite
                        Using brush = New SolidBrush(Color.White)
                            g.FillRectangle(brush, region.Bounds)
                        End Using

                    Case Models.MaskType.Pixelate
                        ApplyPixelate(g, region.Bounds, region.Intensity)

                    Case Else
                        Using brush = New SolidBrush(Color.FromArgb(128, 0, 0, 0))
                            g.FillRectangle(brush, region.Bounds)
                        End Using
                End Select
            Next
        End Sub

        Private Sub ApplyPixelate(g As Graphics, bounds As Rectangle, intensity As Integer)
            Try
                Dim pixelSize = Math.Max(4, intensity \ 5)
                Using brush = New SolidBrush(Color.FromArgb(160, 100, 100, 100))
                    For y As Integer = bounds.Top To bounds.Bottom Step pixelSize
                        For x As Integer = bounds.Left To bounds.Right Step pixelSize
                            g.FillRectangle(brush, x, y, pixelSize, pixelSize)
                        Next
                    Next
                End Using
            Catch
            End Try
        End Sub

        Public Sub PauseRecording()
            If Not _isRecording OrElse _isPaused Then Return
            _isPaused = True
            RaiseEvent RecordingPaused(Me, EventArgs.Empty)
        End Sub

        Public Sub ResumeRecording()
            If Not _isRecording OrElse Not _isPaused Then Return
            _isPaused = False
            RaiseEvent RecordingResumed(Me, EventArgs.Empty)
        End Sub

        Public Sub TogglePause()
            If _isPaused Then
                ResumeRecording()
            Else
                PauseRecording()
            End If
        End Sub

        Public Sub ToggleMicrophoneMute()
            If _audioCapture IsNot Nothing Then
                _audioCapture.IsMicrophoneMuted = Not _audioCapture.IsMicrophoneMuted
            End If
        End Sub

        Public Sub StopRecording()
            If Not _isRecording Then Return

            _isRecording = False
            _cancellationTokenSource?.Cancel()

            Thread.Sleep(50)

            Try
                _captureThread?.Join(2000)

                _audioCapture?.Stop()
                _audioCapture?.Dispose()
                _audioCapture = Nothing

                _ffmpegEncoder?.Stop()
                _ffmpegEncoder?.Dispose()
                _ffmpegEncoder = Nothing

                PerformanceManager.RestoreNormalMode()
                PerformanceManager.ForceGarbageCollection()

                RaiseEvent RecordingStopped(Me, EventArgs.Empty)

            Catch ex As Exception
                RaiseEvent RecordingError(Me, New RecordingErrorEventArgs($"Durdurma hatası: {ex.Message}"))
            Finally
                Cleanup()
            End Try
        End Sub

        Private Sub OnAudioError(sender As Object, e As Exception)
            RaiseEvent RecordingError(Me, New RecordingErrorEventArgs($"Ses hatası: {e.Message}"))
        End Sub

        Private Sub Cleanup()
            Try
                _bufferPool?.Clear()
                _performanceManager?.Dispose()
                _cancellationTokenSource?.Dispose()
            Catch
            End Try
        End Sub

        Private Function GetOutputFilePath() As String
            Dim basePath = _settings.OutputPath

            If _settings.AutoOrganizeByDate Then
                basePath = Path.Combine(basePath, DateTime.Now.ToString("yyyy-MM-dd"))
            End If

            If _settings.AutoOrganizeByProject Then
                basePath = Path.Combine(basePath, _settings.ProjectName)
            End If

            Directory.CreateDirectory(basePath)

            Dim fileName = $"{DateTime.Now:yyyyMMdd_HHmmss}_{_settings.ProjectName}.{_settings.OutputFormat}"
            Return Path.Combine(basePath, fileName)
        End Function

        Public ReadOnly Property RecordingDuration As TimeSpan
            Get
                If _isRecording Then
                    Return DateTime.Now - _startTime
                End If
                Return TimeSpan.Zero
            End Get
        End Property

        Public ReadOnly Property IsRecording As Boolean
            Get
                Return _isRecording
            End Get
        End Property

        Public ReadOnly Property IsPaused As Boolean
            Get
                Return _isPaused
            End Get
        End Property

        Public ReadOnly Property FrameCount As Integer
            Get
                Return _frameCount
            End Get
        End Property

        Public Sub Dispose() Implements IDisposable.Dispose
            StopRecording()
        End Sub

    End Class

    Public Class FrameCapturedEventArgs
        Inherits EventArgs

        Public ReadOnly Property FrameNumber As Integer
        Public ReadOnly Property FrameData As Byte()

        Public Sub New(frameNumber As Integer, frameData As Byte())
            Me.FrameNumber = frameNumber
            Me.FrameData = frameData
        End Sub
    End Class

    Public Class RecordingErrorEventArgs
        Inherits EventArgs

        Public ReadOnly Property ErrorMessage As String

        Public Sub New(errorMessage As String)
            Me.ErrorMessage = errorMessage
        End Sub
    End Class

End Namespace
