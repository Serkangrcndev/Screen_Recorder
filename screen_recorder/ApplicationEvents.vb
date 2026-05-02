Imports Microsoft.VisualBasic.ApplicationServices

Namespace My
    ' The following events are available for MyApplication:
    ' Startup: Raised when the application starts, before the startup form is created.
    ' Shutdown: Raised after all application forms are closed. This event is not raised if the application terminates abnormally.
    ' UnhandledException: Raised if the application encounters an unhandled exception.
    ' StartupNextInstance: Raised when launching a single-instance application and the application is already active.
    ' NetworkAvailabilityChanged: Raised when the network connection is connected or disconnected.

    ' **NEW** ApplyApplicationDefaults: Raised when the application queries default values to be set for the application.

    ' Example:
    ' Private Sub MyApplication_ApplyApplicationDefaults(sender As Object, e As ApplyApplicationDefaultsEventArgs) Handles Me.ApplyApplicationDefaults
    '
    '   ' Setting the application-wide default Font:
    '   e.Font = New Font(FontFamily.GenericSansSerif, 12, FontStyle.Regular)
    '
    '   ' Setting the HighDpiMode for the Application:
    '   e.HighDpiMode = HighDpiMode.PerMonitorV2
    '
    '   ' If a splash dialog is used, this sets the minimum display time:
    '   e.MinimumSplashScreenDisplayTime = 4000
    ' End Sub

    Partial Friend Class MyApplication
        Private Sub MyApplication_ApplyApplicationDefaults(sender As Object, e As ApplyApplicationDefaultsEventArgs) Handles Me.ApplyApplicationDefaults
            e.HighDpiMode = HighDpiMode.PerMonitorV2
        End Sub

        Private Sub MyApplication_Startup(sender As Object, e As StartupEventArgs) Handles Me.Startup
            ' MainController Application.myapp tarafından MainForm olarak başlatılacak
            ' Burada ekstra işlemler yapılabilir
        End Sub

        Protected Overrides Sub OnShutdown()
            MyBase.OnShutdown()
        End Sub

        Private Function LoadSettings() As screen_recorder.Models.RecordingSettings
            Dim settings As New screen_recorder.Models.RecordingSettings()

            Try
                Dim configPath = System.IO.Path.Combine(System.Windows.Forms.Application.StartupPath, "config.json")
                If System.IO.File.Exists(configPath) Then
                    Dim json = System.IO.File.ReadAllText(configPath)
                End If
            Catch
            End Try

            Return settings
        End Function
    End Class
End Namespace
