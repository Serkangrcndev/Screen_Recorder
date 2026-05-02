Imports System.Drawing

Namespace Models

    Public Class RecordingSettings
        Public Property OutputPath As String = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos) & "\ScreenRecorder"
        Public Property VideoCodec As VideoCodec = VideoCodec.H264
        Public Property VideoQuality As Integer = 23
        Public Property FrameRate As Integer = 60
        Public Property CaptureRegion As Rectangle? = Nothing
        Public Property RecordSystemAudio As Boolean = True
        Public Property RecordMicrophone As Boolean = False
        Public Property MicrophoneDevice As String = ""
        Public Property EnablePrivacyMode As Boolean = False
        Public Property PrivacyRegions As New List(Of PrivacyRegion)()
        Public Property EnableOverlayDrawing As Boolean = False
        Public Property Hotkeys As New HotkeySettings()
        Public Property AutoOrganizeByDate As Boolean = True
        Public Property AutoOrganizeByProject As Boolean = False
        Public Property ProjectName As String = "Default"
        Public Property OutputFormat As String = "mp4"
    End Class

    Public Enum VideoCodec
        H264
        H265
        Lossless
    End Enum

    Public Class PrivacyRegion
        Public Property Bounds As Rectangle
        Public Property MaskType As MaskType = MaskType.Blur
        Public Property Intensity As Integer = 20
    End Class

    Public Enum MaskType
        Blur
        SolidBlack
        SolidWhite
        Pixelate
    End Enum

    Public Class HotkeySettings
        Public Property StartStopRecording As Keys = Keys.F9
        Public Property PauseResumeRecording As Keys = Keys.F10
        Public Property TogglePrivacyMode As Keys = Keys.F11
        Public Property CaptureScreenshot As Keys = Keys.PrintScreen
    End Class

    Public Class RecordingSession
        Public Property Id As String = Guid.NewGuid().ToString("N")
        Public Property StartTime As DateTime = DateTime.Now
        Public Property EndTime As DateTime?
        Public Property OutputFilePath As String
        Public Property Duration As TimeSpan
        Public Property FileSize As Long
        Public Property Status As RecordingStatus = RecordingStatus.Pending
    End Class

    Public Enum RecordingStatus
        Pending
        Recording
        Paused
        Completed
        ErrorOccurred
    End Enum

End Namespace
