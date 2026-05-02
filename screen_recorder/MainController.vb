Imports System
Imports System.Drawing
Imports System.IO
Imports System.Windows.Forms
Imports screen_recorder.UI
Imports screen_recorder.Core
Imports screen_recorder.Models

Namespace UI

    Public Class MainController
        Inherits Form

        Private _settings As RecordingSettings
        Private _capture As ScreenCaptureEngine
        Private _hotkeyManager As GlobalHotkeyManager
        Private _trayManager As SystemTrayManager
        Private _floatingBar As FloatingControlBar
        Private _privacyMaskForm As PrivacyMaskForm
        Private _isRecording As Boolean = False
        private _isPaused As Boolean = False
        Private _lastRecordingPath As String

        Private Const HOTKEY_START As Integer = 1
        Private Const HOTKEY_PAUSE As Integer = 2
        Private Const HOTKEY_PRIVACY As Integer = 3

        Public Sub New()
            _settings = LoadDefaultSettings()
            InitializeComponent()
            InitializeSystem()
        End Sub

        Public Sub New(settings As RecordingSettings)
            _settings = settings
            InitializeComponent()
            InitializeSystem()
        End Sub

        Private Function LoadDefaultSettings() As RecordingSettings
            Dim settings As New RecordingSettings()
            Try
                Dim configPath = Path.Combine(Application.StartupPath, "config.json")
                If File.Exists(configPath) Then
                    ' JSON deserialization would go here
                End If
            Catch
            End Try
            Return settings
        End Function

        Private Sub InitializeComponent()
            WindowState = FormWindowState.Minimized
            ShowInTaskbar = False
            Opacity = 0
            Size = New Size(1, 1)
            Visible = False
        End Sub

        Private Sub InitializeSystem()
            Try
                ' Check if FFmpeg is installed
                If Not Core.FFmpegInstaller.IsFFmpegInstalled() Then
                    ' Show installer wizard
                    Using wizard = New InstallerWizard()
                        wizard.ShowDialog()

                        If Not wizard.IsInstallComplete Then
                            ' User cancelled or installation failed
                            Application.Exit()
                            Return
                        End If
                    End Using
                End If

                _hotkeyManager = New GlobalHotkeyManager()
                RegisterHotkeys()

                _trayManager = New SystemTrayManager(_settings)
                AddHandler _trayManager.StartRecordingRequested, AddressOf OnStartRecording
                AddHandler _trayManager.StopRecordingRequested, AddressOf OnStopRecording
                AddHandler _trayManager.PauseRecordingRequested, AddressOf OnPauseRecording
                AddHandler _trayManager.ShowSettingsRequested, AddressOf OnShowSettings
                AddHandler _trayManager.ShowMainWindowRequested, AddressOf OnShowMainWindow
                AddHandler _trayManager.ExitRequested, AddressOf OnExit

                _floatingBar = New FloatingControlBar()
                _floatingBar.Visible = True
                AddHandler _floatingBar.RecordClicked, AddressOf OnStartRecording
                AddHandler _floatingBar.StopClicked, AddressOf OnStopRecording
                AddHandler _floatingBar.PauseClicked, AddressOf OnPauseRecording
                AddHandler _floatingBar.SettingsClicked, AddressOf OnShowSettings
                AddHandler _floatingBar.PrivacyModeToggled, AddressOf OnTogglePrivacyMode
                AddHandler _floatingBar.MicToggled, AddressOf OnToggleMicMode

                _trayManager.ShowBalloonTip("Screen Recorder", "Uygulama arka planda çalışıyor. Yüzen kontrol paneli ekranda görünür durumda.", ToolTipIcon.Info)

            Catch ex As Exception
                MessageBox.Show($"Sistem başlatma hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error)
                Application.Exit()
            End Try
        End Sub

        Private Sub RegisterHotkeys()
            _hotkeyManager.RegisterHotkey(HOTKEY_START, _settings.Hotkeys.StartStopRecording, GlobalHotkeyManager.KeyModifiers.None, "Kayıt Başlat/Durdur")
            _hotkeyManager.RegisterHotkey(HOTKEY_PAUSE, _settings.Hotkeys.PauseResumeRecording, GlobalHotkeyManager.KeyModifiers.None, "Duraklat/Devam")
            _hotkeyManager.RegisterHotkey(HOTKEY_PRIVACY, _settings.Hotkeys.TogglePrivacyMode, GlobalHotkeyManager.KeyModifiers.None, "Gizli Mod")

            AddHandler _hotkeyManager.HotkeyPressed, AddressOf OnHotkeyPressed
        End Sub

        Private Sub OnHotkeyPressed(sender As Object, e As HotkeyPressedEventArgs)
            Select Case e.Id
                Case HOTKEY_START
                    If _isRecording Then
                        OnStopRecording(Nothing, EventArgs.Empty)
                    Else
                        OnStartRecording(Nothing, EventArgs.Empty)
                    End If

                Case HOTKEY_PAUSE
                    OnPauseRecording(Nothing, EventArgs.Empty)

                Case HOTKEY_PRIVACY
                    OnTogglePrivacyMode(Nothing, EventArgs.Empty)
            End Select
        End Sub

        Private Sub OnStartRecording(sender As Object, e As EventArgs)
            If _isRecording Then Return

            Try
                _capture = New ScreenCaptureEngine()
                AddHandler _capture.RecordingStarted, AddressOf OnCaptureStarted
                AddHandler _capture.RecordingStopped, AddressOf OnCaptureStopped
                AddHandler _capture.RecordingPaused, AddressOf OnCapturePaused
                AddHandler _capture.RecordingResumed, AddressOf OnCaptureResumed
                AddHandler _capture.RecordingError, AddressOf OnCaptureError

                _capture.StartRecording(_settings)

            Catch ex As Exception
                _trayManager.ShowBalloonTip("Kayıt Hatası", ex.Message, ToolTipIcon.Error)
            End Try
        End Sub

        Private Sub OnCaptureStarted(sender As Object, e As EventArgs)
            _isRecording = True
            _isPaused = False

            _floatingBar.IsRecording = True
            _trayManager.SetStatus(True, False)

            _trayManager.ShowBalloonTip("Kayıt Başladı", "Ekran kaydı aktif. Gizli alanlar otomatik olarak maskelenecek.", ToolTipIcon.Info)
        End Sub

        Private Sub OnStopRecording(sender As Object, e As EventArgs)
            If Not _isRecording Then Return

            Try
                _capture?.StopRecording()

            Catch ex As Exception
                _trayManager.ShowBalloonTip("Durdurma Hatası", ex.Message, ToolTipIcon.Error)
            End Try
        End Sub

        Private Sub OnCaptureStopped(sender As Object, e As EventArgs)
            _isRecording = False
            _isPaused = False

            _floatingBar.IsRecording = False
            _floatingBar.IsPaused = False
            _trayManager.SetStatus(False, False)

            _trayManager.ShowBalloonTip("Kayıt Tamamlandı", "Video kaydedildi.", ToolTipIcon.Info)

            _capture?.Dispose()
            _capture = Nothing
        End Sub

        Private Sub OnPauseRecording(sender As Object, e As EventArgs)
            If Not _isRecording Then Return

            _capture?.TogglePause()
        End Sub

        Private Sub OnCapturePaused(sender As Object, e As EventArgs)
            _isPaused = True
            _floatingBar.IsPaused = True
            _trayManager.SetStatus(True, True)
            _trayManager.ShowBalloonTip("Kayıt Durumu", "Kayıt duraklatıldı", ToolTipIcon.Info)
        End Sub

        Private Sub OnCaptureResumed(sender As Object, e As EventArgs)
            _isPaused = False
            _floatingBar.IsPaused = False
            _trayManager.SetStatus(True, False)
            _trayManager.ShowBalloonTip("Kayıt Durumu", "Kayıt devam ediyor", ToolTipIcon.Info)
        End Sub

        Private Sub OnCaptureError(sender As Object, e As RecordingErrorEventArgs)
            _trayManager.ShowBalloonTip("Kayıt Hatası", e.ErrorMessage, ToolTipIcon.Error)
            _isRecording = False
            _isPaused = False
            _floatingBar.IsRecording = False
            _floatingBar.IsPaused = False
            _trayManager.SetStatus(False, False)
        End Sub

        Private Sub OnTogglePrivacyMode(sender As Object, e As EventArgs)
            If _privacyMaskForm IsNot Nothing AndAlso _privacyMaskForm.Visible Then
                _privacyMaskForm.Close()
                _privacyMaskForm = Nothing
                Return
            End If

            _privacyMaskForm = New PrivacyMaskForm()
            AddHandler _privacyMaskForm.MasksDefined, AddressOf OnMasksDefined
            _privacyMaskForm.Show()
        End Sub

        Private Sub OnMasksDefined(sender As Object, e As MasksDefinedEventArgs)
            If e.Masks IsNot Nothing AndAlso e.Masks.Count > 0 Then
                _settings.EnablePrivacyMode = True
                _settings.PrivacyRegions.Clear()

                For Each mask In e.Masks
                    _settings.PrivacyRegions.Add(New PrivacyRegion With {
                        .Bounds = mask.Bounds,
                        .MaskType = mask.MaskType,
                        .Intensity = mask.Intensity
                    })
                Next

                If e.IsFinalized Then
                    _trayManager.ShowBalloonTip("Gizli Mod", $"{e.Masks.Count} hassas alan maskelendi.", ToolTipIcon.Info)
                End If
            End If
        End Sub

        Private Sub OnToggleMicMode(sender As Object, e As EventArgs)
            _settings.RecordMicrophone = Not _settings.RecordMicrophone
            
            If _capture IsNot Nothing Then
                _capture.ToggleMicrophoneMute()
            End If

            Dim status = If(_settings.RecordMicrophone, "Açık", "Kapalı (Sessiz)")
            _trayManager.ShowBalloonTip("Mikrofon", $"Mikrofon {status}.", ToolTipIcon.Info)
        End Sub

        Private Sub OnShowSettings(sender As Object, e As EventArgs)
            Dim settingsForm = New SettingsForm(_settings, _hotkeyManager)

            If settingsForm.ShowDialog() = DialogResult.OK Then
                _settings = settingsForm.SavedSettings

                _hotkeyManager.UnregisterAllHotkeys()
                RegisterHotkeys()

                SaveSettings()
                _trayManager.ShowBalloonTip("Ayarlar", "Ayarlar kaydedildi.", ToolTipIcon.Info)
            End If
        End Sub

        Private Sub SaveSettings()
            Try
                Dim configPath = Path.Combine(Application.StartupPath, "config.json")
                ' Settings serialization would go here
            Catch
            End Try
        End Sub

        Private Sub OnShowMainWindow(sender As Object, e As EventArgs)
            _floatingBar.Visible = True
            _floatingBar.WindowState = FormWindowState.Normal
            _floatingBar.Activate()
        End Sub

        Private Sub OnExit(sender As Object, e As EventArgs)
            If _isRecording Then
                Dim result = MessageBox.Show("Kayıt devam ediyor. Çıkmak istediğinize emin misiniz?", "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
                If result = DialogResult.No Then Return

                OnStopRecording(Nothing, EventArgs.Empty)
            End If

            Application.Exit()
        End Sub

        Protected Overrides Sub OnFormClosing(e As System.Windows.Forms.FormClosingEventArgs)
            Cleanup()
            MyBase.OnFormClosing(e)
        End Sub

        Private Sub Cleanup()
            Try
                _capture?.Dispose()
                _hotkeyManager?.Dispose()
                _trayManager?.Dispose()
                _floatingBar?.Dispose()
                _privacyMaskForm?.Dispose()
            Catch
            End Try
        End Sub

    End Class

End Namespace
