Imports System.Drawing
Imports System.Windows.Forms
Imports System.IO

Namespace UI

    Public Class VideoTrimEditor
        Inherits Form

        Private _videoPath As String
        Private _startTime As TimeSpan = TimeSpan.Zero
        Private _endTime As TimeSpan = TimeSpan.FromMinutes(5)
        Private _duration As TimeSpan = TimeSpan.FromMinutes(5)
        Private _isDraggingStart As Boolean = False
        Private _isDraggingEnd As Boolean = False

        Private _picPreview As PictureBox
        Private _trackTimeline As Panel
        Private _lblStartTime As Label
        private _lblEndTime As Label
        Private _lblDuration As Label
        Private _btnTrim As FluentButton
        Private _btnCancel As FluentButton
        Private _txtOutputName As TextBox

        Public Event TrimCompleted(sender As Object, e As TrimCompletedEventArgs)

        Public Sub New(videoPath As String)
            _videoPath = videoPath
            InitializeComponent()
            LoadVideoInfo()
        End Sub

        Private Sub InitializeComponent()
            Text = "Video Kırpma Editörü"
            Size = New Size(900, 600)
            StartPosition = FormStartPosition.CenterScreen
            MinimizeBox = False
            BackColor = FluentTheme.GetBackground(FluentTheme.ThemeMode.System)

            _picPreview = New PictureBox() With {
                .Location = New Point(20, 20),
                .Size = New Size(840, 400),
                .BackColor = Color.Black,
                .BorderStyle = BorderStyle.FixedSingle,
                .SizeMode = PictureBoxSizeMode.Zoom
            }

            _trackTimeline = New Panel() With {
                .Location = New Point(20, 430),
                .Size = New Size(840, 60),
                .BackColor = FluentTheme.GetSurface(FluentTheme.ThemeMode.System),
                .BorderStyle = BorderStyle.FixedSingle
            }
            AddHandler _trackTimeline.Paint, AddressOf OnTimelinePaint
            AddHandler _trackTimeline.MouseDown, AddressOf OnTimelineMouseDown
            AddHandler _trackTimeline.MouseMove, AddressOf OnTimelineMouseMove
            AddHandler _trackTimeline.MouseUp, AddressOf OnTimelineMouseUp

            _lblStartTime = New Label() With {
                .Text = "00:00:00",
                .Location = New Point(20, 495),
                .Size = New Size(80, 20),
                .ForeColor = FluentTheme.GetTextPrimary(FluentTheme.ThemeMode.System)
            }

            _lblEndTime = New Label() With {
                .Text = "00:05:00",
                .Location = New Point(780, 495),
                .Size = New Size(80, 20),
                .ForeColor = FluentTheme.GetTextPrimary(FluentTheme.ThemeMode.System)
            }

            _lblDuration = New Label() With {
                .Text = "Süre: 05:00",
                .Location = New Point(400, 495),
                .Size = New Size(100, 20),
                .ForeColor = FluentTheme.GetTextPrimary(FluentTheme.ThemeMode.System)
            }

            Dim lblName = New Label() With {
                .Text = "Dosya Adı:",
                .Location = New Point(20, 520),
                .Size = New Size(80, 25),
                .ForeColor = FluentTheme.GetTextPrimary(FluentTheme.ThemeMode.System)
            }

            _txtOutputName = New TextBox() With {
                .Location = New Point(110, 520),
                .Size = New Size(300, 25),
                .BackColor = FluentTheme.GetSurface(FluentTheme.ThemeMode.System),
                .ForeColor = FluentTheme.GetTextPrimary(FluentTheme.ThemeMode.System)
            }

            _btnTrim = New FluentButton() With {
                .Text = "Kırp",
                .Location = New Point(680, 520),
                .Size = New Size(100, 36),
                .Tag = "Primary"
            }
            AddHandler _btnTrim.Click, AddressOf OnTrimClick

            _btnCancel = New FluentButton() With {
                .Text = "İptal",
                .Location = New Point(790, 520),
                .Size = New Size(70, 36)
            }
            AddHandler _btnCancel.Click, Sub(s, e) Close()

            Controls.Add(_picPreview)
            Controls.Add(_trackTimeline)
            Controls.Add(_lblStartTime)
            Controls.Add(_lblEndTime)
            Controls.Add(_lblDuration)
            Controls.Add(lblName)
            Controls.Add(_txtOutputName)
            Controls.Add(_btnTrim)
            Controls.Add(_btnCancel)
        End Sub

        Private Sub LoadVideoInfo()
            Try
                Dim fileInfo = New FileInfo(_videoPath)
                _txtOutputName.Text = $"trimmed_{Path.GetFileNameWithoutExtension(_videoPath)}.mp4"

                _duration = TimeSpan.FromMinutes(5)
                _endTime = _duration

                UpdateLabels()
                GenerateThumbnail()

            Catch ex As Exception
                MessageBox.Show($"Video bilgisi yüklenemedi: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Sub

        Private Sub GenerateThumbnail()
            Try
                Using bitmap = New Bitmap(840, 400)
                    Using g = Graphics.FromImage(bitmap)
                        g.Clear(Color.Black)

                        Using font = New Font("Segoe UI", 16)
                            TextRenderer.DrawText(g, "Video Önizlemesi", font, New Rectangle(0, 0, 840, 400), Color.White, TextFormatFlags.HorizontalCenter Or TextFormatFlags.VerticalCenter)
                        End Using

                        Using pen = New Pen(Color.White, 2)
                            For i As Integer = 0 To 840 Step 100
                                g.DrawLine(pen, i, 0, i, 400)
                            Next
                            For i As Integer = 0 To 400 Step 50
                                g.DrawLine(pen, 0, i, 840, i)
                            Next
                        End Using
                    End Using

                    _picPreview.Image = CType(bitmap.Clone(), Image)
                End Using
            Catch
            End Try
        End Sub

        Private Sub OnTimelinePaint(sender As Object, e As PaintEventArgs)
            Dim g = e.Graphics
            g.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias

            Dim rect = _trackTimeline.ClientRectangle

            Using brush = New SolidBrush(FluentTheme.GetSurfaceElevated(FluentTheme.ThemeMode.System))
                g.FillRectangle(brush, rect)
            End Using

            Dim startX = TimeToPixel(_startTime)
            Dim endX = TimeToPixel(_endTime)

            Using brush = New SolidBrush(Color.FromArgb(0, 120, 212))
                g.FillRectangle(brush, startX, 5, endX - startX, rect.Height - 10)
            End Using

            Using pen = New Pen(Color.White, 2)
                g.DrawLine(pen, startX, 0, startX, rect.Height)
                g.DrawLine(pen, endX, 0, endX, rect.Height)
            End Using

            Using brush = New SolidBrush(Color.White)
                g.FillEllipse(brush, startX - 6, rect.Height \ 2 - 6, 12, 12)
                g.FillEllipse(brush, endX - 6, rect.Height \ 2 - 6, 12, 12)
            End Using
        End Sub

        Private Sub OnTimelineMouseDown(sender As Object, e As MouseEventArgs)
            Dim startX = TimeToPixel(_startTime)
            Dim endX = TimeToPixel(_endTime)

            If Math.Abs(e.X - startX) < 10 Then
                _isDraggingStart = True
            ElseIf Math.Abs(e.X - endX) < 10 Then
                _isDraggingEnd = True
            End If
        End Sub

        Private Sub OnTimelineMouseMove(sender As Object, e As MouseEventArgs)
            If _isDraggingStart Then
                _startTime = PixelToTime(e.X)
                If _startTime < TimeSpan.Zero Then _startTime = TimeSpan.Zero
                If _startTime >= _endTime Then _startTime = _endTime - TimeSpan.FromSeconds(1)
                UpdateLabels()
                _trackTimeline.Invalidate()
            ElseIf _isDraggingEnd Then
                _endTime = PixelToTime(e.X)
                If _endTime > _duration Then _endTime = _duration
                If _endTime <= _startTime Then _endTime = _startTime + TimeSpan.FromSeconds(1)
                UpdateLabels()
                _trackTimeline.Invalidate()
            Else
                Dim startX = TimeToPixel(_startTime)
                Dim endX = TimeToPixel(_endTime)

                If Math.Abs(e.X - startX) < 10 OrElse Math.Abs(e.X - endX) < 10 Then
                    _trackTimeline.Cursor = Cursors.SizeWE
                Else
                    _trackTimeline.Cursor = Cursors.Default
                End If
            End If
        End Sub

        Private Sub OnTimelineMouseUp(sender As Object, e As MouseEventArgs)
            _isDraggingStart = False
            _isDraggingEnd = False
        End Sub

        Private Function TimeToPixel(time As TimeSpan) As Integer
            Dim ratio = time.TotalMilliseconds / _duration.TotalMilliseconds
            Return CInt(ratio * _trackTimeline.Width)
        End Function

        Private Function PixelToTime(x As Integer) As TimeSpan
            Dim ratio = x / _trackTimeline.Width
            Return TimeSpan.FromMilliseconds(ratio * _duration.TotalMilliseconds)
        End Function

        Private Sub UpdateLabels()
            _lblStartTime.Text = _startTime.ToString("hh\:mm\:ss")
            _lblEndTime.Text = _endTime.ToString("hh\:mm\:ss")
            _lblDuration.Text = $"Süre: {(_endTime - _startTime):hh\:mm\:ss}"
        End Sub

        Private Async Sub OnTrimClick(sender As Object, e As EventArgs)
            _btnTrim.Enabled = False
            _btnTrim.Text = "İşleniyor..."

            Try
                Dim outputPath = Path.Combine(Path.GetDirectoryName(_videoPath), _txtOutputName.Text)
                Dim startSeconds = _startTime.TotalSeconds
                Dim durationSeconds = (_endTime - _startTime).TotalSeconds

                Await Task.Run(Sub()
                                   TrimVideoWithFFmpeg(_videoPath, outputPath, startSeconds, durationSeconds)
                               End Sub)

                RaiseEvent TrimCompleted(Me, New TrimCompletedEventArgs(outputPath, True, Nothing))
                MessageBox.Show("Video başarıyla kırpıldı!", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Close()

            Catch ex As Exception
                RaiseEvent TrimCompleted(Me, New TrimCompletedEventArgs(Nothing, False, ex))
                MessageBox.Show($"Kırpma hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Finally
                _btnTrim.Enabled = True
                _btnTrim.Text = "Kırp"
            End Try
        End Sub

        Private Sub TrimVideoWithFFmpeg(inputPath As String, outputPath As String, startSeconds As Double, durationSeconds As Double)
            Dim ffmpegPath = GetFFmpegPath()
            Dim arguments = $"-i ""{inputPath}"" -ss {startSeconds} -t {durationSeconds} -c copy -avoid_negative_ts 1 ""{outputPath}"""

            Dim startInfo = New ProcessStartInfo(ffmpegPath, arguments) With {
                .UseShellExecute = False,
                .CreateNoWindow = True,
                .RedirectStandardError = True
            }

            Dim ffmpegProcess = Process.Start(startInfo)
            ffmpegProcess.WaitForExit()

            If ffmpegProcess.ExitCode <> 0 Then
                ffmpegProcess.Dispose()
                Throw New Exception("FFmpeg kırpma işlemi başarısız oldu")
            End If

            ffmpegProcess.Dispose()
        End Sub

        Private Function GetFFmpegPath() As String
            Dim localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg.exe")
            If File.Exists(localPath) Then
                Return localPath
            End If

            Dim pathEnv = Environment.GetEnvironmentVariable("PATH")
            For Each p In pathEnv.Split(";"c)
                Dim fullPath = Path.Combine(p, "ffmpeg.exe")
                If File.Exists(fullPath) Then
                    Return fullPath
                End If
            Next

            Return "ffmpeg"
        End Function

        Protected Overrides Sub OnFormClosing(e As System.Windows.Forms.FormClosingEventArgs)
            _picPreview.Image?.Dispose()
            MyBase.OnFormClosing(e)
        End Sub

    End Class

    Public Class TrimCompletedEventArgs
        Inherits EventArgs

        Public ReadOnly Property OutputPath As String
        Public ReadOnly Property Success As Boolean
        Public ReadOnly Property [Error] As Exception

        Public Sub New(outputPath As String, success As Boolean, [error] As Exception)
            Me.OutputPath = outputPath
            Me.Success = success
            Me.Error = [error]
        End Sub
    End Class

End Namespace
