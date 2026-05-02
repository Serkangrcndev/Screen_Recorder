Imports System.Drawing
Imports System.Windows.Forms

Namespace UI

    Public Class SystemTrayManager
        Implements IDisposable

        Private _notifyIcon As NotifyIcon
        Private _contextMenu As ContextMenuStrip
        Private _settings As Models.RecordingSettings

        Public Event StartRecordingRequested(sender As Object, e As EventArgs)
        Public Event StopRecordingRequested(sender As Object, e As EventArgs)
        Public Event PauseRecordingRequested(sender As Object, e As EventArgs)
        Public Event ShowSettingsRequested(sender As Object, e As EventArgs)
        Public Event ShowMainWindowRequested(sender As Object, e As EventArgs)
        Public Event ExitRequested(sender As Object, e As EventArgs)

        Public Sub New(settings As Models.RecordingSettings)
            _settings = settings
            InitializeTrayIcon()
        End Sub

        Private Sub InitializeTrayIcon()
            _notifyIcon = New NotifyIcon()
            _notifyIcon.Icon = CreateTrayIcon()
            _notifyIcon.Text = "Screen Recorder"
            _notifyIcon.Visible = True
            AddHandler _notifyIcon.DoubleClick, Sub(s, e)
                                               RaiseEvent ShowMainWindowRequested(Me, e)
                                           End Sub

            CreateContextMenu()
            _notifyIcon.ContextMenuStrip = _contextMenu
        End Sub

        Private Sub CreateContextMenu()
            _contextMenu = New ContextMenuStrip()

            Dim menuShow = New ToolStripMenuItem("Aç", Nothing, Sub(s, e) RaiseEvent ShowMainWindowRequested(Me, e))
            menuShow.Font = New Font(_contextMenu.Font.FontFamily, _contextMenu.Font.Size, FontStyle.Bold)

            Dim menuStart = New ToolStripMenuItem("Kayıt Başlat (F9)", Nothing, Sub(s, e) RaiseEvent StartRecordingRequested(Me, e))
            Dim menuStop = New ToolStripMenuItem("Kayıt Durdur (F10)", Nothing, Sub(s, e) RaiseEvent StopRecordingRequested(Me, e))
            Dim menuPause = New ToolStripMenuItem("Duraklat/Devam (F11)", Nothing, Sub(s, e) RaiseEvent PauseRecordingRequested(Me, e))

            Dim menuSeparator1 = New ToolStripSeparator()
            Dim menuSettings = New ToolStripMenuItem("Ayarlar", Nothing, Sub(s, e) RaiseEvent ShowSettingsRequested(Me, e))
            Dim menuSeparator2 = New ToolStripSeparator()
            Dim menuExit = New ToolStripMenuItem("Çıkış", Nothing, Sub(s, e) RaiseEvent ExitRequested(Me, e))

            _contextMenu.Items.Add(menuShow)
            _contextMenu.Items.Add(New ToolStripSeparator())
            _contextMenu.Items.Add(menuStart)
            _contextMenu.Items.Add(menuStop)
            _contextMenu.Items.Add(menuPause)
            _contextMenu.Items.Add(menuSeparator1)
            _contextMenu.Items.Add(menuSettings)
            _contextMenu.Items.Add(menuSeparator2)
            _contextMenu.Items.Add(menuExit)
        End Sub

        Private Function CreateTrayIcon() As Icon
            Try
                Dim bitmap = New Bitmap(32, 32)
                Using g = Graphics.FromImage(bitmap)
                    g.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
                    g.Clear(Color.Transparent)

                    Using brush = New SolidBrush(Color.FromArgb(0, 120, 212))
                        g.FillEllipse(brush, 2, 2, 28, 28)
                    End Using

                    Using brush = New SolidBrush(Color.White)
                        g.FillEllipse(brush, 12, 12, 8, 8)
                    End Using
                End Using

                Return Icon.FromHandle(bitmap.GetHicon())
            Catch
                Return SystemIcons.Application
            End Try
        End Function

        Public Sub ShowBalloonTip(title As String, message As String, icon As ToolTipIcon)
            _notifyIcon?.ShowBalloonTip(3000, title, message, icon)
        End Sub

        Public Sub SetStatus(isRecording As Boolean, isPaused As Boolean)
            If isRecording Then
                If isPaused Then
                    _notifyIcon.Text = "Screen Recorder - Duraklatıldı"
                    _notifyIcon.Icon = CreatePausedIcon()
                Else
                    _notifyIcon.Text = "Screen Recorder - Kaydediliyor..."
                    _notifyIcon.Icon = CreateRecordingIcon()
                End If
            Else
                _notifyIcon.Text = "Screen Recorder - Hazır"
                _notifyIcon.Icon = CreateTrayIcon()
            End If
        End Sub

        Private Function CreateRecordingIcon() As Icon
            Dim bitmap = New Bitmap(32, 32)
            Using g = Graphics.FromImage(bitmap)
                g.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
                g.Clear(Color.Transparent)

                Using brush = New SolidBrush(Color.FromArgb(220, 53, 69))
                    g.FillEllipse(brush, 2, 2, 28, 28)
                End Using

                Using brush = New SolidBrush(Color.White)
                    g.FillRectangle(brush, 11, 11, 10, 10)
                End Using
            End Using
            Return Icon.FromHandle(bitmap.GetHicon())
        End Function

        Private Function CreatePausedIcon() As Icon
            Dim bitmap = New Bitmap(32, 32)
            Using g = Graphics.FromImage(bitmap)
                g.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
                g.Clear(Color.Transparent)

                Using brush = New SolidBrush(Color.FromArgb(255, 193, 7))
                    g.FillEllipse(brush, 2, 2, 28, 28)
                End Using

                Using brush = New SolidBrush(Color.White)
                    g.FillRectangle(brush, 10, 9, 4, 14)
                    g.FillRectangle(brush, 18, 9, 4, 14)
                End Using
            End Using
            Return Icon.FromHandle(bitmap.GetHicon())
        End Function

        Public Sub Dispose() Implements IDisposable.Dispose
            _notifyIcon?.Dispose()
            _contextMenu?.Dispose()
        End Sub

    End Class

End Namespace
